namespace ADWebApplication.Models.DTOs
{
    public class AssignAllRoutesRequestDto
    {
        public List<RouteAssignmentDto> Assignments { get; set; } = new();
    }

    public class RouteAssignmentDto
    {
        public int RouteKey { get; set; }
        public string OfficerUsername { get; set; } = "";
    }
}
