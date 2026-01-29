using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ADWebApplication.Data.Repository;
using ADWebApplication.Models.DTOs;
using System.Linq.Expressions;

namespace ADWebApplication.Controllers
{
    // [Authorize(Roles = "Admin")]
    // public class AdminDashboardController : Controller
    // {
    //     private readonly IDashboardRepository _dashboardRepository;
    //     private readonly ILogger<AdminDashboardController> _logger;

    //     public AdminDashboardController(IDashboardRepository dashboardRepository, ILogger<AdminDashboardController> logger)
    //     {
    //         _dashboardRepository = dashboardRepository;
    //         _logger = logger;
    //     }

    //     public async Task<IActionResult> Index()
    //     {
    //         try
    //         {
    //             var viewModel = new AdminDashboardViewModel
    //             {
    //                 KPIs = await _dashboardRepository.GetAdminDashboardAsync(),
    //                 CollectionTrends = await _dashboardRepository.GetCollectionTrendsAsync(),
    //                 CategoryBreakdowns = await _dashboardRepository.GetCategoryBreakdownAsync(),
    //                 PerformanceMetrics = await _dashboardRepository.GetAvgPerformanceMetricsAsync()
    //             };
    //             _logger.LogInformation("Admin dashboard data retrieved successfully.");
    //             return View(viewModel);
    //         }
    //         catch (Exception ex)
    //         {
    //             // Log the exception (not implemented here for brevity)
    //             _logger.LogError(ex, "Error retrieving admin dashboard data.");
    //             return Content($"ERROR: {ex.Message},<br><br>Stack Trace:<br>{ex.StackTrace}");
    //         }
    //     }
    //     public async Task<IActionResult> RefreshKPIs()
    //     {
    //         try
    //         {

    //         var kpis = await _dashboardRepository.GetAdminDashboardAsync();
    //         return PartialView("_KPIsPartial", kpis);
    //         } 
    //         catch (Exception ex)
    //         {
    //         // Log the exception (not implemented here for brevity)
    //         _logger.LogError(ex, "Error retrieving admin dashboard KPIs.");
    //         return StatusCode(500, "Internal server error");
    //         }
    //     }
    // }

    [Authorize(Roles = "Admin")]
    public class AdminDashboardController : Controller
    {
        public IActionResult Index()
        {
            var viewModel = new AdminDashboardViewModel
            {
                KPIs = new DashboardKPIs
                {
                    TotalUsers = 1280,
                    UserGrowthPercent = 4.2m,
                    TotalCollections = 312,
                    CollectionGrowthPercent = 6.8m,
                    TotalWeightRecycled = 1450.5m,
                    WeightGrowthPercent = 3.1m,
                    AvgBinFillRate = 72.4m,
                    BinFillRateChange = -1.5m
                },

                CollectionTrends = new List<CollectionTrend>
                {
                    new() { Month = "2025-10", Collections = 180, Weight = 820 },
                    new() { Month = "2025-11", Collections = 220, Weight = 940 },
                    new() { Month = "2025-12", Collections = 260, Weight = 1120 },
                    new() { Month = "2026-01", Collections = 312, Weight = 1450 }
                },

                CategoryBreakdowns = new List<CategoryBreakdown>
                {
                    new() { Category = "Computers", Value = 35, Color = "#3b82f6" },
                    new() { Category = "Mobile Devices", Value = 28, Color = "#10b981" },
                    new() { Category = "Home Appliances", Value = 22, Color = "#f59e0b" },
                    new() { Category = "Accessories", Value = 15, Color = "#8b5cf6" }
                },

                PerformanceMetrics = new List<AvgPerformance>
                {
                    new() { Area = "Central", Collections = 120, Participation = 65.5m },
                    new() { Area = "North", Collections = 80, Participation = 52.3m },
                    new() { Area = "East", Collections = 65, Participation = 48.1m },
                    new() { Area = "West", Collections = 47, Participation = 39.8m }
                }
            };

            return View(viewModel);
        }
    }
}
