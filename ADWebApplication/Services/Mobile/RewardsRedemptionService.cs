using ADWebApplication.Data;
using ADWebApplication.Models;
using ADWebApplication.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace ADWebApplication.Services;

public class RewardsRedemptionService : IRewardsRedemptionService
{
    private readonly In5niteDbContext _context;
    private const string VendorCode = "0000";

    public RewardsRedemptionService(In5niteDbContext context)
    {
        _context = context;
    }

    public async Task<RedeemResponseDto> RedeemAsync(int userId, RedeemRequestDto request)
    {
        if (request == null)
            return new RedeemResponseDto { Success = false, Message = "Invalid request" };

        var wallet = await _context.RewardWallet
            .FirstOrDefaultAsync(w => w.UserId == userId);

        if (wallet == null)
        {
            return new RedeemResponseDto { Success = false, Message = "Wallet not found" };
        }

        var reward = await _context.RewardCatalogues
            .FirstOrDefaultAsync(r => r.RewardId == request.RewardId);

        if (reward == null)
        {
            return new RedeemResponseDto { Success = false, Message = "Reward not found" };
        }

        if (!reward.Availability || reward.StockQuantity <= 0)
        {
            return new RedeemResponseDto { Success = false, Message = "Reward unavailable" };
        }

        if (wallet.AvailablePoints < reward.Points)
        {
            return new RedeemResponseDto { Success = false, Message = "Insufficient points" };
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
                UserId = userId,
                PointsUsed = reward.Points,
                RedemptionStatus = "ACTIVE",
                RedemptionDateTime = now,
                FulfilledDatetime = null
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

            return new RedeemResponseDto
            {
                Success = true,
                Message = "Redeemed successfully",
                RemainingPoints = wallet.AvailablePoints,
                RedemptionId = redemption.RedemptionId
            };
        }
        catch
        {
            await transaction.RollbackAsync();
            return new RedeemResponseDto { Success = false, Message = "Redeem failed" };
        }
    }

    public async Task<List<RewardRedemptionItemDto>> GetRedemptionsAsync(int userId)
    {
        return await _context.RewardRedemptions
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
    }

    public async Task<UseRedemptionResponseDto> UseRedemptionAsync(int userId, UseRedemptionRequestDto request)
    {
        if (request == null || request.RedemptionId <= 0)
            return new UseRedemptionResponseDto { Success = false, Message = "Invalid request" };

        if (string.IsNullOrWhiteSpace(request.VendorCode))
            return new UseRedemptionResponseDto { Success = false, Message = "Vendor code required" };

        if (request.VendorCode != VendorCode)
            return new UseRedemptionResponseDto { Success = false, Message = "Invalid vendor code" };

        var redemption = await _context.RewardRedemptions
            .FirstOrDefaultAsync(r => r.RedemptionId == request.RedemptionId);

        if (redemption == null)
            return new UseRedemptionResponseDto { Success = false, Message = "Redemption not found" };

        if (redemption.UserId != userId)
            return new UseRedemptionResponseDto { Success = false, Message = "Forbidden" };

        if (string.Equals(redemption.RedemptionStatus, "USED", StringComparison.OrdinalIgnoreCase))
            return new UseRedemptionResponseDto { Success = false, Message = "Redemption already used" };

        redemption.RedemptionStatus = "USED";
        redemption.FulfilledDatetime = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return new UseRedemptionResponseDto { Success = true, Message = "Redemption marked as used" };
    }
}
