namespace ADWebApplication.Models.DTOs
{
    public class UiRouteStopDto
    {
        public int RouteKey { get; set; }
        public int? BinId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int StopNumber { get; set; }
        public bool IsHighPriority { get; set; }
        public string? AssignedOfficerName { get; set; }
    }
}
