using ADWebApplication.Data;
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
            var query = _db.RouteAssignments
                .AsNoTracking()
                .Where(ra => ra.AssignedTo == username);

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(ra =>
                    ra.RoutePlans.Any(rp =>
                        rp.RouteId.ToString().Contains(search) ||
                        rp.RouteStops.Any(rs =>
                            rs.CollectionBin != null &&
                            rs.CollectionBin.LocationName != null &&
                            rs.CollectionBin.LocationName.Contains(search)
                        )
                    )
                );
            }

            if (regionId.HasValue)
            {
                query = query.Where(ra => ra.RoutePlans.Any(rp =>
                    rp.RouteStops.Any(rs => rs.CollectionBin.RegionId == regionId)));
            }

            if (date.HasValue)
            {
                query = query.Where(ra => ra.RoutePlans.Any(rp =>
                    EF.Property<DateTime?>(rp, "PlannedDate") != null &&
                    rp.PlannedDate.HasValue && rp.PlannedDate.Value.Date == date.Value.Date));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(ra => ra.RoutePlans.Any(rp => rp.RouteStatus == status));
            }

            var totalItems = await query
                .SelectMany(ra => ra.RoutePlans)
                .CountAsync(rp => rp.PlannedDate.HasValue);

            var displayItems = await query
                .SelectMany(ra => ra.RoutePlans
                    .Where(rp => rp.PlannedDate.HasValue)
                    .Select(rp => new RouteAssignmentDisplayItem
                    {
                        AssignmentId = ra.AssignmentId,
                        AssignedBy = ra.AssignedBy,
                        AssignedTo = ra.AssignedTo,
                        Status = rp.RouteStatus ?? "Pending",
                        RouteId = rp.RouteId,
                        PlannedDate = rp.PlannedDate ?? DateTime.Today,
                        RouteStatus = rp.RouteStatus,
                        RegionName = rp.RouteStops
                            .Select(rs => rs.CollectionBin != null && rs.CollectionBin.Region != null
                                ? rs.CollectionBin.Region.RegionName
                                : null)
                            .FirstOrDefault(),
                        TotalStops = rp.RouteStops.Count(),
                        CompletedStops = rp.RouteStops.Count(rs => rs.CollectionDetails.Any(cd => cd.CollectionStatus == "Collected"))
                    }))
                .OrderByDescending(x => x.PlannedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
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
                        .ThenInclude(rs => rs.CollectionBin)
                            .ThenInclude(cb => cb.Region)
                .Include(ra => ra.RoutePlans)
                    .ThenInclude(rp => rp.RouteStops)
                        .ThenInclude(rs => rs.CollectionDetails)
                .Where(ra => ra.AssignmentId == assignmentId && ra.AssignedTo == username)
                .FirstOrDefaultAsync();

            if (assignment == null) return null;

            var route = assignment.RoutePlans.FirstOrDefault();
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
                        IsCollected = latestCollection?.CollectionStatus == "Collected",
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
                .Where(ra => ra.AssignedTo == username
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
                .Where(rs => !rs.CollectionDetails.Any(cd => cd.CollectionStatus == "Collected"))
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
                .Count(rs => !rs.CollectionDetails.Any(cd => cd.CollectionStatus == "Collected"));

            return new NextStopsViewModel
            {
                AssignmentId = todayAssignment.AssignmentId,
                RouteId = route.RouteId,
                PlannedDate = route.PlannedDate ?? DateTime.Today,
                NextStops = nextStops,
                TotalPendingStops = totalPending
            };
        }
    }
}
