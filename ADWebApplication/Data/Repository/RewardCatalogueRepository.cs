using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using ADWebApplication.Models;
using ADWebApplication.Data;

namespace ADWebApplication.Data.Repository
{
    
    public class RewardCatalogueRepository : IRewardCatalogueRepository
    {
        private readonly In5niteDbContext _context;

        public RewardCatalogueRepository(In5niteDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }
        public async Task<IEnumerable<RewardCatalogue>> GetAllRewardsAsync()
        {
            return await _context.RewardCatalogues
                .OrderBy(r => r.RewardCategory)
                .ThenBy(r => r.Points)
                .ToListAsync();
        }
        public async Task<RewardCatalogue?> GetRewardByIdAsync(int rewardId)
        {
            return await _context.RewardCatalogues
                .FirstOrDefaultAsync(r => r.RewardId == rewardId);
        }
        public async Task<int> AddRewardAsync(RewardCatalogue reward)
        {
            reward.CreatedDate = DateTime.UtcNow;
            reward.UpdatedDate = DateTime.UtcNow;

            await _context.RewardCatalogues.AddAsync(reward);
            await _context.SaveChangesAsync();

            return reward.RewardId;
        }
        public async Task<bool> UpdateRewardAsync(RewardCatalogue reward)
       /*  {
           reward.UpdatedDate = DateTime.UtcNow;

            _context.RewardCatalogues.Update(reward);
            var rowsAffected = await _context.SaveChangesAsync();

            return rowsAffected > 0;
        } */
        {
            var existing = await _context.RewardCatalogues
                .FirstOrDefaultAsync(r => r.RewardId == reward.RewardId);

            if (existing == null) return false;

            // Copy allowed fields only
            existing.RewardName = reward.RewardName;
            existing.Description = reward.Description;
            existing.Points = reward.Points;
            existing.RewardCategory = reward.RewardCategory;
            existing.StockQuantity = reward.StockQuantity;
            existing.ImageUrl = reward.ImageUrl;
            existing.Availability = reward.Availability;

            // System-managed fields
            existing.UpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<bool> DeleteRewardAsync(int rewardId)
        {
            var reward = await _context.RewardCatalogues
                .FirstOrDefaultAsync(r => r.RewardId == rewardId);


            if (reward == null)
                return false;
            

            _context.RewardCatalogues.Remove(reward);
            var rowsAffected = await _context.SaveChangesAsync();

            return rowsAffected > 0;
        }
        public async Task<IEnumerable<RewardCatalogue>> GetAvailableRewardsAsync()
        {
            return await _context.RewardCatalogues
                .Where(r => r.Availability && r.StockQuantity > 0)
                .OrderBy(r => r.RewardCategory)
                .ThenBy(r => r.Points)
                .ToListAsync();
        }
        public async Task<IEnumerable<RewardCatalogue>> GetRewardsByCategoryAsync(string category)
        {
            return await _context.RewardCatalogues
                .Where(r => r.RewardCategory == category)
                .OrderBy(r => r.Points)
                .ToListAsync();
        }
        public async Task<IEnumerable<string>> GetAllRewardCategoriesAsync()
        {
            return await _context.RewardCatalogues
                .Where(r => !string.IsNullOrEmpty(r.RewardCategory))
                .Select(r => r.RewardCategory)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
        }
        
    }
}