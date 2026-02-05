using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ADWebApplication.Services;
using ADWebApplication.Models.ViewModels;

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
    public async Task<IActionResult>Index()
    {
        var stops = await _routePlanningService.PlanRouteAsync();
        var viewModel = new RoutePlanningViewModel
        {
            AllStops = stops,
            CollectionDate = DateTime.Now.AddDays(1).ToString("dddd, dd MMMM")
        };

        return View(viewModel);

    }
}