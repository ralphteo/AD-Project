namespace ADWebApplication.Models.DTOs;

public class MobileLookupBinDto
{
    public int BinId { get; set; }
    public int? RegionId { get; set; }
    public string? LocationName { get; set; }
    public string? LocationAddress { get; set; }
    public string? BinStatus { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string PredictedStatus { get; set; } = "Unknown";
    public double? EstimatedFillLevel { get; set; }
    public string? RiskLevel { get; set; }
    public int? DaysToFull { get; set; }
    public bool IsNearlyFull { get; set; }
}
