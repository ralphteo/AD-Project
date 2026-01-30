
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ADWebApplication.Models;

public class RouteAssignment
{
    [Key]
    public int AssignmentId { get; set; }
    
    // Admin who assigned (username string, not ID)
    [Column("assignedBy")]
    [Required]
    public string AssignedBy { get; set; } = string.Empty;
    
    // Collection Officer assigned to (username string, not ID)
    [Column("assignedTo")]
    [Required]
    public string AssignedTo { get; set; } = string.Empty;
    
    // Navigation property
    public ICollection<RoutePlan> RoutePlans { get; set; } = new List<RoutePlan>();
}