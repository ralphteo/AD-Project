using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ADWebApplication.Models;
using ADWebApplication.Data.Repository;
using Microsoft.Extensions.Logging;

namespace ADWebApplication.Services
{
    public class RewardCatalogueService : IRewardCatalogueService
    {
        private readonly IRewardCatalogueRepository _repository;
        private readonly ILogger<RewardCatalogueService> _logger;

        public RewardCatalogueService(IRewardCatalogueRepository repository, ILogger<RewardCatalogueService> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<RewardCatalogue>> GetAllRewardsAsync()
        {
            if(_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Fetching all rewards from the catalogue.");
            }
            return await _repository.GetAllRewardsAsync();
        }
        public async Task<RewardCatalogue?> GetRewardByIdAsync(int rewardId)
        {
            if(_logger.IsEnabled(LogLevel.Information))
            {   
            _logger.LogInformation("Fetching reward with ID {RewardId}.", rewardId);
            }
            return await _repository.GetRewardByIdAsync(rewardId);
        }
        public async Task<int> AddRewardAsync(RewardCatalogue reward)
        {
            if(_logger.IsEnabled(LogLevel.Information))
            {
            _logger.LogInformation("Adding new reward: {RewardName} to the catalogue.", reward.RewardName);
            }
            if (reward.Points == 0)
            {
                throw new InvalidOperationException("Reward points must be greater than zero.");
            }
            if (reward.StockQuantity < 0)
            {
                throw new InvalidOperationException("Stock quantity cannot be negative.");
            }
            var rewardId = await _repository.AddRewardAsync(reward);
            if(_logger.IsEnabled(LogLevel.Information))
            {
            _logger.LogInformation("Reward added with ID {RewardId}.", rewardId);
            }
            return rewardId;
        }
        public async Task<bool> UpdateRewardAsync(RewardCatalogue reward)
        {
            if(_logger.IsEnabled(LogLevel.Information))
            {
            _logger.LogInformation("Updating reward with ID {RewardId}.", reward.RewardId);
            }
          
            if (reward.StockQuantity == 0 && reward.Availability)
            {
                reward.Availability = false;
                if(_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Reward with ID {RewardId} is out of stock. Setting availability to false.", reward.RewardId);
                }
            }
            return await _repository.UpdateRewardAsync(reward);
        }
        public async Task<bool> DeleteRewardAsync(int rewardId)
        {
            if(_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Deleting reward with ID {RewardId}.", rewardId);
            }
            return await _repository.DeleteRewardAsync(rewardId);
        }
        public async Task<IEnumerable<RewardCatalogue>> GetAvailableRewardsAsync()
        {
            if(_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Fetching all available rewards from the catalogue.");
            }
            return await _repository.GetAvailableRewardsAsync();
        }
        public async Task<IEnumerable<string>> GetAllRewardCategoriesAsync()
        {
            if(_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Fetching all reward categories from the catalogue.");
            }
            return await _repository.GetAllRewardCategoriesAsync();
        }
        public async Task<bool> CheckRewardAvailabilityAsync(int rewardId, int quantity = 1)
        {
            var reward = await _repository.GetRewardByIdAsync(rewardId);
            if (reward == null)
            {
                _logger.LogWarning("Reward with ID {RewardId} not found.", rewardId);
                return false;
            }
            var isAvailable = reward.Availability && reward.StockQuantity >= quantity;
            if(_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Reward ID {RewardId} availability: {IsAvailable}, Stock Quantity: {StockQuantity}.", rewardId, isAvailable, reward.StockQuantity);
            }
            return isAvailable;
        }
        public async Task<IEnumerable<RewardCatalogue>> GetRewardsByCategoryAsync(string category)
        {
            if(_logger.IsEnabled(LogLevel.Information))
            {
            _logger.LogInformation("Fetching rewards in category: {Category}.", category);
            }
            return await _repository.GetRewardsByCategoryAsync(category);
        }
        
    }
}