using ADWebApplication.Data;
using ADWebApplication.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ADWebApplication.Models.LogDisposal;


namespace ADWebApplication.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DisposalLogsController : ControllerBase
    {
        private readonly LogDisposalDbContext _context;

        public DisposalLogsController(LogDisposalDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<List<DisposalHistoryDto>>> GetHistory()
        {
            var result = await _context.DisposalLogs
                .AsNoTracking()
                .OrderByDescending(l => l.DisposalTimeStamp)
                .Select(l => new DisposalHistoryDto
                {
                    LogId = l.LogId,
                    BinId = l.BinId,
                    BinLocationName = l.CollectionBin != null ? l.CollectionBin.LocationName : null,

                    ItemTypeId = l.DisposalLogItem.ItemTypeId,
                    ItemTypeName = l.DisposalLogItem.ItemType.ItemName,

                    SerialNo = l.DisposalLogItem.SerialNo,

                    EstimatedTotalWeight = l.EstimatedTotalWeight,
                    DisposalTimeStamp = l.DisposalTimeStamp,
                    Feedback = l.Feedback
                })
                .ToListAsync();

            return Ok(result);
        }
         [HttpPost]
        public async Task<IActionResult> CreateDisposalLog(
            [FromBody] CreateDisposalLogRequest request)
        {
            if (request.ItemTypeId <= 0)
                return BadRequest("Invalid item type.");

            if (string.IsNullOrWhiteSpace(request.SerialNo))
                return BadRequest("Serial number is required.");

            if (request.EstimatedWeightKg <= 0)
                return BadRequest("Estimated weight must be > 0.");

            var itemTypeExists = await _context.EWasteItemTypes
                .AnyAsync(x => x.ItemTypeId == request.ItemTypeId);

            if (!itemTypeExists)
                return BadRequest("Item type does not exist.");

            if (request.BinId.HasValue)
            {
                var binExists = await _context.CollectionBins
                    .AnyAsync(b => b.BinId == request.BinId.Value);

                if (!binExists)
                    return BadRequest("Collection bin does not exist.");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var disposalLog = new DisposalLogs
                {
                    BinId = request.BinId,
                    UserId = null, 
                    EstimatedTotalWeight = request.EstimatedWeightKg,
                    DisposalTimeStamp = DateTime.UtcNow,
                    Feedback = request.Feedback
                };

                _context.DisposalLogs.Add(disposalLog);
                await _context.SaveChangesAsync(); 

                var logItem = new DisposalLogItem
                {
                    LogId = disposalLog.LogId,
                    ItemTypeId = request.ItemTypeId,
                    SerialNo = request.SerialNo,
                };

                _context.DisposalLogItems.Add(logItem);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return CreatedAtAction(
                    nameof(CreateDisposalLog),
                    new { id = disposalLog.LogId },
                    new { disposalLog.LogId }
                );
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, "Failed to store disposal log.");
            }
        }
    }
}