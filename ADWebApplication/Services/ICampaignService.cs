using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ADWebApplication.Models;

namespace ADWebApplication.Services
{
    public interface ICampaignService
    {
        //CRUD methods
        Task<IEnumerable<Campaign>> GetAllCampaignsAsync();
        Task<Campaign?> GetCampaignByIdAsync(int campaignId);
        Task<int>AddCampaignAsync(Campaign campaign);
        Task<bool> UpdateCampaignAsync(Campaign campaign);
        Task<bool> DeleteCampaignAsync(int campaignId);

        //Query methods
        Task<IEnumerable<Campaign>> GetActiveCampaignsAsync();
        Task<Campaign?> GetCurrentCampaignAsync();
        Task<bool> ActivateCampaignAsync(int campaignId);
        Task<bool> DeactivateCampaignAsync(int campaignId);
        Task<decimal> CalculateTotalIncentivesAsync(int basePoints, int? campaignId = null);
       }
}