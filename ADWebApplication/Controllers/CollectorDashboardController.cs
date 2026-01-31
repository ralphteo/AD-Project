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

        public async Task<IActionResult> Index()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized();
            }

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
                          && ra.RoutePlans.Any(rp => rp.PlannedDate.Date == today))
                .FirstOrDefaultAsync();

            if (assignment == null)
            {
                return View(new CollectorRoute
                {
                    RouteName = "No Route Assigned",
                    Zone = "-",
                    ScheduledDate = today,
                    Status = "Pending",
                    CollectionPoints = new List<CollectionPoint>()
                });
            }

            var routePlan = assignment.RoutePlans
                .FirstOrDefault(rp => rp.PlannedDate.Date == today);

            if (routePlan == null)
            {
                return View(new CollectorRoute
                {
                    RouteName = "No Route Assigned",
                    Zone = "-",
                    ScheduledDate = today,
                    Status = "Pending",
                    CollectionPoints = new List<CollectionPoint>()
                });
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

            var route = new CollectorRoute
            {
                RouteId = routePlan.RouteId.ToString(),
                RouteName = $"Route #{routePlan.RouteId}",
                Zone = orderedStops.FirstOrDefault()?.CollectionBin?.Region?.RegionName ?? "Assigned Route",
                ScheduledDate = routePlan.PlannedDate,
                Status = routePlan.RouteStatus ?? "Pending",
                CollectionPoints = points
            };

            return View(route);
        }

        public IActionResult RouteDetails(string id)
        {
            _ = id;
            var route = GetMockRouteData();
            return View(route);
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmCollection(int id)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized();
            }

            var stop = await _db.RouteStops
                .Include(rs => rs.CollectionBin)
                    .ThenInclude(cb => cb.Region)
                .Include(rs => rs.CollectionDetails)
                .Include(rs => rs.RoutePlan)
                    .ThenInclude(rp => rp.RouteAssignment)
                .Where(rs => rs.StopId == id && rs.RoutePlan != null && rs.RoutePlan.RouteAssignment != null)
                .Where(rs => rs.RoutePlan!.RouteAssignment!.AssignedTo == username)
                .FirstOrDefaultAsync();

            if (stop == null)
            {
                TempData["ErrorMessage"] = "Issue not found for this route.";
                return RedirectToAction("ReportIssue");
            }

            var latestCollection = stop.CollectionDetails
                .OrderByDescending(cd => cd.CurrentCollectionDateTime)
                .FirstOrDefault();

            var binId = stop.BinId;
            var binLabel = binId.HasValue ? $"B{binId.Value:000}" : $"Stop {stop.StopId}";

            var viewModel = new CollectionConfirmationVM
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

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmCollection(CollectionConfirmationVM model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // TODO: Save to Database (Paused)
            // Save model.CollectedWeightKg, model.CollectedCategories, etc.

            var username = User.Identity?.Name;
            if (!string.IsNullOrEmpty(username))
            {
                var today = DateTime.Today;
                var routePlan = await _db.RoutePlans
                    .Include(rp => rp.RouteStops)
                        .ThenInclude(rs => rs.CollectionBin)
                    .Include(rp => rp.RouteStops)
                        .ThenInclude(rs => rs.CollectionDetails)
                    .Include(rp => rp.RouteAssignment)
                    .Where(rp => rp.RouteAssignment != null
                              && rp.RouteAssignment.AssignedTo == username
                              && rp.PlannedDate.Date == today)
                    .FirstOrDefaultAsync();

                if (routePlan != null)
                {
                    var orderedStops = routePlan.RouteStops
                        .OrderBy(rs => rs.StopSequence)
                        .ToList();

                    var currentIndex = orderedStops.FindIndex(rs => rs.StopId == model.StopId);
                    if (currentIndex >= 0)
                    {
                        for (var i = currentIndex + 1; i < orderedStops.Count; i++)
                        {
                            var stop = orderedStops[i];
                            var latest = stop.CollectionDetails
                                .OrderByDescending(cd => cd.CurrentCollectionDateTime)
                                .FirstOrDefault();

                            if (latest?.CollectionStatus != "Collected")
                            {
                                model.NextPointId = stop.StopId.ToString();
                                model.NextLocationName = stop.CollectionBin?.LocationName;
                                model.NextAddress = stop.CollectionBin?.LocationAddress;
                                model.NextFillLevel = latest?.BinFillLevel;
                                model.NextPlannedTime = stop.PlannedCollectionTime.DateTime;
                                break;
                            }
                        }
                    }
                }
            }

            // Redirect to Success Page
             return RedirectToAction("CollectionConfirmed", model);
        }

        public IActionResult CollectionConfirmed(CollectionConfirmationVM model)
        {
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ReportIssue(string? search, string? status, string? priority)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized();
            }

            // Fetch bins from today's route assignments
            var today = DateTime.Today;
            var todaysBins = await _db.RouteAssignments
                .Where(ra => ra.AssignedTo == username)
                .SelectMany(ra => ra.RoutePlans)
                .Where(rp => rp.PlannedDate.Date == today)
                .SelectMany(rp => rp.RouteStops)
                .Where(rs => rs.CollectionBin != null)
                .Select(rs => new BinOption
                {
                    BinId = rs.CollectionBin!.BinId,
                    LocationName = rs.CollectionBin.LocationName ?? "",
                    Region = rs.CollectionBin.Region != null ? rs.CollectionBin.Region.RegionName : ""
                })
                .Distinct()
                .ToListAsync();

            var issueStops = await _db.RouteStops
                .Include(rs => rs.CollectionBin)
                    .ThenInclude(cb => cb.Region)
                .Include(rs => rs.CollectionDetails)
                .Include(rs => rs.RoutePlan)
                    .ThenInclude(rp => rp.RouteAssignment)
                .Where(rs => rs.RoutePlan != null
                          && rs.RoutePlan.RouteAssignment != null
                          && rs.RoutePlan.RouteAssignment.AssignedTo == username)
                .OrderByDescending(rs => rs.RoutePlan!.PlannedDate)
                .ToListAsync();

            var issues = issueStops
                .Select(rs => new
                {
                    Stop = rs,
                    IssueLog = !string.IsNullOrWhiteSpace(rs.IssueLog)
                        ? rs.IssueLog
                        : rs.CollectionDetails
                            .OrderByDescending(cd => cd.CurrentCollectionDateTime)
                            .Select(cd => cd.IssueLog)
                            .FirstOrDefault()
                })
                .Where(x => !string.IsNullOrWhiteSpace(x.IssueLog))
                .Select(x => MapIssueLog(x.Stop, x.IssueLog!, username))
                .ToList();

            var filteredIssues = issues.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLowerInvariant();
                filteredIssues = filteredIssues.Where(i =>
                    i.IssueType.ToLowerInvariant().Contains(term) ||
                    i.Description.ToLowerInvariant().Contains(term) ||
                    i.LocationName.ToLowerInvariant().Contains(term) ||
                    i.Address.ToLowerInvariant().Contains(term) ||
                    i.BinId.ToString().Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                filteredIssues = filteredIssues.Where(i => i.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(priority))
            {
                filteredIssues = filteredIssues.Where(i => i.Severity == priority);
            }

            var model = new ReportIssueVM
            {
                AvailableBins = todaysBins,
                Issues = filteredIssues.ToList(),
                TotalIssues = issues.Count,
                OpenIssues = issues.Count(i => i.Status == "Open"),
                InProgressIssues = issues.Count(i => i.Status == "In Progress"),
                ResolvedIssues = issues.Count(i => i.Status == "Resolved"),
                Search = search,
                StatusFilter = status,
                PriorityFilter = priority
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ReportIssue(ReportIssueVM model)
        {
            if (!ModelState.IsValid)
            {
                // Reload bins if validation fails
                var username = User.Identity?.Name;
                if (!string.IsNullOrEmpty(username))
                {
                    var today = DateTime.Today;
                    model.AvailableBins = await _db.RouteAssignments
                        .Where(ra => ra.AssignedTo == username)
                        .SelectMany(ra => ra.RoutePlans)
                        .Where(rp => rp.PlannedDate.Date == today)
                        .SelectMany(rp => rp.RouteStops)
                        .Where(rs => rs.CollectionBin != null)
                        .Select(rs => new BinOption
                        {
                            BinId = rs.CollectionBin!.BinId,
                            LocationName = rs.CollectionBin.LocationName ?? "",
                            Region = rs.CollectionBin.Region != null ? rs.CollectionBin.Region.RegionName : ""
                        })
                        .Distinct()
                        .ToListAsync();

                    var issueStops = await _db.RouteStops
                        .Include(rs => rs.CollectionBin)
                            .ThenInclude(cb => cb.Region)
                        .Include(rs => rs.CollectionDetails)
                        .Include(rs => rs.RoutePlan)
                            .ThenInclude(rp => rp.RouteAssignment)
                        .Where(rs => rs.RoutePlan != null
                                  && rs.RoutePlan.RouteAssignment != null
                                  && rs.RoutePlan.RouteAssignment.AssignedTo == username)
                        .OrderByDescending(rs => rs.RoutePlan!.PlannedDate)
                        .ToListAsync();

                    var issues = issueStops
                        .Select(rs => new
                        {
                            Stop = rs,
                            IssueLog = !string.IsNullOrWhiteSpace(rs.IssueLog)
                                ? rs.IssueLog
                                : rs.CollectionDetails
                                    .OrderByDescending(cd => cd.CurrentCollectionDateTime)
                                    .Select(cd => cd.IssueLog)
                                    .FirstOrDefault()
                        })
                        .Where(x => !string.IsNullOrWhiteSpace(x.IssueLog))
                        .Select(x => MapIssueLog(x.Stop, x.IssueLog!, username))
                        .ToList();

                    model.Issues = issues;
                    model.TotalIssues = issues.Count;
                    model.OpenIssues = issues.Count(i => i.Status == "Open");
                    model.InProgressIssues = issues.Count(i => i.Status == "In Progress");
                    model.ResolvedIssues = issues.Count(i => i.Status == "Resolved");
                }
                return View(model);
            }

            // TODO: Save issue to database
            // Options: Create IssueLog table OR use RouteStop.IssueLog/CollectionDetails.IssueLog JSONB field
            // For now, just store in TempData (placeholder)
            
            TempData["SuccessMessage"] = $"Issue reported for Bin #{model.BinId} - {model.LocationName}";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartIssueWork(int stopId)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized();
            }

            var stop = await _db.RouteStops
                .Include(rs => rs.CollectionDetails)
                .Include(rs => rs.RoutePlan)
                    .ThenInclude(rp => rp.RouteAssignment)
                .Where(rs => rs.StopId == stopId
                          && rs.RoutePlan != null
                          && rs.RoutePlan.RouteAssignment != null
                          && rs.RoutePlan.RouteAssignment.AssignedTo == username)
                .FirstOrDefaultAsync();

            if (stop == null)
            {
                TempData["ErrorMessage"] = "Issue not found for this route.";
                return RedirectToAction("ReportIssue");
            }

            var latestDetail = stop.CollectionDetails
                .Where(cd => !string.IsNullOrWhiteSpace(cd.IssueLog))
                .OrderByDescending(cd => cd.CurrentCollectionDateTime)
                .FirstOrDefault();

            var currentLog = latestDetail?.IssueLog ?? stop.IssueLog ?? string.Empty;
            var currentStatus = ExtractValue(currentLog, "status") ?? InferStatus(currentLog);

            if (currentStatus == "Resolved")
            {
                TempData["SuccessMessage"] = "Issue is already resolved.";
                return RedirectToAction("ReportIssue");
            }

            var newStatus = currentStatus == "In Progress" ? "Resolved" : "In Progress";

            if (latestDetail != null)
            {
                latestDetail.IssueLog = UpdateIssueLogStatus(latestDetail.IssueLog, newStatus);
            }
            else
            {
                stop.IssueLog = UpdateIssueLogStatus(stop.IssueLog, newStatus);
            }

            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = newStatus == "Resolved"
                ? "Issue marked as Resolved."
                : "Issue marked as In Progress.";
            return RedirectToAction("ReportIssue");
        }

        private static IssueLogItem MapIssueLog(RouteStop stop, string issueLog, string reportedBy)
        {
            var issueType = ExtractValue(issueLog, "type") ?? "Other";
            var severity = ExtractValue(issueLog, "severity") ?? InferSeverity(issueLog);
            var status = ExtractValue(issueLog, "status") ?? InferStatus(issueLog);
            var description = ExtractValue(issueLog, "description") ?? issueLog.Trim();

            var latestCollection = stop.CollectionDetails
                .OrderByDescending(cd => cd.CurrentCollectionDateTime)
                .FirstOrDefault();

            return new IssueLogItem
            {
                StopId = stop.StopId,
                BinId = stop.BinId ?? 0,
                LocationName = stop.CollectionBin?.LocationName ?? "Unknown location",
                Address = stop.CollectionBin?.LocationAddress ?? "",
                IssueType = issueType,
                Severity = severity,
                Status = status,
                Description = description,
                ReportedBy = reportedBy,
                ReportedAt = latestCollection?.CurrentCollectionDateTime?.DateTime ?? stop.PlannedCollectionTime.DateTime
            };
        }

        private static string? ExtractValue(string input, string key)
        {
            var match = System.Text.RegularExpressions.Regex.Match(
                input,
                $@"{key}\s*[:=]\s*([^;|\n]+)",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value.Trim() : null;
        }

        private static string UpdateIssueLogStatus(string? input, string status)
        {
            var baseText = string.IsNullOrWhiteSpace(input) ? "" : input;
            var regex = new System.Text.RegularExpressions.Regex(@"status\s*[:=]\s*([^;|\n]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            if (regex.IsMatch(baseText))
            {
                return regex.Replace(baseText, $"status: {status}");
            }

            if (string.IsNullOrWhiteSpace(baseText))
            {
                return $"status: {status}";
            }

            return $"{baseText}; status: {status}";
        }

        private static string InferSeverity(string input)
        {
            var lower = input.ToLowerInvariant();
            if (lower.Contains("high")) return "High";
            if (lower.Contains("low")) return "Low";
            return "Medium";
        }

        private static string InferStatus(string input)
        {
            var lower = input.ToLowerInvariant();
            if (lower.Contains("resolved") || lower.Contains("closed")) return "Resolved";
            if (lower.Contains("progress")) return "In Progress";
            return "Open";
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
                    new CollectionPoint { PointId = "CP-1", LocationName = "Tampines Mall - Loading Bay", Address = "4 Tampines Central 5, Singapore 529510", Latitude = 1.3532, Longitude = 103.9451, DistanceKm = 0.5, EstimatedTimeMins = 5, CurrentFillLevel = 85, Status = "Pending" },
                    new CollectionPoint { PointId = "CP-2", LocationName = "Bedok Mall - Basement 2", Address = "311 New Upper Changi Rd, Singapore 467360", Latitude = 1.3245, Longitude = 103.9301, DistanceKm = 3.2, EstimatedTimeMins = 12, CurrentFillLevel = 60, Status = "Pending" },
                    new CollectionPoint { PointId = "CP-3", LocationName = "Changi City Point", Address = "5 Changi Business Park Central 1, Singapore 486038", Latitude = 1.3346, Longitude = 103.9624, DistanceKm = 5.1, EstimatedTimeMins = 18, CurrentFillLevel = 45, Status = "Pending" },
                    new CollectionPoint { PointId = "CP-4", LocationName = "Pasir Ris White Sands", Address = "1 Pasir Ris Central St 3, Singapore 518457", Latitude = 1.3725, Longitude = 103.9493, DistanceKm = 8.5, EstimatedTimeMins = 25, CurrentFillLevel = 90, Status = "Pending" },
                     // Completed points
                    new CollectionPoint { PointId = "CP-5", LocationName = "Century Square", Address = "2 Tampines Central 5, Singapore 529509", Latitude = 1.3520, Longitude = 103.9442, DistanceKm = 0.2, EstimatedTimeMins = 5, CurrentFillLevel = 10, Status = "Collected", CollectedWeightKg = 15.5, CollectedAt = DateTime.Now.AddMinutes(-30) },
                    new CollectionPoint { PointId = "CP-6", LocationName = "Our Tampines Hub", Address = "1 Tampines Walk, Singapore 528523", Latitude = 1.3538, Longitude = 103.9383, DistanceKm = 0.8, EstimatedTimeMins = 8, CurrentFillLevel = 5, Status = "Collected", CollectedWeightKg = 12.0, CollectedAt = DateTime.Now.AddMinutes(-60) },
                    new CollectionPoint { PointId = "CP-7", LocationName = "Eastpoint Mall", Address = "3 Simei Street 6, Singapore 528833", Latitude = 1.3431, Longitude = 103.9539, DistanceKm = 2.5, EstimatedTimeMins = 15, CurrentFillLevel = 0, Status = "Collected", CollectedWeightKg = 18.2, CollectedAt = DateTime.Now.AddMinutes(-90) },
                    new CollectionPoint { PointId = "CP-8", LocationName = "Jewel Changi Airport", Address = "78 Airport Blvd, Singapore 819666", Latitude = 1.3604, Longitude = 103.9895, DistanceKm = 10.5, EstimatedTimeMins = 30, CurrentFillLevel = 0, Status = "Collected", CollectedWeightKg = 45.0, CollectedAt = DateTime.Now.AddHours(-2) }
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
