namespace ADWebApplication.Models.DTOs
{
    public class DashboardKPIs
    {
        public int TotalUsers { get; set; }
        public decimal UserGrowthPercent { get; set; }
        public int TotalCollections { get; set; }
        public decimal CollectionGrowthPercent { get; set; }
        public decimal TotalWeightRecycled { get; set; }
        public decimal WeightGrowthPercent { get; set; }
        public decimal AvgBinFillRate { get; set; }
        public decimal BinFillRateChange { get; set; }
    }
    
}