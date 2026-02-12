using ADWebApplication.Models.DTOs;

namespace ADWebApplication.Services;

public interface IRewardsRedemptionService
{
    Task<RedeemResponseDto> RedeemAsync(int userId, RedeemRequestDto request);
    Task<List<RewardRedemptionItemDto>> GetRedemptionsAsync(int userId);
    Task<UseRedemptionResponseDto> UseRedemptionAsync(int userId, UseRedemptionRequestDto request);
}
