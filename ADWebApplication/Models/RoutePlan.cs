
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ADWebApplication.Models;

public class RoutePlan
{
    [Key]
    public int RouteId { get; set; }
    
    public int AssignmentId { get; set; }
    [ForeignKey("AssignmentId")]
    public RouteAssignment? RouteAssignment { get; set; }
    
    public DateTime? PlannedDate { get; set; }
    public string? RouteStatus { get; set; }
    
    [Column("generatedBy")]
    public string? GeneratedBy { get; set; }  // Username, not ID
    
    public ICollection<RouteStop> RouteStops { get; set; } = new List<RouteStop>();
}
