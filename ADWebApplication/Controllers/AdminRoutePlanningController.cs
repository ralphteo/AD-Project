using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ADWebApplication.Services;

[Authorize(Roles = "Admin")]
[Route("Admin/RoutePlanning")]
public class AdminRoutePlanningController : Controller
{
    private readonly RoutePlanningService _routePlanningService;

    public AdminRoutePlanningController(RoutePlanningService routePlanningService)
    {
        _routePlanningService = routePlanningService;
    }

    [HttpGet("")]        
    public async Task<IActionResult> Index(string? date = null)
    {
        // 1. Handle Date Logic
        if (!DateTime.TryParse(date, out DateTime parsedDate))
        {
            parsedDate = DateTime.Today;
        }

            var viewModel = await _routePlanningService
            .GetRoutePlanningDetailsAsync(parsedDate);

            return View(viewModel);
        }
}