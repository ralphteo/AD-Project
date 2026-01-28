
namespace ADWebApplication.Models;

public class RouteAssignment
{
    public int AssignmentId { get; set; }
    public Employee? Employee { get; set; } // Assigned By Admin Employee
    public int AssignedTo { get; set; } // Assigned To Collection Officer Employee Id
    public DateTimeOffset AssignedDateTime { get; set; }
}