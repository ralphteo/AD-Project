using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using ADWebApplication.Data;
using ADWebApplication.Services;
using ADWebApplication.Models.DTOs;
using ADWebApplication.Models.ViewModels;

[Authorize(Roles = "Admin")]
[Route("Admin/RoutePlanning")]
public class AdminRoutePlanningController : Controller
{
    private readonly RoutePlanningService _routePlanningService;
    private readonly IRouteAssignmentService _routeAssignmentService;
    private readonly In5niteDbContext _db;

    public AdminRoutePlanningController(
        RoutePlanningService routePlanningService,
        IRouteAssignmentService routeAssignmentService,
        In5niteDbContext db)
    {
        _routePlanningService = routePlanningService;
        _routeAssignmentService = routeAssignmentService;
        _db = db;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var date = DateTime.Today.AddDays(1);

        var saved = await _routePlanningService.GetPlannedRoutesAsync(date);

        List<UiRouteStopDto> uiStops;

        if (saved.Any())
        {
            uiStops = saved.Select(s => new UiRouteStopDto
            {
                RouteKey = s.RouteKey,
                BinId = s.BinId,
                Latitude = s.Latitude,
                Longitude = s.Longitude,
                StopNumber = s.StopNumber,
                AssignedOfficerName = s.AssignedOfficerName,
                IsHighPriority = true
            }).ToList();
        }
        else
        {
            var vrp = await _routePlanningService.PlanRouteAsync();

            uiStops = vrp.Select(s => new UiRouteStopDto
            {
                RouteKey = s.AssignedCO,
                BinId = s.BinId,
                Latitude = s.Latitude ?? 0,
                Longitude = s.Longitude ?? 0,
                StopNumber = s.StopNumber,
                IsHighPriority = s.IsHighPriority
            }).ToList();
        }

        var routes = uiStops
            .GroupBy(s => s.RouteKey)
            .Select(g => new RouteGroupViewModel
            {
                RouteKey = g.Key,
                RouteName = $"Route {g.Key}",
                AssignedOfficerName = g.First().AssignedOfficerName,
                Stops = g.OrderBy(s => s.StopNumber).ToList()
            })
            .ToList();

        var officers = await _db.Employees
            .Where(e => e.RoleId == 3)
            .Select(e => new CollectionOfficerDto
            {
                Username = e.Username,
                FullName = e.FullName
            })
            .ToListAsync();

        var viewModel = new RoutePlanningViewModel
        {
            Routes = routes,
            AllStops = uiStops,
            AvailableOfficers = officers,
            CollectionDate = date.ToString("dddd, dd MMMM")
        };

        return View(viewModel);
    }

    [HttpPost("assign")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignRoute(AssignRouteRequestDto req)
    {
        var adminUsername = User.Identity?.Name ?? "Admin";
        var date = DateTime.Today.AddDays(1);

        var vrpStops = await _routePlanningService.PlanRouteAsync();

        await _routeAssignmentService.SavePlannedRouteAsync(
            vrpStops,
            req.RouteKey,
            req.OfficerUsername,
            adminUsername,
            date
        );

        return RedirectToAction(nameof(Index));
    }
}