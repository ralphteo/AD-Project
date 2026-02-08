using ADWebApplication.Data;
using ADWebApplication.Models.DTOs;
using ADWebApplication.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;

namespace ADWebApplication.Controllers
{
    [ApiController]
    [Route("api/disposallogs")]
    public class DisposalLogsController : ControllerBase
    {
        private readonly In5niteDbContext _context;

        public DisposalLogsController(In5niteDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateDisposalLogRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            using var tx = await _context.Database.BeginTransactionAsync();

            try
            {
                var log = new DisposalLogs
                {
                    BinId = request.BinId,
                    EstimatedTotalWeight = request.EstimatedWeightKg,
                    DisposalTimeStamp = DateTime.UtcNow,
                    Feedback = request.Feedback,
                    UserId = request.UserId
                };

                _context.DisposalLogs.Add(log);
                await _context.SaveChangesAsync();

                var item = new DisposalLogItem
                {
                    LogId = log.LogId,
                    ItemTypeId = request.ItemTypeId,
                    SerialNo = request.SerialNo,
                };

                _context.DisposalLogItems.Add(item);
                await _context.SaveChangesAsync();

                var itemType = await _context.EWasteItemTypes
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.ItemTypeId == request.ItemTypeId);

                var earnedPoints = itemType?.BasePoints ?? 0;

                var wallet = await _context.RewardWallet
                    .FirstOrDefaultAsync(w => w.UserId == request.UserId);

                if (wallet == null)
                {
                    wallet = new RewardWallet
                    {
                        UserId = request.UserId,
                        AvailablePoints = 0
                    };
                    _context.RewardWallet.Add(wallet);
                    await _context.SaveChangesAsync();
                }

                if (earnedPoints > 0)
                {
                    wallet.AvailablePoints += earnedPoints;
                    _context.PointTransactions.Add(new PointTransaction
                    {
                        WalletId = wallet.WalletId,
                        LogId = log.LogId,
                        TransactionType = "DISPOSAL",
                        Status = "COMPLETED",
                        Points = earnedPoints,
                        CreatedDateTime = DateTime.UtcNow
                    });
                }

                await _context.SaveChangesAsync();

                await tx.CommitAsync();
                return Ok(new { log.LogId, earnedPoints });
            }
            catch
            {
                await tx.RollbackAsync();
                return StatusCode(500, "Failed to create disposal log");
            }
        }
        [HttpGet("history")]
        public async Task<ActionResult<List<DisposalHistoryDto>>> GetHistory(
            [FromQuery] int userId,
            [FromQuery] string range = "all"
        )
        {
            DateTime? from = null;
            var now = DateTime.UtcNow;

            range = (range ?? "all").ToLowerInvariant();
            if (range == "month")
                from = new DateTime(now.Year, now.Month, 1,DateTimeKind.Utc);
            else if (range == "last 3")
                from = now.AddMonths(-3);

            var q = _context.DisposalLogs
            .AsNoTracking()
            .Where(l => l.UserId == userId);

            if (from.HasValue)
                q = q.Where(l => l.DisposalTimeStamp >= from.Value);

            var result = await q
                .OrderByDescending(l => l.DisposalTimeStamp)
                .Select(l => new DisposalHistoryDto
                {
                    LogId = l.LogId,
                    DisposalTimeStamp = l.DisposalTimeStamp,
                    EstimatedTotalWeight = l.EstimatedTotalWeight,
                    Feedback = l.Feedback,

                    BinId = l.BinId,
                    BinLocationName = l.CollectionBin != null ? l.CollectionBin.LocationName : null,
                    LocationName = l.CollectionBin != null ? l.CollectionBin.LocationName : null,

                    ItemTypeId = l.DisposalLogItem.ItemTypeId,
                    ItemTypeName = l.DisposalLogItem.ItemType != null ? l.DisposalLogItem.ItemType.ItemName : null,
                    SerialNo = l.DisposalLogItem.SerialNo,

                    CategoryName = (l.DisposalLogItem.ItemType != null && l.DisposalLogItem.ItemType.Category != null)
                        ? l.DisposalLogItem.ItemType.Category.CategoryName
                        : null,

                    // simple points formula (change later if needed)
                    EarnedPoints = (l.DisposalLogItem.ItemType != null)
                        ? l.DisposalLogItem.ItemType.BasePoints
                        : 0
                })
                .ToListAsync();

            return Ok(result);
        }
    }
}
