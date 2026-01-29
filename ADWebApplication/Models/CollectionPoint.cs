using System;

namespace ADWebApplication.Models
{
    public class CollectionPoint
    {
        public string PointId { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty; // e.g. "Downtown Plaza - Bin #45"
        public string Address { get; set; } = string.Empty;
        public double DistanceKm { get; set; }
        public int EstimatedTimeMins { get; set; }
        public int CurrentFillLevel { get; set; } // Percentage
        public string Status { get; set; } = string.Empty; // "Pending", "Collected", "Issue"
        public double CollectedWeightKg { get; set; }
        public DateTime? CollectedAt { get; set; }
    }
}