using System.Collections.Generic;

namespace ADWebApplication.Models.ViewModels.BinPredictions;

public class BinPredictionsPageViewModel
{
    // Data 
    public List<BinPredictionsTableViewModel> Rows { get; set; } = new();

    // Summary cards
    public int TotalBins { get; set; }
    public int HighPriorityBins { get; set; }
    public double AvgDailyFillGrowthOverall =>
        Rows.Any() ? Rows.Average(r => r.PredictedNextAvgDailyGrowth) : 0;
    public int NewCycleDetectedCount { get; set; }

    // Banner
    public int HighRiskUnscheduledCount { get; set; }

    // Pagination
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }

    // Filtering/Sorting
    public string SelectedRisk { get; set; } = "All";
    public string SelectedTimeframe { get; set; } = "All";
    public string SortBy { get; set; } = "DaysToThreshold";
    public string SortDir { get; set; } = "asc";
}