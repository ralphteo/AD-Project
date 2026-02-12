using ADWebApplication.Data;
using ADWebApplication.Models;
using ADWebApplication.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace ADWebApplication.Services;

public class MobileLookupService : IMobileLookupService
{
    private readonly In5niteDbContext _context;

    public MobileLookupService(In5niteDbContext context)
    {
        _context = context;
    }

    public async Task<List<MobileLookupBinDto>> GetBinsAsync()
    {
        var today = DateTimeOffset.UtcNow.Date;

        var latestPredictions = await _context.FillLevelPredictions
            .AsNoTracking()
            .OrderByDescending(p => p.PredictedDate)
            .ToListAsync();

        var latestByBin = latestPredictions
            .GroupBy(p => p.BinId)
            .ToDictionary(g => g.Key, g => g.First());

        var latestCollections = await _context.CollectionDetails
            .AsNoTracking()
            .Where(cd => cd.CurrentCollectionDateTime != null && cd.BinId.HasValue)
            .OrderByDescending(cd => cd.CurrentCollectionDateTime)
            .ToListAsync();

        var latestCollectionByBin = latestCollections
            .GroupBy(cd => cd.BinId!.Value)
            .ToDictionary(g => g.Key, g => g.First());

        var bins = await _context.CollectionBins
            .AsNoTracking()
            .ToListAsync();

        return bins
            .Select(bin => MapBinDto(bin, latestByBin, latestCollectionByBin, today))
            .ToList();
    }

    private static MobileLookupBinDto MapBinDto(
        CollectionBin bin,
        Dictionary<int, FillLevelPrediction> latestByBin,
        Dictionary<int, CollectionDetails> latestCollectionByBin,
        DateTime today)
    {
        var (estimatedFillLevel, riskLevel, daysToThreshold) =
            ResolveForecast(bin.BinId, latestByBin, latestCollectionByBin, today);

        return new MobileLookupBinDto
        {
            BinId = bin.BinId,
            RegionId = bin.RegionId,
            LocationName = bin.LocationName,
            LocationAddress = bin.LocationAddress,
            BinStatus = bin.BinStatus,
            Latitude = bin.Latitude,
            Longitude = bin.Longitude,
            PredictedStatus = riskLevel ?? "Unknown",
            EstimatedFillLevel = estimatedFillLevel.HasValue
                ? Math.Round(estimatedFillLevel.Value, 1)
                : null,
            RiskLevel = riskLevel,
            DaysToFull = daysToThreshold,
            IsNearlyFull = estimatedFillLevel >= 70
        };
    }

    private static (double? EstimatedFillLevel, string? RiskLevel, int? DaysToThreshold) ResolveForecast(
        int binId,
        Dictionary<int, FillLevelPrediction> latestByBin,
        Dictionary<int, CollectionDetails> latestCollectionByBin,
        DateTime today)
    {
        if (!latestByBin.TryGetValue(binId, out var prediction) ||
            !latestCollectionByBin.TryGetValue(binId, out var lastCollection))
        {
            return (null, null, null);
        }

        var daysElapsed = Math.Max((today - lastCollection.CurrentCollectionDateTime!.Value).TotalDays, 0);
        var estimatedFillLevel = Math.Clamp(prediction.PredictedAvgDailyGrowth * daysElapsed, 0, 100);

        var remaining = 80 - estimatedFillLevel;
        var daysToThreshold = estimatedFillLevel >= 80
            ? 0
            : (int)Math.Ceiling(remaining / prediction.PredictedAvgDailyGrowth);

        var riskLevel = daysToThreshold <= 3
            ? "High"
            : daysToThreshold <= 7
                ? "Medium"
                : "Low";

        return (estimatedFillLevel, riskLevel, daysToThreshold);
    }

    public async Task<List<MobileLookupCategoryDto>> GetCategoriesAsync()
    {
        return await _context.EWasteCategories
            .AsNoTracking()
            .Select(c => new MobileLookupCategoryDto
            {
                CategoryId = c.CategoryId,
                CategoryName = c.CategoryName
            })
            .ToListAsync();
    }

    public async Task<List<MobileLookupItemTypeDto>> GetItemTypesAsync(int categoryId)
    {
        return await _context.EWasteItemTypes
            .AsNoTracking()
            .Where(t => t.CategoryId == categoryId)
            .Select(t => new MobileLookupItemTypeDto
            {
                ItemTypeId = t.ItemTypeId,
                ItemName = t.ItemName,
                EstimatedAvgWeight = t.EstimatedAvgWeight
            })
            .ToListAsync();
    }
}
