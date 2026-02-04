
using System.ComponentModel.DataAnnotations;

namespace ADWebApplication.Models;

public class CollectionDetails
{
    [Key]
    public int CollectionId { get; set; }

    public int? StopId { get; set; }  // Foreign key
    public RouteStop? RouteStop { get; set; }

    public int? BinId { get; set; }  // Stores bin ID (not a foreign key in this table)
    public DateTimeOffset? LastCollectionDateTime { get; set; }
    public DateTimeOffset? CurrentCollectionDateTime { get; set; }  // Main collection timestamp
    public int BinFillLevel { get; set; } // Percentage
    public String? CollectionStatus { get; set; }
    public String? IssueLog { get; set; }

    // ML prediction fields
    public int? CycleStartMonth { get; set; }
    public int? CycleDurationDays { get; set; }
    public double? AvgDailyFillGrowth { get; set; }
    public double? NextCycleAvgDailyFillGrowth { get; set; }
}