using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ADWebApplication.Data.Repository;
using ADWebApplication.Models.DTOs;
using ADWebApplication.Services;

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

    [Route("AdminDashBoard")]
    [Authorize(Roles = "Admin")]
    public class AdminDashboardController : Controller
    {
        private readonly IDashboardRepository _dashboardRepository;
        private readonly ILogger<AdminDashboardController> _logger;
        private readonly BinPredictionService _binPredictionService;


        public AdminDashboardController(IDashboardRepository dashboardRepository, ILogger<AdminDashboardController> logger, BinPredictionService binPredictionService)
        {
            _dashboardRepository = dashboardRepository;
            _logger = logger;
            _binPredictionService = binPredictionService;
        }

        /* public async Task<IActionResult> Index()
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
        } */

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            try
            {
                _logger.LogInformation("Starting dashboard data retrieval...");
        
                var kpis = await _dashboardRepository.GetAdminDashboardAsync();
                _logger.LogInformation("KPIs retrieved: {Users} users", kpis.TotalUsers);
        
                var trends = await _dashboardRepository.GetCollectionTrendsAsync();
                _logger.LogInformation("Trends retrieved: {Count} records", trends.Count);
        
                var categories = await _dashboardRepository.GetCategoryBreakdownAsync();
                _logger.LogInformation("Categories retrieved: {Count} records", categories.Count);
        
                var performance = await _dashboardRepository.GetAvgPerformanceMetricsAsync();
                _logger.LogInformation("Performance retrieved: {Count} records", performance.Count);
        
                var highRisk = await _dashboardRepository.GetHighRiskUnscheduledCountAsync();
                _logger.LogInformation("High risk count: {Count}", highRisk);

                var binCounts = await _dashboardRepository.GetBinCountsAsync();
                _logger.LogInformation("Bin counts retrieved: {Active}/{Total}", binCounts.ActiveBins, binCounts.TotalBins);
        
                var viewModel = new AdminDashboardViewModel
                {
                    KPIs = kpis,
                    CollectionTrends = trends,
                    CategoryBreakdowns = categories,
                    PerformanceMetrics = performance,
                    HighRiskUnscheduledCount = highRisk,
                    ActiveBinsCount = binCounts.ActiveBins,
                    TotalBinsCount = binCounts.TotalBins
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
                    HighRiskUnscheduledCount = 0,
                    ActiveBinsCount = 0,
                    TotalBinsCount = 0
                });
            }
        }

        [HttpGet("BinPredictions")]
        public async Task<IActionResult> BinPredictions(int page = 1, string sort = "Days", string sortDir = "asc", string risk = "All", string timeframe = "All")
        {
            var viewModel = await _binPredictionService.BuildBinPredictionsPageAsync(page, sort, sortDir, risk, timeframe);

            return View(viewModel);
        }
    }

}
