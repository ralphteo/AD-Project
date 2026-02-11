using ADWebApplication.Data;
using ADWebApplication.Models.DTOs;
using ADWebApplication.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using System.Security.Claims;

namespace ADWebApplication.Controllers
{
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/disposallogs")]
    [EnableRateLimiting("mobile")]
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

            var tokenUserId = GetUserIdFromToken();
            if (tokenUserId == null)
                return Unauthorized();
            if (tokenUserId != request.UserId)
                return Forbid();

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
            var tokenUserId = GetUserIdFromToken();
            if (tokenUserId == null)
                return Unauthorized();
            if (tokenUserId != userId)
                return Forbid();

            DateTime? from = null;
            var now = DateTime.UtcNow;

            if (range == "month")
            {
                from = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            }
            else if (range == "last 3")
            {
                from = now.AddMonths(-3);
            }

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

        private int? GetUserIdFromToken()
        {
            var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claimValue, out var userId) ? userId : null;
        }
    }
}
