using System.Collections.Generic;
using System.Threading.Tasks;
using ADWebApplication.Models;

namespace ADWebApplication.Data.Repository
{
    public interface ICampaignRepository
    {
        Task<IEnumerable<Campaign>> GetAllCampaignsAsync();
        Task<Campaign?> GetCampaignByIdAsync(int campaignId);
        Task<int>AddCampaignAsync(Campaign campaign);
        Task<bool> UpdateCampaignAsync(Campaign campaign);
        Task<bool> DeleteCampaignAsync(int campaignId);


        //Query methods
        Task<IEnumerable<Campaign>> GetActiveCampaignsAsync();
        Task<IEnumerable<Campaign>> GetScheduledCampaignsAsync();
        Task<IEnumerable<Campaign>> GetByStatusAsync(string status);
        Task<Campaign?> GetCurrentCampaignAsync();

    }

}