using System;
using System.Collections.Generic;
using System.Linq;

namespace ADWebApplication.Models
{
    public class CollectorRoute
    {
        public string RouteId { get; set; } = string.Empty;
        public string RouteName { get; set; } = string.Empty; // e.g. "Route A-12"
        public string Zone { get; set; } = string.Empty; // e.g. "Downtown District"
        public DateTime ScheduledDate { get; set; }
        public string Status { get; set; } = string.Empty; // "Scheduled", "In Progress", "Completed"
        public List<CollectionPoint> CollectionPoints { get; set; } = new();
        
        // Computed Properties for Dashboard
        public int TotalPoints => CollectionPoints.Count;
        public int CompletedPoints => CollectionPoints.Count(p => p.Status == "Collected");
        public double TotalWeightCollected => CollectionPoints.Sum(p => p.CollectedWeightKg);
    }
}