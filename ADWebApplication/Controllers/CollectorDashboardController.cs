using ADWebApplication.Models;
using ADWebApplication.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    }
}