using ADWebApplication.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using ADWebApplication.Data.Repository;


namespace ADWebApplication.Controllers.API
{
   
    [ApiController]
     [Route("api/[controller]")]
    public class DashApiController : ControllerBase
    {
        private readonly IDashboardRepository _dashboardRepository;
        private readonly ILogger<DashApiController> _logger;

        public DashApiController(IDashboardRepository dashboardRepository, ILogger<DashApiController> logger)
        {
            
            _dashboardRepository = dashboardRepository;
            _logger = logger;
        }

        [HttpGet("kpis")]
        public async Task<ActionResult<DashboardKPIs>> GetKPIs([FromQuery] DateTime? forMonth = null)
        {
            try
            {
                var kpis = await _dashboardRepository.GetAdminDashboardAsync(forMonth);
                return Ok(kpis);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching KPIs for month {ForMonth}", forMonth);
                return StatusCode(500, "Internal server error");
            }
        }
        [HttpGet("categories")]

        public async Task<ActionResult<List<CategoryBreakdown>>> GetCategoryBreakdown()
        {
            try
            {
                var breakdowns = await _dashboardRepository.GetCategoryBreakdownAsync();
                return Ok(breakdowns);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching category breakdown");
                return StatusCode(500, "Internal server error");
            }
        }
        [HttpGet("areas")]
        public async Task<ActionResult<List<AvgPerformance>>> GetAreaBreakdown()
        {
            try
            {
                var areas = await _dashboardRepository.GetAvgPerformanceMetricsAsync();
                return Ok(areas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching area breakdown");
                return StatusCode(500, "Internal server error");
            }
        }
        [HttpGet("dashboard")]
        public async Task<ActionResult<AdminDashboardViewModel>> GetAllDashboard()
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
                return Ok(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching complete dashboard data");
                return StatusCode(500, "Internal server error");
        }
        
    }
}
}