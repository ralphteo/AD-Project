using System.Net.Http.Json;
using ADWebApplication.Data;
using ADWebApplication.Models.DTOs;
using ADWebApplication.Models.ViewModels.BinPredictions;
using ADWebApplication.Models;
using Microsoft.EntityFrameworkCore;

namespace ADWebApplication.Services;

public class BinPredictionService
{
    //to call ML API
    private readonly HttpClient _httpClient;
    private readonly In5niteDbContext _db;
    private const int Threshold = 80;

    public BinPredictionService(HttpClient httpClient, In5niteDbContext db)
    {
        _httpClient = httpClient;
        _db = db;
    }

    public async Task<BinPredictionsPageViewModel> BuildBinPredictionsPageAsync(int page, string sort, string sortDir, string risk, string timeframe)
    {
        const int pageSize = 10;
        var today = DateTimeOffset.UtcNow.Date;

        //fetch all bins from DB
        var bins = await _db.CollectionBins
            .Include(b => b.Region)
            .AsNoTracking()  //to load as read only info
            .ToListAsync();

        var rows = new List<BinPredictionsTableViewModel>();

        int newCycleDetectedCount = 0;

        foreach (var bin in bins)
        {
            //fetch most recent collection record for each bin
            var latest = await _db.CollectionDetails
                .AsNoTracking()
                .Where(cd => cd.BinId == bin.BinId)
                .OrderByDescending(cd => cd.CurrentCollectionDateTime)
                .FirstOrDefaultAsync();

            //to prevent runtime error if fields are incomplete
            if (latest == null ||
                latest.CycleDurationDays == null ||
                latest.CycleStartMonth == null ||
                latest.CurrentCollectionDateTime == null)
                continue;

            //checks if bin predictions need to be refreshed (Collection Date>Predicted Date)
            var latestPrediction = await _db.FillLevelPredictions
                .AsNoTracking()
                .Where(p => p.BinId == bin.BinId)
                .OrderByDescending(p => p.PredictedDate)
                .FirstOrDefaultAsync();

            bool needsMlRefresh =
                latestPrediction == null ||
                latestPrediction.PredictedDate < latest.CurrentCollectionDateTime;

            if (needsMlRefresh)
            {
                newCycleDetectedCount++;
            }

            //Fetch the next scheduled route plan if any (> last collection date && today)
            var nextRouteStop = await _db.RouteStops
                .Where(rs =>
                    rs.BinId == bin.BinId &&
                    rs.PlannedCollectionTime > latest.CurrentCollectionDateTime && 
                    rs.PlannedCollectionTime >= today                             
                )
                .OrderBy(rs => rs.PlannedCollectionTime)
                .FirstOrDefaultAsync();

            DateTimeOffset? lastCollectedAt = latest.CurrentCollectionDateTime;
            DateTimeOffset? nextPlannedAt = nextRouteStop?.PlannedCollectionTime;

            bool isScheduled =
                nextPlannedAt.HasValue &&
                lastCollectedAt.HasValue &&
                nextPlannedAt > lastCollectedAt &&
                nextPlannedAt >= today;

            var planningStatus = isScheduled ? "Scheduled" : "Not Scheduled";

            //ML request
            var req = new MLPredictionRequestDto
            {
                container_id = bin.BinId.ToString(),
                collection_fill_percentage = latest.BinFillLevel,
                cycle_duration_days = latest.CycleDurationDays.Value,
                cycle_start_month = latest.CycleStartMonth.Value
            };

            //ML response
            var response = await _httpClient.PostAsJsonAsync("/predict", req);
            response.EnsureSuccessStatusCode();

            var mlResponse = await response.Content.ReadFromJsonAsync<MLPredictionResponseDto>();

            var predictedGrowth = mlResponse.predicted_next_avg_daily_growth;

            //derive info from DB records and ML prediction
            var daysElapsed = Math.Max((today - latest.CurrentCollectionDateTime.Value).TotalDays, 0);

            var estimatedFillToday = Math.Clamp(predictedGrowth * daysElapsed, 0, 100);
            
            int daysTo80;

            if (estimatedFillToday >= Threshold)
            {
                daysTo80 = 0;
            }
            else
            {
                var remaining = Threshold - estimatedFillToday;
                daysTo80 = (int)Math.Ceiling(remaining / predictedGrowth);
            }

            //bins are auto-selected as urgent if it is predicted to reach 80% in 3 days or less
            bool autoSelected = daysTo80 <= 3;

            //create row in ViewModel
            rows.Add(new BinPredictionsTableViewModel
            {
                BinId = bin.BinId,
                Region = bin.Region?.RegionName,
                LastCollectionDateTime = latest.CurrentCollectionDateTime.Value,

                PredictedNextAvgDailyGrowth = predictedGrowth,
                EstimatedFillToday = estimatedFillToday,
                EstimatedDaysToThreshold = daysTo80,

                //WIP
                PlanningStatus = planningStatus,
                RouteId = isScheduled ? nextRouteStop!.RouteId.ToString() : null
            });
        }

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

        query = sort switch
        {
            "EstimatedFill" => isDesc
                ? query.OrderByDescending(r => r.EstimatedFillToday)
                : query.OrderBy(r => r.EstimatedFillToday),

            "AvgGrowth" => isDesc
                ? query.OrderByDescending(r => r.PredictedNextAvgDailyGrowth)
                : query.OrderBy(r => r.PredictedNextAvgDailyGrowth),

            _ => isDesc
                ? query.OrderByDescending(r => r.EstimatedDaysToThreshold)
                : query.OrderBy(r => r.EstimatedDaysToThreshold)
        };

        //pagination
        //counts no. of rows after filtering/sorting
        var totalItems = query.Count(); 
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var pagedRows = query
            .Skip((page - 1) * pageSize) //skip rows belonging to the prev. page
            .Take(pageSize) //limit no. of rows displayed to 10
            .ToList();

        return new BinPredictionsPageViewModel
        {
            Rows = pagedRows,
            TotalBins = totalItems,
            HighPriorityBins = query.Count(r => r.EstimatedDaysToThreshold <= 3),

            SelectedRisk = risk,
            SelectedTimeframe = timeframe,
            SortBy = sort,

            CurrentPage = page,
            TotalPages = totalPages,

            HighRiskUnscheduledCount = rows.Count(r =>
                r.RiskLevel == "High" && r.PlanningStatus == "Not Scheduled"
            ),

            //WIP
            NewCycleDetectedCount = newCycleDetectedCount
        };
    }
}
