using ADWebApplication.Models.DTOs;

namespace ADWebApplication.Services;

public interface IWalletService
{
    Task<RewardsSummaryDto> GetSummaryAsync(int userId);
    Task<List<RewardsHistoryDto>> GetHistoryAsync(int userId);
    Task<RewardWalletDto> GetWalletAsync(int userId);
}
