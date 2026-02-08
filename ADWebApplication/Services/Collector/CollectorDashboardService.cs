using ADWebApplication.Data;
using ADWebApplication.Models;
using ADWebApplication.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ADWebApplication.Services.Collector
{
    public class CollectorDashboardService : ICollectorDashboardService
    {
        private readonly In5niteDbContext _db;

        public CollectorDashboardService(In5niteDbContext db)
        {
            _db = db;
        }

        public async Task<CollectorRoute> GetDailyRouteAsync(string username)
        {
            var today = DateTime.Today;
            var normalizedUsername = username.Trim().ToUpper();
            var assignment = await _db.RouteAssignments
                .Include(ra => ra.RoutePlans)
                    .ThenInclude(rp => rp.RouteStops)
                        .ThenInclude(rs => rs.CollectionBin)
                            .ThenInclude(cb => cb.Region)
                .Include(ra => ra.RoutePlans)
                    .ThenInclude(rp => rp.RouteStops)
                        .ThenInclude(rs => rs.CollectionDetails)
                .AsSplitQuery()
                .Where(ra => ra.AssignedTo.Trim().ToUpper() == normalizedUsername
                          && ra.RoutePlans.Any(rp => rp.PlannedDate.HasValue && rp.PlannedDate.Value.Date == today))
                .FirstOrDefaultAsync();

            if (assignment == null)
            {
                return new CollectorRoute
                {
                    RouteName = "No Route Assigned",
                    Zone = "-",
                    ScheduledDate = today,
                    Status = "Pending",
                    CollectionPoints = new List<CollectionPoint>()
                };
            }

            var routePlan = assignment.RoutePlans
                .FirstOrDefault(rp => rp.PlannedDate.HasValue && rp.PlannedDate.Value.Date == today);

            if (routePlan == null)
            {
                return new CollectorRoute
                {
                    RouteName = "No Route Assigned",
                    Zone = "-",
                    ScheduledDate = today,
                    Status = "Pending",
                    CollectionPoints = new List<CollectionPoint>()
                };
            }

            // Deduplicate stops by StopId to handle potential data issues or EF product join artifacts
            var orderedStops = routePlan.RouteStops
                .GroupBy(rs => rs.StopId)
                .Select(g => g.First())
                .OrderBy(rs => rs.StopSequence)
                .ToList();

            var points = new List<CollectionPoint>();
            DateTime? previousTime = null;

            foreach (var stop in orderedStops)
            {
                var latestCollection = stop.CollectionDetails
                    .OrderByDescending(cd => cd.CurrentCollectionDateTime)
                    .FirstOrDefault();

                var plannedTime = stop.PlannedCollectionTime.DateTime;
                var estimatedMinutes = previousTime.HasValue
                    ? (int)Math.Max(0, (plannedTime - previousTime.Value).TotalMinutes)
                    : 0;

                var binId = stop.BinId;
                var binLabel = binId.HasValue ? $"B{binId.Value:000}" : $"Stop {stop.StopId}";

                points.Add(new CollectionPoint
                {
                    StopId = stop.StopId,
                    BinId = binId,
                    PointId = binLabel,
                    LocationName = stop.CollectionBin?.LocationName ?? "Unknown location",
                    Address = stop.CollectionBin?.LocationAddress ?? "",
                    Latitude = stop.CollectionBin?.Latitude,
                    Longitude = stop.CollectionBin?.Longitude,
                    PlannedCollectionTime = plannedTime,
                    EstimatedTimeMins = estimatedMinutes,
                    CurrentFillLevel = latestCollection?.BinFillLevel ?? 0,
                    Status = latestCollection?.CollectionStatus == "Collected" ? "Collected" : "Pending",
                    CollectedAt = latestCollection?.CurrentCollectionDateTime?.DateTime
                });

                previousTime = plannedTime;
            }

            return new CollectorRoute
            {
                RouteId = routePlan.RouteId.ToString(),
                RouteName = $"Route #{routePlan.RouteId}",
                Zone = orderedStops.FirstOrDefault()?.CollectionBin?.Region?.RegionName ?? "Assigned Route",
                ScheduledDate = routePlan.PlannedDate ?? today,
                Status = routePlan.RouteStatus ?? "Pending",
                CollectionPoints = points
            };
        }

        public async Task<CollectionConfirmationVM?> GetCollectionConfirmationAsync(int stopId, string username)
        {
            var normalizedUsername = username.Trim().ToUpper();
            var stop = await _db.RouteStops
                .Include(rs => rs.CollectionBin)
                    .ThenInclude(cb => cb.Region)
                .Include(rs => rs.CollectionDetails)
                .Include(rs => rs.RoutePlan)
                    .ThenInclude(rp => rp!.RouteAssignment)
                .Where(rs => rs.StopId == stopId && rs.RoutePlan != null && rs.RoutePlan.RouteAssignment != null)
                .Where(rs => rs.RoutePlan!.RouteAssignment!.AssignedTo.Trim().ToUpper() == normalizedUsername)
                .FirstOrDefaultAsync();

            if (stop == null) return null;

            var latestCollection = stop.CollectionDetails
                .OrderByDescending(cd => cd.CurrentCollectionDateTime)
                .FirstOrDefault();

            var binId = stop.BinId;
            var binLabel = binId.HasValue ? $"B{binId.Value:000}" : $"Stop {stop.StopId}";

            return new CollectionConfirmationVM
            {
                StopId = stop.StopId,
                PointId = binLabel,
                LocationName = stop.CollectionBin?.LocationName ?? "",
                Address = stop.CollectionBin?.LocationAddress ?? "",
                BinId = binId?.ToString() ?? "",
                Zone = stop.CollectionBin?.Region?.RegionName ?? "",
                BinFillLevel = latestCollection?.BinFillLevel ?? 0,
                CollectionTime = DateTime.Now
            };
        }

        // =========================
        // Refactored (same logic)
        // =========================
        public async Task<bool> ConfirmCollectionAsync(CollectionConfirmationVM model, string username)
        {
            var normalizedUsername = username.Trim().ToUpper();
            var today = DateTime.Today;

            var stop = await FindStopForConfirmationAsync(model.StopId, normalizedUsername, today);
            if (stop == null) return false;

            var latestCollection = GetLatestCollection(stop);

            var newDetail = new CollectionDetails
            {
                StopId = stop.StopId,
                BinId = stop.BinId,
                LastCollectionDateTime = latestCollection?.CurrentCollectionDateTime,
                CurrentCollectionDateTime = DateTimeOffset.Now,
                BinFillLevel = model.BinFillLevel,
                CollectionStatus = "Collected",
                IssueLog = model.Remarks
            };

            _db.CollectionDetails.Add(newDetail);
            model.CollectionTime = newDetail.CurrentCollectionDateTime.Value.DateTime;

            // Update Route Status + Predict next stop (same intended logic)
            if (stop.RoutePlan != null)
            {
                await UpdateRouteStatusAndNextStopAsync(stop, model);
            }

            await _db.SaveChangesAsync();
            return true;
        }

        // ===== Helpers (to reduce Sonar cognitive complexity) =====

        private Task<RouteStop?> FindStopForConfirmationAsync(int stopId, string normalizedUsername, DateTime today)
        {
            return _db.RouteStops
                .Include(rs => rs.CollectionDetails)
                .Include(rs => rs.RoutePlan)
                    .ThenInclude(rp => rp!.RouteAssignment)
                .Where(rs => rs.StopId == stopId
                          && rs.RoutePlan != null
                          && rs.RoutePlan.RouteAssignment != null
                          && rs.RoutePlan.RouteAssignment.AssignedTo.Trim().ToUpper() == normalizedUsername
                          && rs.RoutePlan.PlannedDate.HasValue
                          && rs.RoutePlan.PlannedDate.Value.Date == today)
                .FirstOrDefaultAsync();
        }

        private static CollectionDetails? GetLatestCollection(RouteStop stop)
        {
            return stop.CollectionDetails
                .OrderByDescending(cd => cd.CurrentCollectionDateTime)
                .FirstOrDefault();
        }

        private async Task UpdateRouteStatusAndNextStopAsync(RouteStop stop, CollectionConfirmationVM model)
        {
            // 1) Pending -> In Progress
            if (stop.RoutePlan!.RouteStatus == "Pending")
            {
                stop.RoutePlan.RouteStatus = "In Progress";
            }

            // 2) Load all stops (Include CollectionBin because we read NextLocationName/NextAddress)
            var allStops = await LoadAllStopsForRouteAsync(stop.RouteId);

            // 3) Completed logic (treat current stop as collected)
            if (IsRouteCompleted(allStops, stop.StopId))
            {
                stop.RoutePlan.RouteStatus = "Completed";
            }

            // 4) Predict next uncollected stop (same approach as your for-loop)
            var nextStop = FindNextUncollectedStop(allStops, model.StopId);
            ApplyNextStopToModel(model, nextStop);
        }

        private Task<List<RouteStop>> LoadAllStopsForRouteAsync(int? routeId)
        {
            return _db.RouteStops
                .Where(rs => rs.RouteId == routeId)
                .Include(rs => rs.CollectionDetails)
                .Include(rs => rs.CollectionBin)
                .ToListAsync();
        }

        private static bool IsRouteCompleted(List<RouteStop> allStops, int currentStopId)
        {
            var completedCount = allStops.Count(rs =>
                rs.StopId == currentStopId ||
                rs.CollectionDetails.Any(cd => cd.CollectionStatus == "Collected")
            );

            return completedCount == allStops.Count;
        }

        private static RouteStop? FindNextUncollectedStop(List<RouteStop> allStops, int currentStopId)
        {
            var orderedStops = allStops
                .OrderBy(rs => rs.StopSequence)
                .ToList();

            var currentIndex = orderedStops.FindIndex(rs => rs.StopId == currentStopId);
            if (currentIndex < 0) return null;

            for (var i = currentIndex + 1; i < orderedStops.Count; i++)
            {
                var nextStop = orderedStops[i];
                var hasCollected = nextStop.CollectionDetails.Any(cd => cd.CollectionStatus == "Collected");

                if (!hasCollected)
                {
                    return nextStop;
                }
            }

            return null;
        }

        private static void ApplyNextStopToModel(CollectionConfirmationVM model, RouteStop? nextStop)
        {
            if (nextStop == null) return;

            model.NextPointId = nextStop.StopId.ToString();
            model.NextLocationName = nextStop.CollectionBin?.LocationName;
            model.NextAddress = nextStop.CollectionBin?.LocationAddress;
            model.NextPlannedTime = nextStop.PlannedCollectionTime.DateTime;
        }
    }
}
