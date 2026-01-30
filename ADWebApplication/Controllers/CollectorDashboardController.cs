using ADWebApplication.Models;
using ADWebApplication.Data;
using ADWebApplication.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ADWebApplication.Controllers
{
    [Authorize(Roles = "Collector")]
    public class CollectorDashboardController : Controller
    {
        private readonly In5niteDbContext _db;

        public CollectorDashboardController(In5niteDbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            // TODO: [ML INTEGRATION] 
            // 1. Fetch predicted fill levels from Python ML Service.
            // 2. Fetch optimized route sequence from Route Optimization Engine.
            // 3. Replace the Mock Data below with the actual API response.

            // Mock Data for Dashboard
            var todayRoute = GetMockRouteData();
            return View(todayRoute);
        }

        public IActionResult RouteDetails(string id)
        {
            _ = id; // Suppress unused parameter warning
            var route = GetMockRouteData();
            return View(route);
        }

        [HttpGet]
        public IActionResult ConfirmCollection(string id)
        {
            // In a real app, we would fetch the specific point by ID.
            // For now, we'll just mock the data for Point CP-1 (Tampines Mall)
            var viewModel = new CollectionConfirmationVM
            {
                PointId = id,
                LocationName = "Tampines Mall - Loading Bay",
                Address = "4 Tampines Central 5, Singapore 529510",
                BinId = "#45",
                Zone = "Zone A",
                CollectionTime = DateTime.Now
            };
            return View(viewModel);
        }

        [HttpPost]
        public IActionResult ConfirmCollection(CollectionConfirmationVM model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // TODO: Save to Database (Paused)
            // Save model.CollectedWeightKg, model.CollectedCategories, etc.

            // Redirect to Success Page
             return RedirectToAction("CollectionConfirmed", model);
        }

        public IActionResult CollectionConfirmed(CollectionConfirmationVM model)
        {
            return View(model);
        }

        [HttpGet]
        public IActionResult ReportIssue(string? pointId)
        {
            var model = new ReportIssueVM { PointId = pointId ?? "" };
            return View(model);
        }

        [HttpPost]
        public IActionResult ReportIssue(ReportIssueVM model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // TODO: Process report (Mock)
            // Save issue details...

            TempData["SuccessMessage"] = "Issue reported successfully!";
            return RedirectToAction("Index");
        }

        private static CollectorRoute GetMockRouteData()
        {
             var route = new CollectorRoute
            {
                RouteId = "R-SG-101",
                RouteName = "Route SG-East",
                Zone = "Tampines & Bedok",
                ScheduledDate = DateTime.Today,
                Status = "In Progress",
                CollectionPoints = new List<CollectionPoint>
                {
                    new CollectionPoint { PointId = "CP-1", LocationName = "Tampines Mall - Loading Bay", Address = "4 Tampines Central 5, Singapore 529510", DistanceKm = 0.5, EstimatedTimeMins = 5, CurrentFillLevel = 85, Status = "Pending" },
                    new CollectionPoint { PointId = "CP-2", LocationName = "Bedok Mall - Basement 2", Address = "311 New Upper Changi Rd, Singapore 467360", DistanceKm = 3.2, EstimatedTimeMins = 12, CurrentFillLevel = 60, Status = "Pending" },
                    new CollectionPoint { PointId = "CP-3", LocationName = "Changi City Point", Address = "5 Changi Business Park Central 1, Singapore 486038", DistanceKm = 5.1, EstimatedTimeMins = 18, CurrentFillLevel = 45, Status = "Pending" },
                    new CollectionPoint { PointId = "CP-4", LocationName = "Pasir Ris White Sands", Address = "1 Pasir Ris Central St 3, Singapore 518457", DistanceKm = 8.5, EstimatedTimeMins = 25, CurrentFillLevel = 90, Status = "Pending" },
                     // Completed points
                    new CollectionPoint { PointId = "CP-5", LocationName = "Century Square", Address = "2 Tampines Central 5, Singapore 529509", DistanceKm = 0.2, EstimatedTimeMins = 5, CurrentFillLevel = 10, Status = "Collected", CollectedWeightKg = 15.5, CollectedAt = DateTime.Now.AddMinutes(-30) },
                    new CollectionPoint { PointId = "CP-6", LocationName = "Our Tampines Hub", Address = "1 Tampines Walk, Singapore 528523", DistanceKm = 0.8, EstimatedTimeMins = 8, CurrentFillLevel = 5, Status = "Collected", CollectedWeightKg = 12.0, CollectedAt = DateTime.Now.AddMinutes(-60) },
                    new CollectionPoint { PointId = "CP-7", LocationName = "Eastpoint Mall", Address = "3 Simei Street 6, Singapore 528833", DistanceKm = 2.5, EstimatedTimeMins = 15, CurrentFillLevel = 0, Status = "Collected", CollectedWeightKg = 18.2, CollectedAt = DateTime.Now.AddMinutes(-90) },
                    new CollectionPoint { PointId = "CP-8", LocationName = "Jewel Changi Airport", Address = "78 Airport Blvd, Singapore 819666", DistanceKm = 10.5, EstimatedTimeMins = 30, CurrentFillLevel = 0, Status = "Collected", CollectedWeightKg = 45.0, CollectedAt = DateTime.Now.AddHours(-2) }
                }
            };
            return route;
        }

        // ============================================
        // ROUTE ASSIGNMENT FEATURE
        // ============================================

        /// <summary>
        /// Display collector's route assignments with search and filter
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> MyRouteAssignments(
            string? search,
            int? regionId,
            DateTime? date,
            string? status,
            int page = 1,
            int pageSize = 10)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized();
            }

            // Base query - get all assignments for this collector
            var query = _db.RouteAssignments
                .Include(ra => ra.RoutePlans)
                    .ThenInclude(rp => rp.RouteStops)
                        .ThenInclude(rs => rs.CollectionBin)
                            .ThenInclude(cb => cb.Region)
                .Include(ra => ra.RoutePlans)
                    .ThenInclude(rp => rp.RouteStops)
                        .ThenInclude(rs => rs.CollectionDetails)
                .Where(ra => ra.AssignedTo == username);

            // Apply search filter (search by route ID or location name)
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

            // Apply region filter
            if (regionId.HasValue)
            {
                query = query.Where(ra => ra.RoutePlans.Any(rp =>
                    rp.RouteStops.Any(rs => rs.CollectionBin.RegionId == regionId)));
            }

            // Apply date filter (use RoutePlan.PlannedDate)
            if (date.HasValue)
            {
                query = query.Where(ra => ra.RoutePlans.Any(rp => rp.PlannedDate.Date == date.Value.Date));
            }

            // Apply status filter (use RoutePlan.RouteStatus)
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(ra => ra.RoutePlans.Any(rp => rp.RouteStatus == status));
            }

            // Get total count for pagination
            var totalItems = await query.CountAsync();

            // Get paginated results (order by PlannedDate from RoutePlan)
            var assignments = await query
                .SelectMany(ra => ra.RoutePlans.Select(rp => new { Assignment = ra, RoutePlan = rp }))
                .OrderByDescending(x => x.RoutePlan.PlannedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Map to display items
            var displayItems = assignments.Select(x => new RouteAssignmentDisplayItem
            {
                AssignmentId = x.Assignment.AssignmentId,
                AssignedBy = x.Assignment.AssignedBy,
                AssignedTo = x.Assignment.AssignedTo,
                Status = x.RoutePlan.RouteStatus ?? "Pending",  // Use RoutePlan.RouteStatus
                RouteId = x.RoutePlan.RouteId,
                PlannedDate = x.RoutePlan.PlannedDate,
                RouteStatus = x.RoutePlan.RouteStatus,
                RegionName = x.RoutePlan.RouteStops.FirstOrDefault()?.CollectionBin?.Region?.RegionName,
                TotalStops = x.RoutePlan.RouteStops.Count,
                CompletedStops = x.RoutePlan.RouteStops.Count(rs => rs.CollectionDetails.Any(cd => cd.CollectionStatus == "Collected"))
            }).ToList();

            // Get available regions for dropdown
            var availableRegions = await _db.Regions.ToListAsync();

            // Create ViewModel
            var viewModel = new RouteAssignmentSearchViewModel
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

            return View(viewModel);
        }

        /// <summary>
        /// Display detailed view of a specific route assignment
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> RouteAssignmentDetails(int id)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized();
            }

            var assignment = await _db.RouteAssignments
                .Include(ra => ra.RoutePlans)
                    .ThenInclude(rp => rp.RouteStops)
                        .ThenInclude(rs => rs.CollectionBin)
                            .ThenInclude(cb => cb.Region)
                .Include(ra => ra.RoutePlans)
                    .ThenInclude(rp => rp.RouteStops)
                        .ThenInclude(rs => rs.CollectionDetails)
                .Where(ra => ra.AssignmentId == id && ra.AssignedTo == username)
                .FirstOrDefaultAsync();

            if (assignment == null)
            {
                return NotFound();
            }

            var route = assignment.RoutePlans.FirstOrDefault();
            if (route == null)
            {
                return NotFound();
            }

            // Map route stops
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

            var viewModel = new RouteAssignmentDetailViewModel
            {
                AssignmentId = assignment.AssignmentId,
                AssignedBy = assignment.AssignedBy,
                AssignedTo = assignment.AssignedTo,
                RouteId = route.RouteId,
                PlannedDate = route.PlannedDate,
                RouteStatus = route.RouteStatus,
                RouteStops = stops
            };

            return View(viewModel);
        }

        /// <summary>
        /// Get next pending stops (sequence-based, customizable count)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetNextStops(int? top = 10)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized();
            }

            // Get today's route assignment (filter by RoutePlan.PlannedDate and RouteStatus)
            var todayAssignment = await _db.RouteAssignments
                .Include(ra => ra.RoutePlans)
                    .ThenInclude(rp => rp.RouteStops)
                        .ThenInclude(rs => rs.CollectionBin)
                .Include(ra => ra.RoutePlans)
                    .ThenInclude(rp => rp.RouteStops)
                        .ThenInclude(rs => rs.CollectionDetails)
                .Where(ra => ra.AssignedTo == username
                          && ra.RoutePlans.Any(rp => 
                              rp.PlannedDate.Date == DateTime.Today 
                              && rp.RouteStatus != "Completed"))
                .FirstOrDefaultAsync();

            if (todayAssignment == null)
            {
                return NotFound(new { message = "No active route assignment for today" });
            }

            var route = todayAssignment.RoutePlans
                .FirstOrDefault(rp => rp.PlannedDate.Date == DateTime.Today && rp.RouteStatus != "Completed");
            if (route == null)
            {
                return NotFound(new { message = "No route plan found" });
            }

            // Get next pending stops by sequence
            var nextStops = route.RouteStops
                .Where(rs => !rs.CollectionDetails.Any(cd => cd.CollectionStatus == "Collected"))
                .OrderBy(rs => rs.StopSequence)
                .Take(top ?? 10)
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

            var viewModel = new NextStopsViewModel
            {
                AssignmentId = todayAssignment.AssignmentId,
                RouteId = route.RouteId,
                PlannedDate = route.PlannedDate,
                NextStops = nextStops,
                TotalPendingStops = totalPending
            };

            return Json(viewModel);
        }
    }
}