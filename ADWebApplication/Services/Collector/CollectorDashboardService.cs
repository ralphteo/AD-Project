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
            var assignment = await _db.RouteAssignments
                .Include(ra => ra.RoutePlans)
                    .ThenInclude(rp => rp.RouteStops)
                        .ThenInclude(rs => rs.CollectionBin)
                            .ThenInclude(cb => cb.Region)
                .Include(ra => ra.RoutePlans)
                    .ThenInclude(rp => rp.RouteStops)
                        .ThenInclude(rs => rs.CollectionDetails)
                .Where(ra => ra.AssignedTo == username
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

            var orderedStops = routePlan.RouteStops
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
            var stop = await _db.RouteStops
                .Include(rs => rs.CollectionBin)
                    .ThenInclude(cb => cb.Region)
                .Include(rs => rs.CollectionDetails)
                .Include(rs => rs.RoutePlan)
                    .ThenInclude(rp => rp.RouteAssignment)
                .Where(rs => rs.StopId == stopId && rs.RoutePlan != null && rs.RoutePlan.RouteAssignment != null)
                .Where(rs => rs.RoutePlan!.RouteAssignment!.AssignedTo == username)
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

        public async Task<bool> ConfirmCollectionAsync(CollectionConfirmationVM model, string username)
        {
            var today = DateTime.Today;
            var stop = await _db.RouteStops
                .Include(rs => rs.CollectionDetails)
                .Include(rs => rs.RoutePlan)
                    .ThenInclude(rp => rp.RouteAssignment)
                .Where(rs => rs.StopId == model.StopId 
                          && rs.RoutePlan != null 
                          && rs.RoutePlan.RouteAssignment != null
                          && rs.RoutePlan.RouteAssignment.AssignedTo == username
                          && rs.RoutePlan.PlannedDate.HasValue
                          && rs.RoutePlan.PlannedDate.Value.Date == today)
                .FirstOrDefaultAsync();

            if (stop == null) return false;

            var latestCollection = stop.CollectionDetails
                .OrderByDescending(cd => cd.CurrentCollectionDateTime)
                .FirstOrDefault();

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

            // Update Route Status
            if (stop.RoutePlan != null)
            {
                if (stop.RoutePlan.RouteStatus == "Pending")
                {
                    stop.RoutePlan.RouteStatus = "In Progress";
                }

                // Check if all stops in this route are now collected
                var allStops = await _db.RouteStops
                    .Where(rs => rs.RouteId == stop.RouteId)
                    .Include(rs => rs.CollectionDetails)
                    .ToListAsync();

                var completedCount = allStops.Count(rs =>
                    rs.StopId == stop.StopId ||
                    rs.CollectionDetails.Any(cd => cd.CollectionStatus == "Collected")
                );

                if (completedCount == allStops.Count)
                {
                    stop.RoutePlan.RouteStatus = "Completed";
                }

                // Predict next stop for the view model
                var orderedStops = allStops
                    .OrderBy(rs => rs.StopSequence)
                    .ToList();

                var currentIndex = orderedStops.FindIndex(rs => rs.StopId == model.StopId);
                if (currentIndex >= 0)
                {
                    for (var i = currentIndex + 1; i < orderedStops.Count; i++)
                    {
                        var nextStop = orderedStops[i];
                        var hasCollected = nextStop.CollectionDetails.Any(cd => cd.CollectionStatus == "Collected");

                        if (!hasCollected)
                        {
                            model.NextPointId = nextStop.StopId.ToString();
                            model.NextLocationName = nextStop.CollectionBin?.LocationName;
                            model.NextAddress = nextStop.CollectionBin?.LocationAddress;
                            model.NextPlannedTime = nextStop.PlannedCollectionTime.DateTime;
                            break;
                        }
                    }
                }
            }

            await _db.SaveChangesAsync();
            return true;
        }
    }
}
