
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ADWebApplication.Models;

public class BinCollectionViewModel
{
    // Based on Route Plan, submit bin collection details for each route stop
    public int Id { get; set; }

    // Selection
    public int? SelectedRoutePlanId { get; set; }
    public int? SelectedRouteStopId { get; set; }

    // Data sources
    public List<RoutePlan> RoutePlans { get; set; } = new();
    public List<RouteStop> RouteStops { get; set; } = new();

    // Submission
    public CollectionDetails CollectionDetails { get; set; } = new();
    public int TimezoneOffset { get; set; }

}