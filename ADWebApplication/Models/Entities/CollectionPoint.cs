using System;

namespace ADWebApplication.Models
{
    public class CollectionPoint
    {
        public int StopId { get; set; }
        public string PointId { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty; // e.g. "Downtown Plaza - Bin #45"
        public int? BinId { get; set; }
        public string Address { get; set; } = string.Empty;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public double DistanceKm { get; set; }
        public int EstimatedTimeMins { get; set; }
        public DateTime? PlannedCollectionTime { get; set; }
        public int CurrentFillLevel { get; set; } // Percentage
        public string Status { get; set; } = string.Empty; // "Pending", "Collected", "Issue"
        public DateTime? CollectedAt { get; set; }
    }
}
