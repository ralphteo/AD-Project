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

    public async Task SavePlannedRoutesAsync(List<UiRouteStopDto> allStops, Dictionary<int, string> routeAssignments, string adminUsername, DateTime date)
    {
        var existingPlans = await _db.RoutePlans
            .Include(r => r.RouteStops)
            .Include(r => r.RouteAssignment)
            .Where(r => r.PlannedDate == date)
            .ToListAsync();

        if (existingPlans.Count == 0)
        {
            var grouped = allStops.GroupBy(s => s.RouteKey);

            foreach (var g in grouped)
            {
                var route = new RoutePlan
                {
                    PlannedDate = date,
                    GeneratedBy = adminUsername,
                    RouteStatus = "Scheduled",
                    RouteStops = g.Select(s => new RouteStop
                    {
                        BinId = s.BinId,
                        StopSequence = s.StopNumber,
                        PlannedCollectionTime = new DateTimeOffset(date, TimeZoneInfo.Local.GetUtcOffset(date))
                    }).ToList()
                };

                if (routeAssignments.TryGetValue(g.Key, out var officer))
                {
                    route.RouteAssignment = new RouteAssignment
                    {
                        AssignedTo = officer,
                        AssignedBy = adminUsername
                    };
                }

                _db.RoutePlans.Add(route);
            }
        }
        else
        {
            foreach (var plan in existingPlans)
            {
                if (routeAssignments.TryGetValue(plan.RouteId, out var officer))
                {
                    if (plan.RouteAssignment == null)
                    {
                        plan.RouteAssignment = new RouteAssignment
                        {
                            AssignedTo = officer,
                            AssignedBy = adminUsername
                        };
                    }
                    else
                    {
                        plan.RouteAssignment.AssignedTo = officer;
                    }
                }
            }
        }

        await _db.SaveChangesAsync();
    }
}
}