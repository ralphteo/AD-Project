using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using ADWebApplication.Models;

namespace ADWebApplication.Data.Repository
{
    public class CampaignRepository : ICampaignRepository
    {
        private readonly In5niteDbContext _context;

        public CampaignRepository(In5niteDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Campaign>> GetAllCampaignsAsync()
        {
            return await _context.Campaigns
            .OrderByDescending(c => c.StartDate)
            .ToListAsync();
        }

        public async Task<Campaign?> GetCampaignByIdAsync(int campaignId)
        {
            return await _context.Campaigns
            .FirstOrDefaultAsync(c => c.CampaignId == campaignId);
        }

        public async Task<int> AddCampaignAsync(Campaign campaign)
        {
            await _context.Campaigns.AddAsync(campaign);
            await _context.SaveChangesAsync();
            return campaign.CampaignId;
        }

        public async Task<bool> UpdateCampaignAsync(Campaign campaign)
        {
            _context.Campaigns.Update(campaign);
            var rowsAffected = await _context.SaveChangesAsync();
            return rowsAffected > 0;
        }

        public async Task<bool> DeleteCampaignAsync(int campaignId)
        {
            var campaign = await _context.Campaigns
            .FirstOrDefaultAsync(c => c.CampaignId ==  campaignId);
            if (campaign == null) 
                return false;

            _context.Campaigns.Remove(campaign);
            var rowsAffected = await _context.SaveChangesAsync();
            return rowsAffected > 0;    
        }
        // Query methods
        public async Task<IEnumerable<Campaign>> GetActiveCampaignsAsync()
        {
            var now = DateTime.UtcNow;
            return await _context.Campaigns
            .Where(c => c.StartDate <= now && c.EndDate >= now && c.Status == "Active")
            .ToListAsync();
        }
        public async Task<IEnumerable<Campaign>> GetScheduledCampaignsAsync()
        {
            var now = DateTime.UtcNow;
            return await _context.Campaigns
            .Where(c => c.StartDate > now && c.Status == "Planned")
            .OrderBy(c => c.StartDate)
            .ToListAsync();
        }
        
        public async Task<IEnumerable<Campaign>> GetByStatusAsync(string status)
        {
            return await _context.Campaigns
            .Where(c => c.Status == status)
            .OrderByDescending(c => c.StartDate)
            .ToListAsync();
        }
        public async Task<Campaign?> GetCurrentCampaignAsync()
        {
            var now = DateTime.UtcNow;
            return await _context.Campaigns
            .Where(c => c.StartDate <= now && c.EndDate >= now && c.Status == "Active")
            .OrderBy(c => c.StartDate)
            .FirstOrDefaultAsync();
        }
    }
}    