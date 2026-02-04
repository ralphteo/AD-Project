using System.Drawing;
using ADWebApplication.Data;
using ADWebApplication.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ADWebApplication.Controllers
{
    [ApiController]
    [Route("api/rewards")]
    public class RewardsController : ControllerBase
    {
        private readonly LogDisposalDbContext _context;

        public RewardsController(LogDisposalDbContext context)
        {
            _context = context;
        }

        // GET: /api/rewards/summary?userId=1
        [HttpGet("summary")]
        public async Task<ActionResult<RewardsSummaryDto>> GetSummary([FromQuery] int userId)
        {
            var wallet = await _context.RewardWallets
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.UserId == userId);

            if (wallet == null)
                return Ok(new RewardsSummaryDto());

            // Total disposals = from DisposalLogs
            var totalDisposals = await _context.DisposalLogs
                .AsNoTracking()
                .CountAsync(d => d.UserId == userId);

            // Total redeemed = completed negative transactions
            var totalRedeemed = await _context.PointTransactions
                .AsNoTracking()
                .CountAsync(t =>
                    t.WalletId == wallet.WalletId &&
                    t.Status == "COMPLETED" &&
                    t.Points < 0
                );

            return Ok(new RewardsSummaryDto
            {
                TotalPoints = wallet.AvailablePoints,
                TotalDisposals = totalDisposals,
                TotalRedeemed = totalRedeemed,
                TotalReferrals = 0,
                ExpiringSoonPoints = 0,
                NearestExpiryDate = null
            });
        }

        // GET: /api/rewards/history?userId=1
        [HttpGet("history")]
        public async Task<ActionResult<List<RewardsHistoryDto>>> GetHistory([FromQuery] int userId)
        {
            var wallet = await _context.RewardWallets
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.UserId == userId);

            if (wallet == null)
                return Ok(new List<RewardsHistoryDto>());

            var history = await _context.PointTransactions
                .AsNoTracking()
                .Where(t => t.WalletId == wallet.WalletId && t.Status == "COMPLETED")
                .OrderByDescending(t => t.CreatedDateTime)
                .Select(t => new RewardsHistoryDto
                {
                    TransactionId = t.TransactionId,
                    Title = t.Points > 0
                        ? (t.DisposalLog != null
                            ? (t.DisposalLog.DisposalLogItem.ItemType != null
                                ? t.DisposalLog.DisposalLogItem.ItemType.ItemName
                                : "E-Waste Disposal")
                            : "E-Waste Disposal")
                        : "Redeemed Rewards",
                    CategoryName = t.DisposalLog != null
                        ? (t.DisposalLog.DisposalLogItem.ItemType != null
                            ? t.DisposalLog.DisposalLogItem.ItemType.Category.CategoryName
                            : "Other")
                        : "Other",
                    Points = t.Points,
                    CreatedAt = t.CreatedDateTime
                })
                .ToListAsync();

            return Ok(history);
        }
    }
}
