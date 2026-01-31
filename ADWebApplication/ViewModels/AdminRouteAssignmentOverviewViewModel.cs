using ADWebApplication.Models;

namespace ADWebApplication.ViewModels;

/// <summary>
/// ViewModel for admin dashboard to view all route assignments
/// </summary>
public class AdminRouteAssignmentOverviewViewModel
{
    // Collector assignments
    public List<CollectorAssignmentSummary> CollectorAssignments { get; set; } = new();
    
    // Pagination
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCollectors { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCollectors / (double)PageSize);
    
    // Filters
    public DateTime? FilterDate { get; set; }
    public string? FilterCollectorUsername { get; set; }
    public string? FilterAdminUsername { get; set; }
    public string? FilterStatus { get; set; }
    
    // Dropdown options
    public List<string> AvailableCollectors { get; set; } = new();
    public List<string> AvailableAdmins { get; set; } = new();
    public string[] AvailableStatuses { get; set; } = new[] { "Pending", "Active", "Completed" };
    
    // Helper properties
    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
}

/// <summary>
/// Summary of assignments for a single collector
/// </summary>
public class CollectorAssignmentSummary
{
    public string CollectorUsername { get; set; } = string.Empty;
    public string CollectorFullName { get; set; } = string.Empty;
    
    public List<AssignmentSummaryItem> Assignments { get; set; } = new();
    
    public int TotalAssignments => Assignments.Count;
    public int CompletedAssignments => Assignments.Count(a => a.Status == "Completed");
    public int ActiveAssignments => Assignments.Count(a => a.Status == "Active");
    public int PendingAssignments => Assignments.Count(a => a.Status == "Pending");
}

/// <summary>
/// Individual assignment summary
/// </summary>
public class AssignmentSummaryItem
{
    public int AssignmentId { get; set; }
    public int RouteId { get; set; }
    public string RouteName { get; set; } = string.Empty;
    public DateTime CollectionDate { get; set; }
    public string AssignedBy { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    
    public int TotalStops { get; set; }
    public int CompletedStops { get; set; }
    public int ProgressPercentage => TotalStops > 0 ? (int)((double)CompletedStops / TotalStops * 100) : 0;
}
