using ADWebApplication.Data;
using ADWebApplication.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace ADWebApplication.Services;

public class WalletService : IWalletService
{
    private readonly In5niteDbContext _context;

    public WalletService(In5niteDbContext context)
    {
        _context = context;
    }

    public async Task<RewardsSummaryDto> GetSummaryAsync(int userId)
    {
        var wallet = await _context.RewardWallet
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.UserId == userId);

        if (wallet == null)
            return new RewardsSummaryDto();

        var totalDisposals = await _context.DisposalLogs
            .AsNoTracking()
            .CountAsync(d => d.UserId == userId);

        var totalRedeemed = await _context.PointTransactions
            .AsNoTracking()
            .CountAsync(t =>
                t.WalletId == wallet.WalletId &&
                t.Status == "COMPLETED" &&
                t.Points < 0
            );

        return new RewardsSummaryDto
        {
            TotalPoints = wallet.AvailablePoints,
            TotalDisposals = totalDisposals,
            TotalRedeemed = totalRedeemed,
            TotalReferrals = 0,
            ExpiringSoonPoints = 0,
            NearestExpiryDate = null
        };
    }

    public async Task<List<RewardsHistoryDto>> GetHistoryAsync(int userId)
    {
        var wallet = await _context.RewardWallet
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.UserId == userId);

        if (wallet == null)
            return new List<RewardsHistoryDto>();

        return await _context.PointTransactions
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
    }

    public async Task<RewardWalletDto> GetWalletAsync(int userId)
    {
        var wallet = await _context.RewardWallet
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.UserId == userId);

        if (wallet == null)
        {
            return new RewardWalletDto { UserId = userId, AvailablePoints = 0 };
        }

        return new RewardWalletDto
        {
            WalletId = wallet.WalletId,
            UserId = wallet.UserId,
            AvailablePoints = wallet.AvailablePoints
        };
    }
}
