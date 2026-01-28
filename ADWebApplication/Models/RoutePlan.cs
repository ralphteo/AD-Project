
namespace ADWebApplication.Models;

public class RoutePlan
{
    public int RoutetId { get; set; }
    public RouteAssignment? RouteAssignment { get; set; }
    public DateTimeOffset PlannedDateTime { get; set; }
    public String? RouteStatus { get; set; }
}