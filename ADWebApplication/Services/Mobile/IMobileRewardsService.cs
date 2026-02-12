using ADWebApplication.Models.DTOs;

namespace ADWebApplication.Services;

public interface IMobileRewardsService
{
    Task<RewardsSummaryDto> GetSummaryAsync(int userId);
    Task<List<RewardsHistoryDto>> GetHistoryAsync(int userId);
    Task<RewardWalletDto> GetWalletAsync(int userId);
    Task<List<RewardCatalogueDto>> GetCatalogueAsync();
    Task<RedeemResponseDto> RedeemAsync(int userId, RedeemRequestDto request);
    Task<List<RewardRedemptionItemDto>> GetRedemptionsAsync(int userId);
    Task<UseRedemptionResponseDto> UseRedemptionAsync(int userId, UseRedemptionRequestDto request);
}
