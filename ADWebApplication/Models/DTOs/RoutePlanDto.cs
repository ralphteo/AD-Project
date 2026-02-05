namespace ADWebApplication.Models.DTOs
{
    public class RoutePlanDto
    {
        public int BinId {get; set;}
        
        public double? Latitude { get; set; }
        
        public double? Longitude { get; set; }
        
        public string BinStatus { get; set; } = "Active";

        public bool IsHighPriority {get; set;}

        //fields for web display
        public int AssignedCO {get; set;}
        public int StopNumber {get; set;}

    }
}