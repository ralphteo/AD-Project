using ADWebApplication.Data;
using ADWebApplication.Models;
using ADWebApplication.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace ADWebApplication.Services;

public class MobileRewardsService : IMobileRewardsService
{
    private readonly In5niteDbContext _context;
    private readonly IWalletService _walletService;
    private readonly IRewardsRedemptionService _redemptionService;

    public MobileRewardsService(
        In5niteDbContext context,
        IWalletService walletService,
        IRewardsRedemptionService redemptionService)
    {
        _context = context;
        _walletService = walletService;
        _redemptionService = redemptionService;
    }

    public async Task<RewardsSummaryDto> GetSummaryAsync(int userId)
    {
        return await _walletService.GetSummaryAsync(userId);
    }

    public async Task<List<RewardsHistoryDto>> GetHistoryAsync(int userId)
    {
        return await _walletService.GetHistoryAsync(userId);
    }

    public async Task<RewardWalletDto> GetWalletAsync(int userId)
    {
        return await _walletService.GetWalletAsync(userId);
    }

    public async Task<List<RewardCatalogueDto>> GetCatalogueAsync()
    {
        return await _context.RewardCatalogues
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
    }

    public async Task<RedeemResponseDto> RedeemAsync(int userId, RedeemRequestDto request)
    {
        return await _redemptionService.RedeemAsync(userId, request);
    }

    public async Task<List<RewardRedemptionItemDto>> GetRedemptionsAsync(int userId)
    {
        return await _redemptionService.GetRedemptionsAsync(userId);
    }

    public async Task<UseRedemptionResponseDto> UseRedemptionAsync(int userId, UseRedemptionRequestDto request)
    {
        return await _redemptionService.UseRedemptionAsync(userId, request);
    }
}
