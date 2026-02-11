using ADWebApplication.Data;
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

        var result = bins.Select(bin =>
        {
            double? estimatedFillLevel = null;
            string? riskLevel = null;
            int? daysToThreshold = null;

            if (latestByBin.TryGetValue(bin.BinId, out var prediction) &&
                latestCollectionByBin.TryGetValue(bin.BinId, out var lastCollection))
            {
                var daysElapsed = Math.Max((today - lastCollection.CurrentCollectionDateTime!.Value).TotalDays, 0);
                estimatedFillLevel = Math.Clamp(prediction.PredictedAvgDailyGrowth * daysElapsed, 0, 100);

                var remaining = 80 - estimatedFillLevel.Value;
                daysToThreshold = estimatedFillLevel >= 80
                    ? 0
                    : (int)Math.Ceiling(remaining / prediction.PredictedAvgDailyGrowth);

                riskLevel = daysToThreshold <= 3
                    ? "High"
                    : daysToThreshold <= 7
                        ? "Medium"
                        : "Low";
            }

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
        }).ToList();

        return result;
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
