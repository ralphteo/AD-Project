namespace ADWebApplication.Models.DTOs
{
    public class AdminDashboardViewModel
    {
        public DashboardKPIs KPIs { get; set; }
        public List<CollectionTrend> CollectionTrends { get; set; }
        public List<CategoryBreakdown> CategoryBreakdowns { get; set; }
        public List<AvgPerformance> PerformanceMetrics { get; set; }
        public int HighRiskUnscheduledCount { get; set; }
        public int ActiveBinsCount { get; set; }
        public int TotalBinsCount { get; set; }
    }
}
