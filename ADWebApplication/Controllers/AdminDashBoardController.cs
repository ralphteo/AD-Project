using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ADWebApplication.Data.Repository;
using ADWebApplication.Models.DTOs;

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
        private readonly IDashboardRepository _dashboardRepository;
        private readonly ILogger<AdminDashboardController> _logger;

        public AdminDashboardController(IDashboardRepository dashboardRepository, ILogger<AdminDashboardController> logger)
        {
            _dashboardRepository = dashboardRepository;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var viewModel = new AdminDashboardViewModel
                {
                    KPIs = await _dashboardRepository.GetAdminDashboardAsync(),
                    CollectionTrends = await _dashboardRepository.GetCollectionTrendsAsync(),
                    CategoryBreakdowns = await _dashboardRepository.GetCategoryBreakdownAsync(),
                    PerformanceMetrics = await _dashboardRepository.GetAvgPerformanceMetricsAsync(),
                    HighRiskUnscheduledCount = await _dashboardRepository.GetHighRiskUnscheduledCountAsync()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving admin dashboard data.");
                return View(new AdminDashboardViewModel
                {
                    KPIs = new DashboardKPIs(),
                    CollectionTrends = new List<CollectionTrend>(),
                    CategoryBreakdowns = new List<CategoryBreakdown>(),
                    PerformanceMetrics = new List<AvgPerformance>(),
                    HighRiskUnscheduledCount = 0
                });
            }
        }
    }
}
