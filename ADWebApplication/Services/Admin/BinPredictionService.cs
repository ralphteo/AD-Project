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
            .GroupBy(cd => cd.BinId!.Value)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(x => x.CurrentCollectionDateTime).Take(2).ToList()
            );
    }

    // Get the latest prediction records for all bins
    private async Task<Dictionary<int, FillLevelPrediction>> GetLatestPredictionsAsync()
    {
        var records = await db.FillLevelPredictions
            .ToListAsync();

        return records
            .GroupBy(p => p.BinId)
            .Select(g => g.OrderByDescending(x => x.PredictedDate).First())
            .ToDictionary(p => p.BinId, p => p);
    }

    public async Task<BinPredictionsPageViewModel> BuildBinPredictionsPageAsync(int page, string sort, string sortDir, string risk, string timeframe)
    {
        int pageSize = 10;
        var today = DateTimeOffset.UtcNow.Date;

<<<<<<< HEAD:ADWebApplication/Services/BinPredictionService.cs
        // risk = string.IsNullOrWhiteSpace(risk) ? "All" : risk;
        // timeframe = string.IsNullOrWhiteSpace(timeframe) ? "All" : timeframe;
        // sort = string.IsNullOrWhiteSpace(sort) ? "DaysToThreshold" : sort;
        // sortDir = string.IsNullOrWhiteSpace(sortDir) ? "asc" : sortDir;

        var latestCollectionByBin = await GetLatestCollectionsAsync();
=======
        var collectionHistoryByBin = await GetCollectionHistoryAsync();
>>>>>>> origin/main:ADWebApplication/Services/Admin/BinPredictionService.cs
        var latestPredictionByBin = await GetLatestPredictionsAsync();

        var bins = await db.CollectionBins
            .Where(b => b.BinStatus == "Active")
            .Include(b => b.Region)
            .ToListAsync();

        // Fetch next scheduled route stop for each bin (from today onwards)
        var nextRouteStop = await db.RouteStops
            .Where(rs => rs.PlannedCollectionTime >= today)
            .GroupBy(rs => rs.BinId)
            .Select(g => g.OrderBy(x => x.PlannedCollectionTime).FirstOrDefault())
            .ToListAsync();

        var nextStopByBin = nextRouteStop
            .Where(x => x != null)
            .ToDictionary(x => x!.BinId, x => x!);

        var rows = new List<BinPredictionsTableViewModel>();

        int newCycleDetectedCount = 0;
        int missingPredictionCount = 0;

        foreach (var bin in bins)
        {
            if (!collectionHistoryByBin.TryGetValue(bin.BinId, out var history) || history.Count == 0)
                continue;

            var latestCollection = history[0];
            var olderCollection = history.Count > 1 ? history[1] : null;

            var latestCollectedAt = latestCollection.CurrentCollectionDateTime!.Value;

            int? cycleDurationDays = null;
            int? cycleStartMonth = null;

            //compute cycle duration period
            if (olderCollection?.CurrentCollectionDateTime != null)
            {
                cycleDurationDays = (int)Math.Ceiling(
                    (latestCollectedAt - olderCollection.CurrentCollectionDateTime.Value).TotalDays
                );
                cycleStartMonth = latestCollectedAt.Month;
            }

            latestPredictionByBin.TryGetValue(bin.BinId, out var latestPrediction);
            nextStopByBin.TryGetValue(bin.BinId, out var nextStop);

            if (NeedsPredictionRefresh(latestPrediction, latestCollection))
            {
                newCycleDetectedCount++;

                rows.Add(new BinPredictionsTableViewModel
                {
                    BinId = bin.BinId,
                    Region = bin.Region?.RegionName,
                    LastCollectionDateTime = latestCollectedAt,

                    // For bins that are collected/new, requiring refresh
                    PredictedNextAvgDailyGrowth = null,
                    EstimatedFillToday = 0,
                    EstimatedDaysToThreshold = null,

                    RiskLevel = "—",
                    PlanningStatus = "Collection done",

                    CollectionDone = true,
                    NeedsPredictionRefresh = true,

                    AutoSelected = false,
                    RouteId = null
                });

                continue;
            }

            if (latestPrediction == null ||
                cycleDurationDays == null ||
                cycleStartMonth == null)
            {
                missingPredictionCount++;
                continue;
            }

            double predictedGrowth = latestPrediction.PredictedAvgDailyGrowth;

            // Count no. of days since last collection
            var daysElapsed = Math.Max((today - latestCollection.CurrentCollectionDateTime.Value).TotalDays, 0);

            // Bin fill % starts from 0 after collection
            var estimatedFillToday = Math.Clamp(predictedGrowth * daysElapsed, 0, 100);

            // Count no. of days left to threshold 80%
            int daysTo80;
            if (estimatedFillToday >= 80)
            {
                daysTo80 = 0;
            }
            else
            {
                var remaining = 80 - estimatedFillToday;
                daysTo80 = (int)Math.Ceiling(remaining / predictedGrowth);
            }

<<<<<<< HEAD:ADWebApplication/Services/BinPredictionService.cs
            // Bins are auto-selected as urgent if it is predicted to reach 80% the next day
            bool autoSelected = daysTo80 <= 1;
            
            // Retrieve next scheduled route stop of each bin if any
            nextStopByBin.TryGetValue(bin.BinId, out var nextStop);

            var lastCollectedAt = latest.CurrentCollectionDateTime;
            var nextPlannedAt = nextStop?.PlannedCollectionTime;

            // A bin is considered scheduled if a planned collection exists and the planned date is after the last collection and not in the past
=======
>>>>>>> origin/main:ADWebApplication/Services/Admin/BinPredictionService.cs
            bool isScheduled =
                nextStop?.PlannedCollectionTime >= today &&
                nextStop.PlannedCollectionTime > latestCollectedAt;

            var planningStatus = isScheduled ? "Scheduled" : "Not Scheduled";
            var riskLevel = GetRiskLevel(daysTo80);

            // Auto-select bins that are high risk and unscheduled for scheduling
            var autoSelected = riskLevel == "High" && !isScheduled;

            rows.Add(new BinPredictionsTableViewModel
            {
                BinId = bin.BinId,
                Region = bin.Region?.RegionName,
                LastCollectionDateTime = latestCollectedAt,

                PredictedNextAvgDailyGrowth = predictedGrowth,
                EstimatedFillToday = estimatedFillToday,
                EstimatedDaysToThreshold = daysTo80,
                RiskLevel = riskLevel,
                PlanningStatus = planningStatus,

                AutoSelected = autoSelected,
                RouteId = isScheduled && nextStop?.RouteId != null
                    ? nextStop.RouteId.Value.ToString()
                    : null
            });

        }

        // calculate avg fill growth rate
        var avgGrowth = rows
            .Where(r => r.PredictedNextAvgDailyGrowth.HasValue)
            .Select(r => r.PredictedNextAvgDailyGrowth!.Value)
            .DefaultIfEmpty(0)
            .Average();

        // filters
        IEnumerable<BinPredictionsTableViewModel> query = rows;

        if (risk != "All")
            query = query.Where(r => r.RiskLevel == risk);

        if (timeframe == "3")
            query = query.Where(r => r.EstimatedDaysToThreshold <= 3);

        else if (timeframe == "7")
            query = query.Where(r => r.EstimatedDaysToThreshold <= 7);

        // to list high risk unscheduled bins first
        bool isDefaultSort = sort == "EstimatedFill" && sortDir == "desc";

        IEnumerable<BinPredictionsTableViewModel> orderedQuery;

        if (isDefaultSort)
        {
            // Priority view
            orderedQuery = query
                .OrderBy(r =>
                    r.RiskLevel == "High" && r.PlanningStatus == "Not Scheduled" ? 0 :
                    r.RiskLevel == "High" ? 1 :
                    r.RiskLevel == "Medium" ? 2 :
                    3
                )
                .ThenByDescending(r => r.EstimatedFillToday);
        }
        else
        {
            // User-controlled sorting
            bool isDesc = sortDir == "desc";

            if (sort == "EstimatedFill")
            {
                orderedQuery = isDesc
                    ? query.OrderByDescending(r => r.EstimatedFillToday)
                    : query.OrderBy(r => r.EstimatedFillToday);
            }
            else if (sort == "AvgGrowth")
            {
                orderedQuery = isDesc
                    ? query.OrderByDescending(r => r.PredictedNextAvgDailyGrowth ?? -1)
                    : query.OrderBy(r => r.PredictedNextAvgDailyGrowth ?? double.MaxValue);
            }
            else
            {
                orderedQuery = isDesc
                    ? query.OrderByDescending(r => r.EstimatedDaysToThreshold ?? int.MaxValue)
                    : query.OrderBy(r => r.EstimatedDaysToThreshold ?? int.MaxValue);
            }
        }

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
        
<<<<<<< HEAD:ADWebApplication/Services/BinPredictionService.cs
        var pagedRows = query
=======
        var pagedRows = orderedQuery
>>>>>>> origin/main:ADWebApplication/Services/Admin/BinPredictionService.cs
            .Skip((page - 1) * pageSize) //skip  rows belonging to the prev. page
            .Take(pageSize) //limit no. of rows displayed to 10
            .ToList();

        return new BinPredictionsPageViewModel
        {
            Rows = pagedRows,
            TotalBins = bins.Count,
<<<<<<< HEAD:ADWebApplication/Services/BinPredictionService.cs
            HighPriorityBins = query.Count(r => r.EstimatedDaysToThreshold <= 1),
=======
            HighPriorityBins = rows.Count(r => r.EstimatedDaysToThreshold <= 1),
            AvgDailyFillGrowthOverall = avgGrowth,
>>>>>>> origin/main:ADWebApplication/Services/Admin/BinPredictionService.cs

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
                continue;

            var latestCollection = history[0];
            var olderCollection = history[1];

            if (latestCollection.CurrentCollectionDateTime == null ||
                olderCollection.CurrentCollectionDateTime == null)
                continue;

            latestPredictionByBin.TryGetValue(binId, out var latestPrediction);

            if (!NeedsPredictionRefresh(latestPrediction, latestCollection))
                continue;

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

        var latestCollectionByBin = await GetLatestCollectionsAsync();
        var latestPredictionByBin = await GetLatestPredictionsAsync();

        var result = new List<BinPriorityDto>();

        foreach (var (binId, latest) in latestCollectionByBin)
        {
            if (!latestPredictionByBin.TryGetValue(binId, out var prediction))
            continue;

            if(latest.CurrentCollectionDateTime == null)
            continue;

            var daysElapsed = Math.Max(
                (today - latest.CurrentCollectionDateTime.Value).TotalDays, 0);

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
