using System.Drawing;
using ADWebApplication.Data;
using ADWebApplication.Models.DTOs;
using ADWebApplication.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ADWebApplication.Controllers
{
    [ApiController]
    [Route("api/rewards")]
    public class RewardsController : ControllerBase
    {
        private readonly In5niteDbContext _context;

        public RewardsController(In5niteDbContext context)
        {
            _context = context;
        }

        // GET: /api/rewards/summary?userId=1
        [HttpGet("summary")]
        public async Task<ActionResult<RewardsSummaryDto>> GetSummary([FromQuery] int userId)
        {
            var wallet = await _context.RewardWallet
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
            if (userId <= 0)
                return BadRequest("Invalid userId.");

            var wallet = await _context.RewardWallet
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.UserId == userId);

            if (wallet == null)
                return Ok(new List<RewardsHistoryDto>());

            var history = await _context.PointTransactions
                .AsNoTracking()
                .Where(t => t.WalletId == wallet.WalletId && t.Status == "COMPLETED")
                .OrderByDescending(t => t.CreatedDateTime)
                .Select(t => new
                {
                    TransactionId = t.TransactionId,
                    Points = t.Points,
                    CreatedAt = t.CreatedDateTime,
                    ItemName = (t.DisposalLog != null && t.DisposalLog.DisposalLogItem.ItemType != null)
                        ? t.DisposalLog.DisposalLogItem.ItemType.ItemName
                        : null,
                    Category = (t.DisposalLog != null && t.DisposalLog.DisposalLogItem.ItemType != null)
                        ? t.DisposalLog.DisposalLogItem.ItemType.Category.CategoryName
                        : null
                })
                .Select(x => new RewardsHistoryDto
                {
                    TransactionId = x.TransactionId,
                    Title = x.Points > 0 ? (x.ItemName ?? "E-Waste Disposal") : "Redeemed Rewards",
                    CategoryName = x.Category ?? "Other",
                    Points = x.Points,
                    CreatedAt = x.CreatedAt
                })
                .ToListAsync();

            return Ok(history);
        }

        // GET: /api/rewards/wallet?userId=1
        [HttpGet("wallet")]
        public async Task<ActionResult<RewardWalletDto>> GetWallet([FromQuery] int userId)
        {
            var wallet = await _context.RewardWallet
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.UserId == userId);

            if (wallet == null)
            {
                return Ok(new RewardWalletDto { UserId = userId, AvailablePoints = 0 });
            }

            return Ok(new RewardWalletDto
            {
                WalletId = wallet.WalletId,
                UserId = wallet.UserId,
                AvailablePoints = wallet.AvailablePoints
            });
        }

        // GET: /api/rewards/catalogue
        [HttpGet("catalogue")]
        public async Task<ActionResult<List<RewardCatalogueDto>>> GetCatalogue()
        {
            var rewards = await _context.RewardCatalogues
                .AsNoTracking()
                .Where(r => r.Availability && r.StockQuantity > 0)
                .OrderBy(r => r.RewardId)
                .Select(r => new RewardCatalogueDto
                {
                    RewardId = r.RewardId,
                    RewardName = r.RewardName!,
                    Description = r.Description!,
                    Points = r.Points,
                    RewardCategory = r.RewardCategory!,
                    StockQuantity = r.StockQuantity,
                    ImageUrl = r.ImageUrl!,
                    Availability = r.Availability
                })
                .ToListAsync();

            return Ok(rewards);
        }

        // POST: /api/rewards/redeem
        [HttpPost("redeem")]
        public async Task<ActionResult<RedeemResponseDto>> Redeem([FromBody] RedeemRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (request == null)
            {
                return BadRequest(new RedeemResponseDto { Success = false, Message = "Invalid request" });
            }

            var wallet = await _context.RewardWallet
                .FirstOrDefaultAsync(w => w.UserId == request.UserId);

            if (wallet == null)
            {
                return Ok(new RedeemResponseDto { Success = false, Message = "Wallet not found" });
            }

            var reward = await _context.RewardCatalogues
                .FirstOrDefaultAsync(r => r.RewardId == request.RewardId);

            if (reward == null)
            {
                return Ok(new RedeemResponseDto { Success = false, Message = "Reward not found" });
            }

            if (!reward.Availability || reward.StockQuantity <= 0)
            {
                return Ok(new RedeemResponseDto { Success = false, Message = "Reward unavailable" });
            }

            if (wallet.AvailablePoints < reward.Points)
            {
                return Ok(new RedeemResponseDto { Success = false, Message = "Insufficient points" });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                wallet.AvailablePoints -= reward.Points;
                reward.StockQuantity -= 1;

                var now = DateTime.UtcNow;
                var redemption = new RewardRedemption
                {
                    RewardId = reward.RewardId,
                    WalletId = wallet.WalletId,
                    UserId = wallet.UserId,
                    PointsUsed = reward.Points,
                    RedemptionStatus = "COMPLETED",
                    RedemptionDateTime = now,
                    FulfilledDatetime = now
                };

                var transactionRow = new PointTransaction
                {
                    WalletId = wallet.WalletId,
                    LogId = null,
                    TransactionDate = now,
                    TransactionType = "Reward Redemption",
                    Status = "Completed",
                    Points = -reward.Points,
                    CreatedDateTime = now
                };

                _context.RewardRedemptions.Add(redemption);
                _context.PointTransactions.Add(transactionRow);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new RedeemResponseDto
                {
                    Success = true,
                    Message = "Redeemed successfully",
                    RemainingPoints = wallet.AvailablePoints,
                    RedemptionId = redemption.RedemptionId
                });
            }
            catch
            {
                await transaction.RollbackAsync();
                return Ok(new RedeemResponseDto { Success = false, Message = "Redeem failed" });
            }
        }

        // GET: /api/rewards/redemptions?userId=1
        [HttpGet("redemptions")]
        public async Task<ActionResult<List<RewardRedemptionItemDto>>> GetRedemptions([FromQuery] int userId)
        {
            var redemptions = await _context.RewardRedemptions
                .AsNoTracking()
                .Where(r => r.UserId == userId)
                .Join(
                    _context.RewardCatalogues.AsNoTracking(),
                    redemption => redemption.RewardId,
                    reward => reward.RewardId,
                    (redemption, reward) => new RewardRedemptionItemDto
                    {
                        RedemptionId = redemption.RedemptionId,
                        RewardId = reward.RewardId,
                        RewardName = reward.RewardName!,
                        ImageUrl = reward.ImageUrl!,
                        PointsUsed = redemption.PointsUsed,
                        RedemptionStatus = redemption.RedemptionStatus!,
                        RedemptionDateTime = redemption.RedemptionDateTime
                    }
                )
                .OrderByDescending(r => r.RedemptionDateTime)
                .ToListAsync();

            return Ok(redemptions);
        }
    }
}
