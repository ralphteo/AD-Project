using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ADWebApplication.Data.Repository;
using ADWebApplication.Models.DTOs;
using ADWebApplication.Services;
using System.Runtime.CompilerServices;
using System.Diagnostics.Tracing;
using System.Threading.Tasks.Sources;
using System.Text;
using System.Runtime.Serialization.Json;

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

        private readonly RoutePlanningService _routePlanningService;
        private readonly IBinPredictionService _binPredictionService;


        public AdminDashboardController(IDashboardRepository dashboardRepository, ILogger<AdminDashboardController> logger, IBinPredictionService binPredictionService, RoutePlanningService routePlanningService)
        {
            _dashboardRepository = dashboardRepository;
            _logger = logger;
            _binPredictionService = binPredictionService;
            _routePlanningService = routePlanningService;
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

                // var highRisk = await _dashboardRepository.GetHighRiskUnscheduledCountAsync();
                // _logger.LogInformation("High risk count: {Count}", highRisk);

                var binCounts = await _dashboardRepository.GetBinCountsAsync();
                _logger.LogInformation("Bin counts retrieved: {Active}/{Total}", binCounts.ActiveBins, binCounts.TotalBins);

                var predictionVm = await _binPredictionService
                    .BuildBinPredictionsPageAsync(
                        page: 1,
                        sort: "DaysToThreshold",
                        sortDir: "asc",
                        risk: "High",
                        timeframe: "3"
                    );

                var highRisk = predictionVm.HighRiskUnscheduledCount;
                var mlRefreshCount = predictionVm.NewCycleDetectedCount;

                var alerts = new List<AdminAlertDto>();

                if (highRisk > 0)
                {
                    alerts.Add(new AdminAlertDto
                    {
                        Type = "HighRisk",
                        Title = "High overflow risk predicted",
                        Message = $"{highRisk} bins are high-risk and not yet scheduled for collection",
                        LinkText = "View Bin Predictions",
                        LinkUrl = Url.Action("Index", "AdminBinPredictions") ?? ""
                    });
                }

                if (mlRefreshCount > 0)
                {
                    alerts.Add(new AdminAlertDto
                    {
                        Type = "MLRefresh",
                        Title = "Bin Predictions need refresh",
                        Message = $"{mlRefreshCount} bins have new collection cycles detected",
                        LinkText = "Refresh Predictions",
                        LinkUrl = Url.Action("Index", "AdminBinPredictions") ?? ""
                    });
                }

                var viewModel = new AdminDashboardViewModel
                {
                    KPIs = kpis,
                    CollectionTrends = trends,
                    CategoryBreakdowns = categories,
                    PerformanceMetrics = performance,
                    HighRiskUnscheduledCount = highRisk,
                    ActiveBinsCount = binCounts.ActiveBins,
                    TotalBinsCount = binCounts.TotalBins,
                    Alerts = alerts
                };


                viewModel.Alerts = alerts;

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
    }
}

