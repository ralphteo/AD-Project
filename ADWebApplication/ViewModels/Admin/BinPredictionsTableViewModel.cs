namespace ADWebApplication.Models.ViewModels.BinPredictions;

public class BinPredictionsTableViewModel
{
    public int BinId { get; set; }
    public string Region { get; set; } = "";
    public DateTimeOffset LastCollectionDateTime { get; set; }
    public double? PredictedNextAvgDailyGrowth { get; set; }
    public double EstimatedFillToday { get; set; }
    public int? EstimatedDaysToThreshold { get; set; }
    public bool AutoSelected { get; set; }
    public string? RiskLevel { get; set; } = "";
    public string PlanningStatus { get; set; } = "Not Scheduled";
    public string? RouteId { get; set; }
    public bool IsNewCycleDetected { get; set; }
    public bool NeedsPredictionRefresh { get; set; }
    public bool CollectionDone { get; set; }
    public bool IsActualFillLevel { get; set; }
    public string CycleStartDisplay =>
        $"Cycle started: {LastCollectionDateTime:dd MMM yyyy}";
}