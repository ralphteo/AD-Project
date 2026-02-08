namespace ADWebApplication.Models.DTOs
{
    public class AdminDashboardViewModel
    {
        public required DashboardKPIs KPIs { get; set; }
        public required List<CollectionTrend> CollectionTrends { get; set; }
        public required List<CategoryBreakdown> CategoryBreakdowns { get; set; }
        public required List<AvgPerformance> PerformanceMetrics { get; set; }
        public int HighRiskUnscheduledCount { get; set; }
        public int ActiveBinsCount { get; set; }
        public int TotalBinsCount { get; set; }
        public List<AdminAlertDto> Alerts { get; set; } = new();
    }
}
