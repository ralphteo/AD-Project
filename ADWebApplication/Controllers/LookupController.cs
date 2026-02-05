using ADWebApplication.Data;
using ADWebApplication.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ADWebApplication.Controllers
{
    [ApiController]
    [Route("api/lookup")]
    public class LookupController : ControllerBase
    {
        private readonly In5niteDbContext _context;

        public LookupController(In5niteDbContext context)
        {
            _context = context;
        }

        [HttpGet("bins")]
        public async Task<IActionResult> GetBins()
        {
            var latestPredictions = await _context.FillLevelPredictions
                .AsNoTracking()
                .OrderByDescending(p => p.PredictedDate)
                .ToListAsync();

            var latestByBin = latestPredictions
                .GroupBy(p => p.BinId)
                .ToDictionary(g => g.Key, g => g.First());

            var bins = await _context.CollectionBins
                .AsNoTracking()
                .ToListAsync();

            var result = bins.Select(bin => new
            {
                binId = bin.BinId,
                regionId = bin.RegionId,
                locationName = bin.LocationName,
                locationAddress = bin.LocationAddress,
                binStatus = bin.BinStatus,
                latitude = bin.Latitude,
                longitude = bin.Longitude,
                predictedStatus = latestByBin.TryGetValue(bin.BinId, out var prediction)
                    ? prediction.PredictedStatus
                    : null
            });

            return Ok(result);
        }

        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            return Ok(await _context.EWasteCategories
                .AsNoTracking()
                .Select(c => new { categoryId = c.CategoryId, categoryName = c.CategoryName })
                .ToListAsync());
        }

        [HttpGet("itemtypes")]
        public async Task<IActionResult> GetItemTypes([FromQuery] int categoryId)
        {
            return Ok(await _context.EWasteItemTypes
                .AsNoTracking()
                .Where(t => t.CategoryId == categoryId)
                .Select(t => new
                {
                    itemTypeId = t.ItemTypeId,
                    itemName = t.ItemName,
                    estimatedAvgWeight = t.EstimatedAvgWeight
                })
                .ToListAsync());
        }
    }
}
