using ADWebApplication.Models;

namespace ADWebApplication.ViewModels;

/// <summary>
/// ViewModel for displaying detailed route assignment information
/// </summary>
public class RouteAssignmentDetailViewModel
{
    // Assignment info
    public int AssignmentId { get; set; }
    public string AssignedBy { get; set; } = string.Empty;
    public string AssignedTo { get; set; } = string.Empty;
    
    // Route info (from RoutePlan)
    public int RouteId { get; set; }
    public DateTime PlannedDate { get; set; }      // Collection date from RoutePlan
    public string? RouteStatus { get; set; }       // Status from RoutePlan
    
    // Stops
    public List<RouteStopDisplayItem> RouteStops { get; set; } = new();
    
    // Progress
    public int TotalStops => RouteStops.Count;
    public int CompletedStops => RouteStops.Count(s => s.IsCollected);
    public int PendingStops => RouteStops.Count(s => !s.IsCollected);
    public int ProgressPercentage => TotalStops > 0 ? (int)((double)CompletedStops / TotalStops * 100) : 0;
    
    // Display helpers
    public string RouteDisplayName => $"Route #{RouteId}";
}

/// <summary>
/// Individual route stop display item
/// </summary>
public class RouteStopDisplayItem
{
    public int StopId { get; set; }
    public int StopSequence { get; set; }
    public DateTime PlannedCollectionTime { get; set; }
    
    // Bin information
    public int BinId { get; set; }
    public string? LocationName { get; set; }
    public string? RegionName { get; set; }
    
    // Collection status (determined from CollectionDetails)
    public bool IsCollected { get; set; }
    public DateTime? CollectedAt { get; set; }
    public string? CollectionStatus { get; set; }
    public int? BinFillLevel { get; set; }
}

/// <summary>
/// ViewModel for displaying next pending stops (sequence-based)
/// </summary>
public class NextStopsViewModel
{
    public int AssignmentId { get; set; }
    public int RouteId { get; set; }
    public DateTime PlannedDate { get; set; }  // From RoutePlan.PlannedDate
    
    public List<RouteStopDisplayItem> NextStops { get; set; } = new();
    public int TotalPendingStops { get; set; }
    
    // Display helper
    public string RouteDisplayName => $"Route #{RouteId}";
}
