namespace ADWebApplication.Models.ViewModels.BinPredictions;

public class BinPredictionsTableViewModel
{
    public int BinId { get; set; }
    public string Region { get; set; } = "";
    public DateTimeOffset LastCollectionDateTime { get; set; }
    public double PredictedNextAvgDailyGrowth { get; set; }
    public double EstimatedFillToday { get; set; }
    public int EstimatedDaysToThreshold { get; set; }
    public bool AutoSelected => EstimatedDaysToThreshold <= 3;

    public string RiskLevel =>
        EstimatedDaysToThreshold <= 3 ? "High" :
        EstimatedDaysToThreshold <= 7 ? "Medium" :
        "Low";

    public string PlanningStatus { get; set; } = "Not Scheduled";
    public string? RouteId { get; set; }
    public bool IsNewCycleDetected { get; set; }

    public string CycleStartDisplay =>
        $"Cycle started: {LastCollectionDateTime:dd MMM yyyy}";
}