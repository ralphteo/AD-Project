using ADWebApplication.Models.DTOs;
using ADWebApplication.Services;

namespace ADWebApplication.Models.ViewModels
{
    public class RouteGroupViewModel
    {
        public int RouteKey { get; set; }
        public string RouteName { get; set; } = "";
        public List<UiRouteStopDto> Stops { get; set; } = new();
        public string? AssignedOfficerName { get; set; }
    }
}
