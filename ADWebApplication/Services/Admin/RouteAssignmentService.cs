using Google.OrTools.ConstraintSolver;
using ADWebApplication.Data;
using ADWebApplication.Models;
using ADWebApplication.Models.DTOs;
using ADWebApplication.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ADWebApplication.Services
{
    public class RouteAssignmentService : IRouteAssignmentService
{
    private readonly In5niteDbContext _db;

    public RouteAssignmentService(In5niteDbContext db)
    {
        _db = db;
    }

    public async Task SavePlannedRouteAsync(List<RoutePlanDto> allStops, int routeKey, string officerUsername, string adminUsername, DateTime plannedDate)
    {
        var routeStops = allStops
            .Where(s => s.AssignedCO == routeKey)
            .OrderBy(s => s.StopNumber)
            .ToList();

        if (!routeStops.Any())
            throw new InvalidOperationException("No stops found for route.");

        var assignment = new RouteAssignment
        {
            AssignedTo = officerUsername,
            AssignedBy = adminUsername,
            AssignedDateTime = DateTime.UtcNow
        };
        _db.RouteAssignments.Add(assignment);
        await _db.SaveChangesAsync();

        var routePlan = new RoutePlan
        {
            AssignmentId = assignment.AssignmentId,
            PlannedDate = plannedDate,
            RouteStatus = "Assigned",
            GeneratedBy = adminUsername
        };
        _db.RoutePlans.Add(routePlan);
        await _db.SaveChangesAsync();

        foreach (var stop in routeStops)
        {
            _db.RouteStops.Add(new RouteStop
            {
                RouteId = routePlan.RouteId,
                BinId = stop.BinId,
                StopSequence = stop.StopNumber,
                PlannedCollectionTime = DateTimeOffset.UtcNow
            });
        }

        await _db.SaveChangesAsync();
    }
}
}