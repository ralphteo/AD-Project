using ADWebApplication.Models;

namespace ADWebApplication.ViewModels;

/// <summary>
/// ViewModel for displaying and searching route assignments
/// </summary>
public class RouteAssignmentSearchViewModel
{
    // Results
    public List<RouteAssignmentDisplayItem> Assignments { get; set; } = new();

    // Pagination
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalItems { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalItems / (double)PageSize);

    // Search filters (to maintain state)
    public string? SearchTerm { get; set; }
    public int? SelectedRegionId { get; set; }
    public DateTime? SelectedDate { get; set; }
    public string? SelectedStatus { get; set; }

    // Dropdown options
    public List<Region> AvailableRegions { get; set; } = new();
    public string[] AvailableStatuses { get; set; } = new[] { "Pending", "Scheduled", "Active" };

    // Helper properties
    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
}

/// <summary>
/// Individual route assignment display item
/// </summary>
public class RouteAssignmentDisplayItem
{
    public int AssignmentId { get; set; }
    public string AssignedBy { get; set; } = string.Empty;
    public string AssignedTo { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";  // Pending, Active, Completed

    // Route information (from RoutePlan)
    public int RouteId { get; set; }
    public DateTime PlannedDate { get; set; }  // From RoutePlan.PlannedDate (collection date)
    public string? RouteStatus { get; set; }   // From RoutePlan.RouteStatus
    public string? RegionName { get; set; }

    // Progress tracking
    public int TotalStops { get; set; }
    public int CompletedStops { get; set; }
    public int ProgressPercentage => TotalStops > 0 ? (int)((double)CompletedStops / TotalStops * 100) : 0;

    // Display helpers
    public string RouteDisplayName => $"Route #{RouteId}";
}
