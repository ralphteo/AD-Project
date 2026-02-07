using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using ADWebApplication.Data;
using ADWebApplication.Services;
using ADWebApplication.Models.DTOs;
using ADWebApplication.Models.ViewModels;

namespace ADWebApplication.Controllers.WebAdmin;

[Authorize(Roles = "Admin")]
[Route("Admin/RoutePlanning")]
public class AdminRoutePlanningController : Controller
{
    private readonly IRoutePlanningService _routePlanningService;
    private readonly IRouteAssignmentService _routeAssignmentService;
    private readonly In5niteDbContext _db;

    public AdminRoutePlanningController(
        IRoutePlanningService routePlanningService,
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
        var date = DateTime.Today;

        var saved = await _routePlanningService.GetPlannedRoutesAsync(date);

        List<UiRouteStopDto> uiStops;

        if (saved.Count > 0)
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

    [HttpPost("assign-all")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignAllRoutes(AssignAllRoutesRequestDto req)
    {
        if (!ModelState.IsValid)
        {
            return RedirectToAction(nameof(Index));
        }

        var date = DateTime.Today;
        var admin = User.Identity?.Name ?? "Admin";

        var savedStops = await _routePlanningService.GetPlannedRoutesAsync(date);

        List<UiRouteStopDto> uiStops;

        if (savedStops.Count > 0)
        {
            uiStops = savedStops.Select(s => new UiRouteStopDto
            {
                RouteKey = s.RouteKey,
                BinId = s.BinId,
                Latitude = s.Latitude,
                Longitude = s.Longitude,
                StopNumber = s.StopNumber,
                AssignedOfficerName = s.AssignedOfficerName,
                IsHighPriority = false
            }).ToList();
        }
        else
        {
            var vrpStops = await _routePlanningService.PlanRouteAsync();
            
            uiStops = vrpStops.Select(s => new UiRouteStopDto
            {
                RouteKey = s.AssignedCO,
                BinId = s.BinId,
                Latitude = s.Latitude ?? 0,
                Longitude = s.Longitude ?? 0,
                StopNumber = s.StopNumber,
                IsHighPriority = s.IsHighPriority,
                AssignedOfficerName = s.AssignedOfficerName
            }).ToList();
        }

        var assignments = req.Assignments
            .Where(a => !string.IsNullOrEmpty(a.OfficerUsername))
            .ToDictionary(a => a.RouteKey, a => a.OfficerUsername);

        await _routeAssignmentService.SavePlannedRoutesAsync(
            uiStops,
            assignments,
            admin,
            date
        );

        TempData["SuccessMessage"] = $"All {assignments.Count} route(s) assigned successfully.";

        return RedirectToAction(nameof(Index));
    }
}