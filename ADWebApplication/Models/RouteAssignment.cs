
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

    [Column("assignedDateTime")]
    public DateTime AssignedDateTime { get; set; }

    // Navigation property
    public ICollection<RoutePlan> RoutePlans { get; set; } = new List<RoutePlan>();

    // Navigation property to Employee (Collection Officer)
    [NotMapped]
    public Employee? AssignedToEmployee { get; set; }

    // Navigation property to "assigned by" Admin
    [NotMapped]
    public Employee? AssignedByEmployee { get; set; }
}