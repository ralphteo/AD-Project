using System.Net.Http.Json;
using ADWebApplication.Data;
using ADWebApplication.Models.DTOs;
using ADWebApplication.Models.ViewModels.BinPredictions;
using ADWebApplication.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography.X509Certificates;
using System.ComponentModel;
using Microsoft.VisualBasic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices.Marshalling;
using System.Net.WebSockets;
using System.Reflection.Metadata;
using System.Linq.Expressions;
using System.Collections.Specialized;
using System.Runtime.Serialization.Formatters.Binary;
using System.Dynamic;
using System.Reflection.Metadata.Ecma335;

namespace ADWebApplication.Services;

public class BinPredictionService : IBinPredictionService
{
    private readonly HttpClient client;
    private readonly In5niteDbContext db;

    public BinPredictionService(HttpClient httpClient, In5niteDbContext context)
    {
        client = httpClient;
        db = context;
    }

    // bin fill risk classification
    private static string GetRiskLevel(int daysToThreshold)
    {
        if (daysToThreshold <= 1)
            return "High";

        if (daysToThreshold <= 3)
            return "Medium";

        return "Low";
    }

    // Check if ML prediction needs refresh
    private static bool NeedsPredictionRefresh(FillLevelPrediction? latestPrediction, CollectionDetails latestCollection)
    {
        if (latestPrediction == null)
            return true;

        if (!latestCollection.CurrentCollectionDateTime.HasValue)
            return true;

        // Prediction is older than the most recent collection
        return latestPrediction.PredictedDate < latestCollection.CurrentCollectionDateTime.Value.UtcDateTime;
    }

    // Get the lastest 2 collection records for all bins
    private async Task<Dictionary<int, List<CollectionDetails>>> GetCollectionHistoryAsync()
    {
        var records = await db.CollectionDetails
            .Where(cd => cd.BinId != null && cd.CurrentCollectionDateTime != null)
            .ToListAsync();

        return records
            .GroupBy(cd => cd.BinId.Value)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(x => x.CurrentCollectionDateTime).Take(2).ToList()
            );
    }

    private async Task<Dictionary<int, FillLevelPrediction>> GetLatestPredictionsAsync()
    {
        var records = await db.FillLevelPredictions
            .ToListAsync();

        return records
            .GroupBy(p => p.BinId)
            .Select(g => g.OrderByDescending(x => x.PredictedDate).First())
            .ToDictionary(p => p.BinId, p => p);
    }

    private static int CalculateDaysTo80Percent(double currentFill, double dailyGrowth)
    {
        if (currentFill >= 80)
        {
            return 0;
        }

        var remaining = 80 - currentFill;
        return (int)Math.Ceiling(remaining / dailyGrowth);
    }

    private static BinPredictionsTableViewModel CreateRowForBin(CollectionBin bin, CollectionDetails latestCollection, DateTimeOffset latestCollectedAt, 
        FillLevelPrediction? prediction, RouteStop? nextStop, DateTimeOffset today, bool needsRefresh)
    {
        // If bin just got collected and needs ML refresh
        if (needsRefresh)
        {
            return new BinPredictionsTableViewModel
            {
                BinId = bin.BinId,
                Region = bin.Region?.RegionName ?? "",
                LastCollectionDateTime = latestCollectedAt,
                PredictedNextAvgDailyGrowth = null,
                EstimatedFillToday = latestCollection.BinFillLevel,
                EstimatedDaysToThreshold = null,
                RiskLevel = "—",
                PlanningStatus = "Collection done",
                CollectionDone = true,
                NeedsPredictionRefresh = true,
                IsActualFillLevel = true,
                AutoSelected = false,
                RouteId = null
            };
        }

        // Normal bins with prediction data
        var predictedGrowth = prediction!.PredictedAvgDailyGrowth;
        var daysElapsed = Math.Max((today - latestCollectedAt).TotalDays, 0);
        var estimatedFillToday = Math.Clamp(predictedGrowth * daysElapsed, 0, 100);
        var daysTo80 = CalculateDaysTo80Percent(estimatedFillToday, predictedGrowth);
        
        bool isScheduled = nextStop?.PlannedCollectionTime >= today && nextStop.PlannedCollectionTime > latestCollectedAt;
        var planningStatus = isScheduled ? "Scheduled" : "Not Scheduled";
        var riskLevel = GetRiskLevel(daysTo80);
        var autoSelected = riskLevel == "High" && !isScheduled;

        return new BinPredictionsTableViewModel
        {
            BinId = bin.BinId,
            Region = bin.Region?.RegionName ?? "",
            LastCollectionDateTime = latestCollectedAt,
            PredictedNextAvgDailyGrowth = predictedGrowth,
            EstimatedFillToday = estimatedFillToday,
            EstimatedDaysToThreshold = daysTo80,
            RiskLevel = riskLevel,
            PlanningStatus = planningStatus,
            IsActualFillLevel = false,
            AutoSelected = autoSelected,
            RouteId = isScheduled && nextStop!.RouteId != null
                ? nextStop.RouteId.Value.ToString()
                : null
        };
    }

    private static BinPredictionsTableViewModel? ProcessSingleBin(CollectionBin bin, Dictionary<int, List<CollectionDetails>> collectionHistoryByBin, Dictionary<int, FillLevelPrediction> latestPredictionByBin,
        Dictionary<int, RouteStop> nextStopByBin, DateTimeOffset today, ref int newCycleDetectedCount, ref int missingPredictionCount)
    {
        if (!collectionHistoryByBin.TryGetValue(bin.BinId, out var history) || history.Count == 0)
        {
            return null;
        }

        var latestCollection = history[0];
        var latestCollectedAt = latestCollection.CurrentCollectionDateTime.Value;

        latestPredictionByBin.TryGetValue(bin.BinId, out var latestPrediction);
        nextStopByBin.TryGetValue(bin.BinId, out var nextStop);

        if (NeedsPredictionRefresh(latestPrediction, latestCollection))
        {
            newCycleDetectedCount++;
            return CreateRowForBin(bin, latestCollection, latestCollectedAt, null, nextStop, today, true);
        }

        if (latestPrediction == null)
        {
            missingPredictionCount++;
            return null;
        }

        return CreateRowForBin(bin, latestCollection, latestCollectedAt, latestPrediction, nextStop, today, false);
    }

    private static int GetPrioritySortKey(BinPredictionsTableViewModel row)
    {
        if (row.RiskLevel == "High" && row.PlanningStatus == "Not Scheduled")
        {
            return 0;
        }
        if (row.RiskLevel == "High")
        {
            return 1;
        }
        if (row.RiskLevel == "Medium")
        {
            return 2;
        }
        return 3;
    }

    private static IEnumerable<BinPredictionsTableViewModel> ApplySort(IEnumerable<BinPredictionsTableViewModel> query, 
        string sort, bool isDesc)
    {
        if (sort == "EstimatedFill")
        {
            return isDesc
                ? query.OrderByDescending(r => r.EstimatedFillToday)
                : query.OrderBy(r => r.EstimatedFillToday);
        }
        
        if (sort == "AvgGrowth")
        {
            var defaultValueAsc = double.MaxValue;
            var defaultValueDesc = -1.0;
            return isDesc
                ? query.OrderByDescending(r => r.PredictedNextAvgDailyGrowth ?? defaultValueDesc)
                : query.OrderBy(r => r.PredictedNextAvgDailyGrowth ?? defaultValueAsc);
        }
        
        var defaultThresholdValue = int.MaxValue;
        return isDesc
            ? query.OrderByDescending(r => r.EstimatedDaysToThreshold ?? defaultThresholdValue)
            : query.OrderBy(r => r.EstimatedDaysToThreshold ?? defaultThresholdValue);
    }

    private static IEnumerable<BinPredictionsTableViewModel> SortAndFilterRows(IEnumerable<BinPredictionsTableViewModel> rows,
        string sort, string sortDir, string risk, string timeframe)
    {
        var query = rows;

        if (risk != "All")
        {
            query = query.Where(r => r.RiskLevel == risk);
        }

        if (timeframe == "3")
        {
            query = query.Where(r => r.EstimatedDaysToThreshold <= 3);
        }
        else if (timeframe == "7")
        {
            query = query.Where(r => r.EstimatedDaysToThreshold <= 7);
        }

        bool isDefaultSort = sort == "EstimatedFill" && sortDir == "desc";

        if (isDefaultSort)
        {
            // Priority view: high risk unscheduled bins first
            return query
                .OrderBy(GetPrioritySortKey)
                .ThenByDescending(r => r.EstimatedFillToday);
        }

        bool isDesc = sortDir == "desc";
        return ApplySort(query, sort, isDesc);
    }

    public async Task<BinPredictionsPageViewModel> BuildBinPredictionsPageAsync(int page, string sort, string sortDir, string risk, string timeframe)
    {
        int pageSize = 10;
        var today = DateTimeOffset.UtcNow.Date;

        var collectionHistoryByBin = await GetCollectionHistoryAsync();
        var latestPredictionByBin = await GetLatestPredictionsAsync();

        var bins = await db.CollectionBins
            .Where(b => b.BinStatus == "Active")
            .Include(b => b.Region)
            .ToListAsync();

        // Fetch next scheduled route stop for each bin (from today onwards)
        var nextRouteStop = await db.RouteStops
            .Where(rs => rs.PlannedCollectionTime >= today && rs.BinId.HasValue)
            .GroupBy(rs => rs.BinId.Value)
            .Select(g => g.OrderBy(x => x.PlannedCollectionTime).FirstOrDefault())
            .ToListAsync();

        var nextStopByBin = nextRouteStop
            .Where(x => x != null && x.BinId.HasValue)
            .ToDictionary(x => x.BinId.Value, x => x);

        var rows = new List<BinPredictionsTableViewModel>();

        int newCycleDetectedCount = 0;
        int missingPredictionCount = 0;

        // Process each bin and build rows
        foreach (var bin in bins)
        {
            var row = ProcessSingleBin(
                bin, 
                collectionHistoryByBin, 
                latestPredictionByBin, 
                nextStopByBin, 
                today,
                ref newCycleDetectedCount, 
                ref missingPredictionCount);

            if (row != null)
            {
                rows.Add(row);
            }
        }

        // calculate avg fill growth rate
        var avgGrowth = rows
            .Where(r => r.PredictedNextAvgDailyGrowth.HasValue)
            .Select(r => r.PredictedNextAvgDailyGrowth.Value)
            .DefaultIfEmpty(0)
            .Average();

        // Apply filters and sorting
        var orderedQuery = SortAndFilterRows(rows, sort, sortDir, risk, timeframe);

        //pagination
        //counts no. of rows after filtering/sorting
        var totalItems = orderedQuery.Count();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        if (totalPages == 0)
        {
            page = 1;
        }
        else
        {
            page = Math.Clamp(page, 1, totalPages);
        }
        
        var pagedRows = orderedQuery
            .Skip((page - 1) * pageSize) //skip rows belonging to the prev. page
            .Take(pageSize) //limit no. of rows displayed to 10
            .ToList();

        bool isDefaultSort = sort == "EstimatedFill" && sortDir == "desc";

        return new BinPredictionsPageViewModel
        {
            Rows = pagedRows,
            TotalBins = bins.Count,
            HighPriorityBins = rows.Count(r => r.EstimatedDaysToThreshold <= 1),
            AvgDailyFillGrowthOverall = avgGrowth,

            SelectedRisk = risk,
            SelectedTimeframe = timeframe,
            SortBy = sort,
            SortDir = sortDir,

            CurrentPage = page,
            TotalPages = totalPages,

            HighRiskUnscheduledCount = rows.Count(r =>
                r.RiskLevel == "High" && r.PlanningStatus == "Not Scheduled"
            ),
            IsDefaultPriorityView = isDefaultSort,

            NewCycleDetectedCount = newCycleDetectedCount,
            MissingPredictionCount = missingPredictionCount
        };
    }

    public async Task<int> RefreshPredictionsForNewCyclesAsync()
    {
        var now = DateTimeOffset.UtcNow;

        var collectionHistoryByBin = await GetCollectionHistoryAsync();
        var latestPredictionByBin = await GetLatestPredictionsAsync();

        int refreshed = 0;

        foreach (var (binId, history) in collectionHistoryByBin)
        {
            if (history.Count < 2)
            {
                continue;
            }

            var latestCollection = history[0];
            var olderCollection = history[1];

            if (latestCollection.CurrentCollectionDateTime == null || olderCollection.CurrentCollectionDateTime == null)
            {
                continue;
            }

            latestPredictionByBin.TryGetValue(binId, out var latestPrediction);

            if (!NeedsPredictionRefresh(latestPrediction, latestCollection))
            {
                continue;
            }

            var latestCollectedAt = latestCollection.CurrentCollectionDateTime.Value;

            int cycleDurationDays = (int)Math.Ceiling((latestCollectedAt - olderCollection.CurrentCollectionDateTime.Value).TotalDays);

            int cycleStartMonth = latestCollectedAt.Month;

            // Call ML
            var req = new MLPredictionRequestDto
            {
                container_id = binId.ToString(),
                collection_fill_percentage = latestCollection.BinFillLevel,
                cycle_duration_days = cycleDurationDays,
                cycle_start_month = cycleStartMonth
            };

            var response = await client.PostAsJsonAsync("/predict", req);
            response.EnsureSuccessStatusCode();

            var ml = await response.Content.ReadFromJsonAsync<MLPredictionResponseDto>();
            if (ml == null) continue;

            // Save predicted next cycle avg daily growth
            db.FillLevelPredictions.Add(new FillLevelPrediction
            {
                BinId = binId,
                PredictedAvgDailyGrowth = ml.predicted_next_avg_daily_growth,
                PredictedDate = now.UtcDateTime,
                ModelVersion = "v1"
            });

            refreshed++;
        }

        await db.SaveChangesAsync();
        return refreshed;
    }

    //send high-priority bins to Route Planning (Sara)
    public async Task<List<BinPriorityDto>> GetBinPrioritiesAsync()
    {
        var today = DateTimeOffset.UtcNow.Date;

        var collectionHistoryByBin = await GetCollectionHistoryAsync();
        var latestPredictionByBin = await GetLatestPredictionsAsync();

        var result = new List<BinPriorityDto>();

        foreach (var (binId, history) in collectionHistoryByBin)
        {
            if (history.Count == 0)
            {
                continue;
            }

            var latest = history[0];

            if (!latestPredictionByBin.TryGetValue(binId, out var prediction))
            {
                continue;
            }

            if(!latest.CurrentCollectionDateTime.HasValue)
            {
                continue;
            }

            if (prediction.PredictedAvgDailyGrowth <= 0)
            {
                continue;
            }

            var daysElapsed = Math.Max((today - latest.CurrentCollectionDateTime.Value).TotalDays, 0);

            var estimatedFillToday = Math.Clamp(prediction.PredictedAvgDailyGrowth * daysElapsed, 0, 100);

            int daysTo80 = estimatedFillToday >= 80
            ? 0
            : (int)Math.Ceiling(
                (80 - estimatedFillToday) / prediction.PredictedAvgDailyGrowth
            );

            result.Add(new BinPriorityDto
            {
              BinId = binId,
                DaysTo80 = daysTo80
            });
        }
        return result;
    }

}
