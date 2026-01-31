using ADWebApplication.Data;
using ADWebApplication.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace ADWebApplication.Data.Repository
{
    public interface IDashboardRepository
    {
       Task <DashboardKPIs> GetAdminDashboardAsync(DateTime? forMonth = null);
       Task<List<CollectionTrend>> GetCollectionTrendsAsync(int monthsBack = 6);
       Task<List<CategoryBreakdown>> GetCategoryBreakdownAsync();
       Task<List<AvgPerformance>> GetAvgPerformanceMetricsAsync();
       Task<int> GetHighRiskUnscheduledCountAsync();
    }
    public class DashboardRepository : IDashboardRepository
    {
        private readonly DashboardDbContext _db;

        public DashboardRepository(DashboardDbContext db)
        {
            _db = db;
        }

        public async Task<DashboardKPIs> GetAdminDashboardAsync(DateTime? forMonth = null)
        {
            var targetMonth = forMonth ?? DateTime.UtcNow;
            var previousMonth = targetMonth.AddMonths(-1);
            var totalUsers = await _db.Users.CountAsync(u => u.IsActive);

            var currentCollections = await _db.DisposalLogs.CountAsync(l =>
                l.DisposalTimeStamp.Year == targetMonth.Year && l.DisposalTimeStamp.Month == targetMonth.Month);
            var prevCollections = await _db.DisposalLogs.CountAsync(l =>
                l.DisposalTimeStamp.Year == previousMonth.Year && l.DisposalTimeStamp.Month == previousMonth.Month);

            var currentWeight = await _db.DisposalLogs
                .Where(l => l.DisposalTimeStamp.Year == targetMonth.Year
                            && l.DisposalTimeStamp.Month == targetMonth.Month)
                .SumAsync(l => (decimal?)l.EstimatedTotalWeight) ?? 0;

            var prevWeight = await _db.DisposalLogs
                .Where(l => l.DisposalTimeStamp.Year == previousMonth.Year
                            && l.DisposalTimeStamp.Month == previousMonth.Month)
                .SumAsync(l => (decimal?)l.EstimatedTotalWeight) ?? 0;

            return new DashboardKPIs
            {
                TotalUsers = totalUsers,
                UserGrowthPercent = 0,
                TotalCollections = currentCollections,
                CollectionGrowthPercent = prevCollections > 0 ? ((currentCollections - prevCollections) * 100.0m / prevCollections) : 0,
                TotalWeightRecycled = currentWeight,
                WeightGrowthPercent = prevWeight > 0 ? ((currentWeight - prevWeight) * 100.0m / prevWeight) : 0,
                AvgBinFillRate = 0,
                BinFillRateChange = 0
            };
        }

        public async Task<List<CollectionTrend>> GetCollectionTrendsAsync(int monthsBack = 6)
        {
            var cutoff = DateTime.UtcNow.AddMonths(-monthsBack);

            var results = await _db.DisposalLogs
                .Where(l => l.DisposalTimeStamp >= cutoff)
                .GroupBy(l => new { l.DisposalTimeStamp.Year, l.DisposalTimeStamp.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    Collections = g.Count(),
                    Weight = g.Sum(x => (decimal?)x.EstimatedTotalWeight) ?? 0
                })
                .OrderBy(g => g.Year)
                .ThenBy(g => g.Month)
                .ToListAsync();

            return results
                .Select(r => new CollectionTrend
                {
                    Month = $"{r.Year:D4}-{r.Month:D2}",
                    Collections = r.Collections,
                    Weight = r.Weight
                })
                .ToList();
        }

        public async Task<List<CategoryBreakdown>> GetCategoryBreakdownAsync()
        {
            var cutoff = DateTime.UtcNow.AddMonths(-1);

            var categoryCounts = await (from item in _db.DisposalLogItems
                                        join log in _db.DisposalLogs on item.LogId equals log.LogId
                                        join type in _db.EWasteItemTypes on item.ItemTypeId equals type.ItemTypeId
                                        join category in _db.EWasteCategories on type.CategoryId equals category.CategoryId
                                        where log.DisposalTimeStamp >= cutoff
                                        group category by category.CategoryName into g
                                        select new
                                        {
                                            Category = g.Key,
                                            Count = g.Count()
                                        })
                .OrderByDescending(g => g.Count)
                .ToListAsync();

            var total = categoryCounts.Sum(x => x.Count);

            return categoryCounts
                .Select(x =>
                {
                    var category = string.IsNullOrWhiteSpace(x.Category) ? "Uncategorized" : x.Category;
                    return new CategoryBreakdown
                    {
                        Category = category,
                        Value = total > 0 ? (int)(x.Count * 100.0 / total) : 0,
                        Color = category switch
                        {
                            "Computers" => "#3b82f6",
                            "Mobile Devices" => "#10b981",
                            "Home Appliances" => "#f59e0b",
                            "Accessories" => "#8b5cf6",
                            _ => "#6b7280"
                        }
                    };
                })
                .ToList();
        }
        

        public async Task<List<AvgPerformance>> GetAvgPerformanceMetricsAsync()
        {
            var cutoff = DateTime.UtcNow.AddMonths(-1);

            var areaStats = await (from log in _db.DisposalLogs
                                   join bin in _db.CollectionBins on log.BinId equals bin.BinId into binJoin
                                   from bin in binJoin.DefaultIfEmpty()
                                   join region in _db.Regions on bin.RegionId equals region.RegionId into regionJoin
                                   from region in regionJoin.DefaultIfEmpty()
                                   where log.DisposalTimeStamp >= cutoff
                                   group new { log, bin, region } by new { bin.RegionId, region.RegionName } into g
                                   select new
                                   {
                                       RegionId = g.Key.RegionId,
                                       RegionName = g.Key.RegionName,
                                       Collections = g.Count(),
                                       UniqueUsers = g.Select(x => x.log.UserId).Distinct().Count()
                                   })
                .OrderByDescending(g => g.Collections)
                .ToListAsync();

            var areaPopulation = await _db.Users
                .Where(u => u.IsActive)
                .GroupBy(u => u.RegionId)
                .Select(g => new
                {
                    RegionId = g.Key,
                    TotalUsers = g.Count()
                })
                .ToDictionaryAsync(x => x.RegionId, x => x.TotalUsers);

            return areaStats
                .Select(a =>
                {
                    areaPopulation.TryGetValue(a.RegionId, out var totalUsers);
                    var participation = totalUsers > 0
                        ? (decimal)(a.UniqueUsers * 100.0 / totalUsers)
                        : 0;

                    return new AvgPerformance
                    {
                        Area = string.IsNullOrWhiteSpace(a.RegionName)
                            ? (a.RegionId.HasValue ? $"Region {a.RegionId}" : "Unassigned")
                            : a.RegionName,
                        Collections = a.Collections,
                        Participation = Math.Round(participation, 2)
                    };
                })
                .ToList();
        }

        public async Task<int> GetHighRiskUnscheduledCountAsync()
        {
            return await _db.FillLevelPredictions
                .Where(p => p.PredictedStatus == "critical" || p.PredictedStatus == "Critical")
                .Select(p => p.BinId)
                .Distinct()
                .CountAsync();
        }
    }
}
