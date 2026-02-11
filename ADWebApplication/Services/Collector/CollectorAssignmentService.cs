using ADWebApplication.Data;
using ADWebApplication.Models;
using ADWebApplication.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ADWebApplication.Services.Collector
{
    public class CollectorAssignmentService : ICollectorAssignmentService
    {
        private readonly In5niteDbContext _db;

        public CollectorAssignmentService(In5niteDbContext db)
        {
            _db = db;
        }

        public async Task<RouteAssignmentSearchViewModel> GetRouteAssignmentsAsync(string username, string? search, int? regionId, DateTime? date, string? status, int page, int pageSize)
        {
            // Start from RoutePlans to ensure filters apply correctly to the individual daily routes
            var query = _db.RoutePlans
                .AsNoTracking()
                .Include(rp => rp.RouteAssignment)
                .Include(rp => rp.RouteStops)
                    .ThenInclude(rs => rs.CollectionBin)
                        .ThenInclude(cb => cb!.Region)
                .Where(rp => rp.RouteAssignment != null && 
                           EF.Functions.Collate(rp.RouteAssignment.AssignedTo, "NOCASE") == EF.Functions.Collate(username.Trim(), "NOCASE") &&
                           rp.PlannedDate.HasValue);

            // 1. Search Filter (ID or Location)
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = ApplySearchFilter(query, search);
            }

            // 2. Region Filter
            if (regionId.HasValue)
            {
                query = query.Where(rp => rp.RouteStops.Any(rs => rs.CollectionBin != null && rs.CollectionBin.RegionId == regionId));
            }

            // 3. Date Filter
            if (date.HasValue)
            {
                var targetDate = date.Value.Date;
                query = query.Where(rp => rp.PlannedDate!.Value.Date == targetDate);
            }

            // 4. Status Filter (including our Pending/Scheduled fix)
            if (!string.IsNullOrWhiteSpace(status))
            {
                if (string.Equals(status, CollectorConstants.StatusPending, StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(rp => rp.RouteStatus == CollectorConstants.StatusPending || rp.RouteStatus == CollectorConstants.StatusScheduled);
                }
                else
                {
                    query = query.Where(rp => rp.RouteStatus == status);
                }
            }

            var totalItems = await query.CountAsync();

            var displayItems = await query
                .OrderByDescending(rp => rp.PlannedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(rp => new RouteAssignmentDisplayItem
                {
                    AssignmentId = rp.AssignmentId,
                    AssignedBy = rp.RouteAssignment!.AssignedBy,
                    AssignedTo = rp.RouteAssignment!.AssignedTo,
                    Status = rp.RouteStatus ?? CollectorConstants.StatusPending,
                    RouteId = rp.RouteId,
                    PlannedDate = rp.PlannedDate ?? DateTime.Today,
                    RouteStatus = rp.RouteStatus,
                    RegionName = rp.RouteStops
                        .Select(rs => rs.CollectionBin != null && rs.CollectionBin.Region != null
                            ? rs.CollectionBin.Region.RegionName
                            : null)
                        .FirstOrDefault(),
                    TotalStops = rp.RouteStops.Count,
                    CompletedStops = rp.RouteStops.Count(rs => rs.CollectionDetails.Any(cd => cd.CollectionStatus == CollectorConstants.StatusCollected))
                })
                .ToListAsync();

            var availableRegions = await _db.Regions.ToListAsync();

            return new RouteAssignmentSearchViewModel
            {
                Assignments = displayItems,
                CurrentPage = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                SearchTerm = search,
                SelectedRegionId = regionId,
                SelectedDate = date,
                SelectedStatus = status,
                AvailableRegions = availableRegions
            };
        }



        public async Task<RouteAssignmentDetailViewModel?> GetRouteAssignmentDetailsAsync(int assignmentId, string username)
        {
            var assignment = await _db.RouteAssignments
                .Include(ra => ra.RoutePlans)
                    .ThenInclude(rp => rp.RouteStops)
                        .ThenInclude(rs => rs!.CollectionBin)
                            .ThenInclude(cb => cb!.Region)
                .Include(ra => ra.RoutePlans)
                    .ThenInclude(rp => rp.RouteStops)
                        .ThenInclude(rs => rs!.CollectionDetails)
                .Where(ra => ra.AssignmentId == assignmentId && EF.Functions.Collate(ra.AssignedTo, "NOCASE") == EF.Functions.Collate(username.Trim(), "NOCASE"))
                .FirstOrDefaultAsync();

            if (assignment == null) return null;

            var route = assignment.RoutePlans!.FirstOrDefault();
            if (route == null) return null;

            var stops = route.RouteStops
                .OrderBy(rs => rs.StopSequence)
                .Select(rs =>
                {
                    var latestCollection = rs.CollectionDetails
                        .OrderByDescending(cd => cd.CurrentCollectionDateTime)
                        .FirstOrDefault();

                    return new RouteStopDisplayItem
                    {
                        StopId = rs.StopId,
                        StopSequence = rs.StopSequence,
                        PlannedCollectionTime = rs.PlannedCollectionTime.DateTime,
                        BinId = rs.CollectionBin?.BinId ?? 0,
                        LocationName = rs.CollectionBin?.LocationName,
                        RegionName = rs.CollectionBin?.Region?.RegionName,
                        IsCollected = latestCollection?.CollectionStatus == CollectorConstants.StatusCollected,
                        CollectedAt = latestCollection?.CurrentCollectionDateTime?.DateTime,
                        CollectionStatus = latestCollection?.CollectionStatus,
                        BinFillLevel = latestCollection?.BinFillLevel
                    };
                }).ToList();

            return new RouteAssignmentDetailViewModel
            {
                AssignmentId = assignment.AssignmentId,
                AssignedBy = assignment.AssignedBy,
                AssignedTo = assignment.AssignedTo,
                RouteId = route.RouteId,
                PlannedDate = route.PlannedDate ?? DateTime.Today,
                RouteStatus = route.RouteStatus,
                RouteStops = stops
            };
        }

        public async Task<NextStopsViewModel?> GetNextStopsAsync(string username, int top)
        {
            var todayAssignment = await _db.RouteAssignments
                .Include(ra => ra.RoutePlans)
                    .ThenInclude(rp => rp.RouteStops)
                        .ThenInclude(rs => rs.CollectionBin)
                .Include(ra => ra.RoutePlans)
                    .ThenInclude(rp => rp.RouteStops)
                        .ThenInclude(rs => rs.CollectionDetails)
                .Where(ra => EF.Functions.Collate(ra.AssignedTo, "NOCASE") == EF.Functions.Collate(username.Trim(), "NOCASE")
                          && ra.RoutePlans.Any(rp => 
                              rp.PlannedDate.HasValue
                              && rp.PlannedDate.Value.Date == DateTime.Today 
                              && rp.RouteStatus != "Completed"))
                .FirstOrDefaultAsync();

            if (todayAssignment == null) return null;

            var route = todayAssignment.RoutePlans
                .FirstOrDefault(rp => rp.PlannedDate.HasValue && rp.PlannedDate.Value.Date == DateTime.Today && rp.RouteStatus != "Completed");
            if (route == null) return null;

            var nextStops = route.RouteStops
                .Where(rs => !rs.CollectionDetails.Any(cd => cd.CollectionStatus == CollectorConstants.StatusCollected))
                .OrderBy(rs => rs.StopSequence)
                .Take(top)
                .Select(rs => new RouteStopDisplayItem
                {
                    StopId = rs.StopId,
                    StopSequence = rs.StopSequence,
                    PlannedCollectionTime = rs.PlannedCollectionTime.DateTime,
                    BinId = rs.CollectionBin?.BinId ?? 0,
                    LocationName = rs.CollectionBin?.LocationName,
                    IsCollected = false
                }).ToList();

            var totalPending = route.RouteStops
                .Count(rs => !rs.CollectionDetails.Any(cd => cd.CollectionStatus == CollectorConstants.StatusCollected));

            return new NextStopsViewModel
            {
                AssignmentId = todayAssignment.AssignmentId,
                RouteId = route.RouteId,
                PlannedDate = route.PlannedDate ?? DateTime.Today,
                NextStops = nextStops,
                TotalPendingStops = totalPending
            };
        }

        private static IQueryable<RoutePlan> ApplySearchFilter(IQueryable<RoutePlan> query, string search)
        {
            return query.Where(rp =>
                rp.RouteId.ToString().Contains(search) ||
                rp.RouteStops.Any(rs =>
                    rs.CollectionBin != null &&
                    rs.CollectionBin.LocationName != null &&
                    rs.CollectionBin.LocationName.Contains(search)
                )
            );
        }
    }
}
