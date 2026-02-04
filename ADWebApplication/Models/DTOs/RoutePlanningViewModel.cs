namespace ADWebApplication.Models.DTOs
{
    public class RoutePlanningViewModel
    {
        public DateTime SelectedDate { get; set; }
        public string SelectedVehicleId { get; set; }
        public int TotalStops { get; set; }
        public double EstimatedTotalDistance { get; set; }
        public List<RouteStopDto> Stops { get; set; } = new List<RouteStopDto>();
    }

    public class RouteStopDto
    {
        public int SequenceOrder { get; set; }
        public string LocationName { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string BinType { get; set; }
        public string Status { get; set; }
        public bool IsHighRisk { get; set; }
    }
}