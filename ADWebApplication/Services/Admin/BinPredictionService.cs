using System.Net.Http.Json;
using ADWebApplication.Data;
using ADWebApplication.Models.DTOs;
using ADWebApplication.Models.ViewModels.BinPredictions;
using ADWebApplication.Models;
using Microsoft.EntityFrameworkCore;

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

    // Get the lastest collection records for all bins
    private async Task<Dictionary<int, CollectionDetails>> GetLatestCollectionsAsync()
    {
        var records = await db.CollectionDetails
            .Where(cd => cd.BinId != null)
            .ToListAsync();

        return records
            .GroupBy(cd => cd.BinId!.Value)
            .Select(g => g.OrderByDescending(x => x.CurrentCollectionDateTime).First())
            .ToDictionary(x => x.BinId!.Value, x => x);
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

        var latestCollectionByBin = await GetLatestCollectionsAsync();
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
            // Retrieve latest collection record
            if (!latestCollectionByBin.TryGetValue(bin.BinId, out var latest))
                continue;

            latestPredictionByBin.TryGetValue(bin.BinId, out var latestPrediction);
            nextStopByBin.TryGetValue(bin.BinId, out var nextStop);

            if (NeedsPredictionRefresh(latestPrediction, latest))
            {
                newCycleDetectedCount++;
                continue;
            }

            if (latestPrediction == null ||
                latest.CycleDurationDays == null ||
                latest.CycleStartMonth == null ||
                latest.CurrentCollectionDateTime == null)
            {
                missingPredictionCount++;
                continue;
            }

            double predictedGrowth = latestPrediction.PredictedAvgDailyGrowth;

            // Count no. of days since last collection
            var daysElapsed = Math.Max((today - latest.CurrentCollectionDateTime.Value).TotalDays, 0);

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

            // A bin is considered scheduled if a planned collection exists
            bool isScheduled =
                nextStop?.PlannedCollectionTime >= today &&
                nextStop.PlannedCollectionTime > latest.CurrentCollectionDateTime;

            var planningStatus = isScheduled ? "Scheduled" : "Not Scheduled";
            var riskLevel = GetRiskLevel(daysTo80);

            // Auto-select if high risk and unscheduled
            var autoSelected = riskLevel == "High" && !isScheduled;

            rows.Add(new BinPredictionsTableViewModel
            {
                BinId = bin.BinId,
                Region = bin.Region?.RegionName,
                LastCollectionDateTime = latest.CurrentCollectionDateTime.Value,

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
        var avgGrowth = rows.Any()
            ? rows.Average(r => r.PredictedNextAvgDailyGrowth)
            : 0;

        // filters
        IEnumerable<BinPredictionsTableViewModel> query = rows;

        if (risk != "All")
            query = query.Where(r => r.RiskLevel == risk);

        if (timeframe == "3")
            query = query.Where(r => r.EstimatedDaysToThreshold <= 3);

        else if (timeframe == "7")
            query = query.Where(r => r.EstimatedDaysToThreshold <= 7);

        // sorting
        bool isDesc = sortDir == "desc";

        if (sort == "EstimatedFill")
        {
            if (isDesc)
            {
                query = query.OrderByDescending(r => r.EstimatedFillToday);
            }
            else
            {
                query = query.OrderBy(r => r.EstimatedFillToday);
            }
        }
        else if (sort == "AvgGrowth")
        {
            if (isDesc)
            {
                query = query.OrderByDescending(r => r.PredictedNextAvgDailyGrowth);
            }
            else
            {
                query = query.OrderBy(r => r.PredictedNextAvgDailyGrowth);
            }
        }
        else
        {
            // default sort
            if (isDesc)
            {
                query = query.OrderByDescending(r => r.EstimatedDaysToThreshold);
            }
            else
            {
                query = query.OrderBy(r => r.EstimatedDaysToThreshold);
            }
        }

        //pagination
        //counts no. of rows after filtering/sorting
        var totalItems = query.Count();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        if (totalPages == 0)
        {
            page = 1;
        }
        else
        {
            page = Math.Clamp(page, 1, totalPages);
        }
        
        var pagedRows = query
            .Skip((page - 1) * pageSize) //skip  rows belonging to the prev. page
            .Take(pageSize) //limit no. of rows displayed to 10
            .ToList();

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

            NewCycleDetectedCount = newCycleDetectedCount,
            MissingPredictionCount = missingPredictionCount
        };
    }

    public async Task<int> RefreshPredictionsForNewCyclesAsync()
    {
        var now = DateTimeOffset.UtcNow;

        var latestCollectionByBin = await GetLatestCollectionsAsync();
        var latestPredictionByBin = await GetLatestPredictionsAsync();

        int refreshed = 0;

        foreach (var kvp in latestCollectionByBin)
        {
            int binId = kvp.Key;
            var latestCollection = kvp.Value;

            latestPredictionByBin.TryGetValue(binId, out var latestPrediction);

            if (!NeedsPredictionRefresh(latestPrediction, latestCollection))
                continue;

            if (latestCollection.CycleDurationDays == null ||
                latestCollection.CycleStartMonth == null ||
                latestCollection.CurrentCollectionDateTime == null)
                continue;

            // Call ML
            var req = new MLPredictionRequestDto
            {
                container_id = binId.ToString(),
                collection_fill_percentage = latestCollection.BinFillLevel,
                cycle_duration_days = latestCollection.CycleDurationDays.Value,
                cycle_start_month = latestCollection.CycleStartMonth.Value
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
}
