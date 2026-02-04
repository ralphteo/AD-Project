
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace ADWebApplication.Models;

public class RouteStop
{
    [Key]
    public int StopId { get; set; }
    
    public int? RouteId { get; set; }  // Foreign key
    public RoutePlan? RoutePlan { get; set; }
    
    public int? BinId { get; set; }  // Foreign key
    public CollectionBin? CollectionBin { get; set; }
    
    public int StopSequence { get; set; }
    public DateTimeOffset PlannedCollectionTime  { get; set; }
    public String? IssueLog { get; set; }
    
    // Navigation property: one RouteStop can have multiple collection details (history)
    public ICollection<CollectionDetails> CollectionDetails { get; set; } = new List<CollectionDetails>();
}