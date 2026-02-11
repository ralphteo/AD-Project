using ADWebApplication.Data;
using ADWebApplication.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace ADWebApplication.Data.Repository
{
    public interface IDashboardRepository
    {
        Task<DashboardKPIs> GetAdminDashboardAsync(DateTime? forMonth = null);
        Task<List<CollectionTrend>> GetCollectionTrendsAsync(int monthsBack = 6);
        Task<List<CategoryBreakdown>> GetCategoryBreakdownAsync();
        Task<List<AvgPerformance>> GetAvgPerformanceMetricsAsync();
        Task<int> GetHighRiskUnscheduledCountAsync();
        Task<(int ActiveBins, int TotalBins)> GetBinCountsAsync();
    }
    public class DashboardRepository : IDashboardRepository
    {
        private readonly In5niteDbContext _db;

        public DashboardRepository(In5niteDbContext db)
        {
            _db = db;
        }

        public async Task<DashboardKPIs> GetAdminDashboardAsync(DateTime? forMonth = null)
        {
            var targetMonth = forMonth ?? DateTime.SpecifyKind(DateTime.Now.AddMonths(-1), DateTimeKind.Utc);
            var previousMonth = targetMonth.AddMonths(-1);
        
            var allUsers = await _db.PublicUser.CountAsync();

            var activeUsers1 = await _db.PublicUser.CountAsync(u => u.IsActive);

            var sampleUsers = await _db.PublicUser.Take(5).Select(u => new { u.Id, u.IsActive }).ToListAsync();

            var totalUsers = activeUsers1;

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

            var (currentUsers, userGrowthPercent) = 
                await GetUserGrowthAsync(targetMonth, previousMonth);
          
            //binFillRate

            var currentBinFillRate = await GetAverageBinFillRateAsync(targetMonth);
            var previousBinFillRate = await GetAverageBinFillRateAsync(previousMonth);

            var binFillRateChange = CalculateGrowthPercent(currentBinFillRate, previousBinFillRate);

            var collectionMTD = await GetCurrentMonthCollectionsMTDAsync(); // To be implemented
            var weightMTD = await GetCurrentMonthWeightMTDAsync(); // To be implemented
                                            
            return new DashboardKPIs
            {
                TotalUsers = totalUsers,
                ActiveUsersWithDisposals = currentUsers,
                UserGrowthPercent = userGrowthPercent,
                TotalCollections = currentCollections,
                CollectionGrowthPercent = prevCollections > 0 ? ((currentCollections - prevCollections) * 100.0m / prevCollections) : 0,
                TotalWeightRecycled = currentWeight,
                WeightGrowthPercent = prevWeight > 0 ? ((currentWeight - prevWeight) * 100.0m / prevWeight) : 0,
                AvgBinFillRate = currentBinFillRate,
                BinFillRateChange = binFillRateChange,
                CurrentCollectionMTD = collectionMTD, 
                CurrentWeightMTD = weightMTD    
            };
        }

        private async Task<int> GetCurrentMonthCollectionsMTDAsync()
        {
            var now = DateTime.UtcNow;
            var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            return await _db.DisposalLogs
                .Where(l => l.DisposalTimeStamp >= startOfMonth && 
                l.DisposalTimeStamp <= now)
                .CountAsync();
        }
        private async Task<int> GetCurrentMonthWeightMTDAsync()
        {
            var now = DateTime.UtcNow;
            var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            return (int)(await _db.DisposalLogs
                .Where(l => l.DisposalTimeStamp >= startOfMonth &&
                l.DisposalTimeStamp <= now)
                .SumAsync(l => (decimal?)l.EstimatedTotalWeight) ?? 0);
        }
        private static decimal CalculateGrowthPercent(decimal current, decimal previous)
        {
            return previous > 0
                ? ((current - previous) * 100.0m / previous)
                : 0;
        }
        private async Task<(int current, decimal growthPercent)> GetUserGrowthAsync(DateTime targetMonth, DateTime previousMonth)
        {
            var current = await GetActiveUsersWithDisposalsAsync(targetMonth);
            var previous = await GetActiveUsersWithDisposalsAsync(previousMonth);
            return (current, CalculateGrowthPercent(current, previous));

        }
        private async Task<int> GetActiveUsersWithDisposalsAsync(DateTime month)
        {
            return await (
                from user in _db.PublicUser
                join log in _db.DisposalLogs on user.Id equals log.UserId
                where user.IsActive &&
                      log.DisposalTimeStamp.Year == month.Year &&
                      log.DisposalTimeStamp.Month == month.Month
                select user.Id).Distinct().CountAsync();
        }
        private async Task<decimal> GetAverageBinFillRateAsync(DateTime month)
        {
            var nextMonthStart = new DateTime(
                month.Year, 
                month.Month,
                1, 0, 0, 0, DateTimeKind.Utc) //Day, Hr, Min, Sec
                .AddMonths(1);
            var latestCollectionPerBin = await _db.CollectionDetails
                .Where(cd => cd.CurrentCollectionDateTime != null 
                        && cd.CurrentCollectionDateTime < nextMonthStart
                        && cd.AvgDailyFillGrowth.HasValue)
                .GroupBy(cd => cd.BinId)
                .Select(binGroup => new
                {
                    BinId = binGroup.Key,
                    LatestDate = binGroup.Max(x => x.CurrentCollectionDateTime)
                })
                .ToListAsync();

            var fillRates = new List<decimal>();
            foreach (var binData in latestCollectionPerBin)
            {
                var fillRate = await _db.CollectionDetails
                    .Where(cd => cd.BinId == binData.BinId 
                            && cd.CurrentCollectionDateTime == binData.LatestDate
                            && cd.AvgDailyFillGrowth.HasValue)
                    .Select(cd => (decimal)cd.AvgDailyFillGrowth!.Value)
                    .FirstOrDefaultAsync();
                if (fillRate > 0)
                {
                    fillRates.Add(fillRate);
                }
                
            }
            return fillRates.Count > 0 ? fillRates.Average() : 0m;
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
                        Color = GetCategoryColor(category)
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
                                        && bin.RegionId != null  // ✅ ADD THIS - Filter out unassigned regions
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

            var areaPopulation = await _db.PublicUser
                .Where(u => u.IsActive && u.RegionId != null)
                .GroupBy(u => u.RegionId)
                .Select(g => new
                {
                    RegionId = g.Key,
                    TotalUsers = g.Count()
                })
                .Where(x => x.RegionId.HasValue)
                .ToDictionaryAsync(x => x.RegionId!.Value, x => x.TotalUsers);

            return areaStats
            .Where(a => a.RegionId.HasValue) // Ensure we only process areas with a RegionId
                .Select(a =>
                {
                    var totalUsers = 0;
                    if (a.RegionId.HasValue)
                    {
                        areaPopulation.TryGetValue(a.RegionId.Value, out totalUsers);
                    }

                    var participation = totalUsers > 0
                        ? (decimal)(a.UniqueUsers * 100.0 / totalUsers)
                        : 0;

                    return new AvgPerformance
                    {
                        Area = string.IsNullOrWhiteSpace(a.RegionName)
                            ?$"Region {a.RegionId}"
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

        public async Task<(int ActiveBins, int TotalBins)> GetBinCountsAsync()
        {
            var totalBins = await _db.CollectionBins.CountAsync();
            var activeBins = await _db.CollectionBins.CountAsync(b => b.BinStatus == "Active");
            return (activeBins, totalBins);
        }

        private static string GetCategoryColor(string category)
        {
            var palette = new[]
            {
                "#0ea5e9",
                "#22c55e",
                "#f59e0b",
                "#ef4444",
                "#8b5cf6",
                "#14b8a6",
                "#84cc16"
            };

            var hash = 17;
            foreach (var ch in category)
            {
                hash = (hash * 31) + ch;
            }

            var index = Math.Abs(hash) % palette.Length;
            return palette[index];
        }
    }
}
