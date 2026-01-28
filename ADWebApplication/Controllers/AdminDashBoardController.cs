using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ADWebApplication.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminDashboardController : Controller
    {
        private readonly IDashboardRepository _dashboardRepository;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(IDashboardRepository dashboardRepository, ILogger<DashboardController> logger)
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
                    PerformanceMetrics = await _dashboardRepository.GetAvgPerformanceMetricsAsync()
                };
                _logger.LogInformation("Admin dashboard data retrieved successfully.");
                return View(viewModel);
            }
            catch (Exception ex)
            {
                // Log the exception (not implemented here for brevity)
                _logger.LogError(ex, "Error retrieving admin dashboard data.");
                return Content($"ERROR: {ex.Message},<br><br>Stack Trace:<br>{ex.StackTrace}");
            }
        }
        public async Task<IActionResult> RefreshKPIs()
        {
            try
            {

            var kpis = await _dashboardRepository.GetAdminDashboardAsync();
            return PartialView("_KPIsPartial", kpis);
            } 
            catch (Exception ex)
            {
            // Log the exception (not implemented here for brevity)
            _logger.LogError(ex, "Error retrieving admin dashboard KPIs.");
            return StatusCode(500, "Internal server error");
            }
        }
    }
}