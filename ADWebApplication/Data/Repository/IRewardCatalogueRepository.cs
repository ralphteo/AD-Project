using System.Collections.Generic;
using System.Threading.Tasks;
using ADWebApplication.Models;

namespace ADWebApplication.Data.Repository
{
    public interface IRewardCatalogueRepository
    {
        Task<IEnumerable<RewardCatalogue>> GetAllRewardsAsync();
        Task<RewardCatalogue?> GetRewardByIdAsync(int rewardId);
        Task<int> AddRewardAsync(RewardCatalogue reward);
        Task<bool> UpdateRewardAsync(RewardCatalogue reward);
        Task<bool> DeleteRewardAsync(int rewardId);
        Task<IEnumerable<RewardCatalogue>> GetAvailableRewardsAsync();
        Task<IEnumerable<RewardCatalogue>> GetRewardsByCategoryAsync(string category);
        Task<IEnumerable<string>> GetAllRewardCategoriesAsync();
    }
}