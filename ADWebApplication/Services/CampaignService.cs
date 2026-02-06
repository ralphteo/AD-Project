using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ADWebApplication.Models;
using ADWebApplication.Data.Repository;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client.Extensibility;

namespace ADWebApplication.Services
{
    public class CampaignService : ICampaignService
    {
        private readonly ICampaignRepository _campaignRepository;
        private readonly ILogger<CampaignService> _logger;

        public CampaignService(ICampaignRepository campaignRepository, ILogger<CampaignService> logger)
        {
            _campaignRepository = campaignRepository??throw new ArgumentNullException(nameof(campaignRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<Campaign>> GetAllCampaignsAsync()
        {
            _logger.LogInformation("Retrieving all campaigns.");
            return await _campaignRepository.GetAllCampaignsAsync();
        }

        public async Task<Campaign?> GetCampaignByIdAsync(int campaignId)
        {
            _logger.LogInformation($"Retrieving campaign with ID: {campaignId}");
            return await _campaignRepository.GetCampaignByIdAsync(campaignId);
        }

        public async Task<int> AddCampaignAsync(Campaign campaign)
        {
            _logger.LogInformation("Adding a new campaign: {@Campaign}", campaign.CampaignName);
            if (campaign.EndDate < campaign.StartDate)
            {
                throw new InvalidOperationException("EndDate cannot be earlier than StartDate.");
            }
            if (campaign.IncentiveValue < 0)
            {
                throw new InvalidOperationException("IncentiveValue cannot be negative.");
            }
            var now = DateTime.UtcNow;
            if (campaign.StartDate >  now)
            {
                campaign.Status = "Planned";
            }
            else if (campaign.EndDate < now)
            {
                campaign.Status = "Completed";
            }
            var campaignId = await _campaignRepository.AddCampaignAsync(campaign);
            _logger.LogInformation($"Campaign added with ID: {campaignId}--{campaign.CampaignName}", campaignId, campaign.CampaignName);
            return campaignId;
        }
        public async Task<bool> UpdateCampaignAsync(Campaign campaign)
        {
            _logger.LogInformation($"Updating campaign with ID: {campaign.CampaignId}");
            if (campaign.EndDate < campaign.StartDate)
            {
                throw new InvalidOperationException("EndDate cannot be earlier than StartDate.");
            }
            if (campaign.IncentiveValue < 0)
            {
                throw new InvalidOperationException("IncentiveValue cannot be negative.");
            }
            var now = DateTime.UtcNow;
            if (campaign.StartDate > now)
            {
                campaign.Status = "Planned";
            }
            else if (campaign.EndDate < now)
            {
                campaign.Status = "Completed";
            }
            var result = await _campaignRepository.UpdateCampaignAsync(campaign);
            if (result)
            {
                _logger.LogInformation($"Campaign with ID: {campaign.CampaignId} updated successfully.");
            }
            else
            {
                _logger.LogWarning($"Failed to update campaign with ID: {campaign.CampaignId}. It may not exist.");
            }
            return await _campaignRepository.UpdateCampaignAsync(campaign);
        }
        public async Task<bool> DeleteCampaignAsync(int campaignId)
        {
            _logger.LogInformation("Deleting campaign with ID: {campaignId}", campaignId);
            var campaign = await _campaignRepository.GetCampaignByIdAsync(campaignId);
            if (campaign == null)
            {
                _logger.LogWarning($"Campaign with ID: {campaignId} not found.");
                return false;
            }
            if (campaign.Status == "Active")
            {
                throw new InvalidOperationException("Cannot delete an active campaign.");
            }
            return await _campaignRepository.DeleteCampaignAsync(campaignId);
        }
        public async Task<IEnumerable<Campaign>> GetActiveCampaignsAsync()
        {
            _logger.LogInformation("Retrieving active campaigns.");
            return await _campaignRepository.GetActiveCampaignsAsync();
        }
        public async Task<Campaign?> GetCurrentCampaignAsync()
        {
            _logger.LogInformation("Retrieving current campaign.");
            return await _campaignRepository.GetCurrentCampaignAsync();
        }
        public async Task<bool> ActivateCampaignAsync(int campaignId)
        {
            _logger.LogInformation("Activating campaign with ID: {campaignId}", campaignId);
            var campaign = await _campaignRepository.GetCampaignByIdAsync(campaignId);
            if (campaign == null)
                return false;
            
            var now = DateTime.UtcNow;
            if (now < campaign.StartDate || now > campaign.EndDate)
            {
                throw new InvalidOperationException("Campaign cannot be activated outside its start and end dates.");
            }
            campaign.Status = "ACTIVE";
            return await _campaignRepository.UpdateCampaignAsync(campaign);
        }
        public async Task<bool> DeactivateCampaignAsync(int campaignId)
        {
            _logger.LogInformation("Deactivating campaign with ID: {campaignId}", campaignId);
            var campaign = await _campaignRepository.GetCampaignByIdAsync(campaignId);
            if (campaign == null)
                return false;
            
            campaign.Status = "INACTIVE";
            return await _campaignRepository.UpdateCampaignAsync(campaign);
        }
        public async Task<decimal> CalculateTotalIncentivesAsync(int basePoints)
        {
            var currentCampaign = await GetCurrentCampaignAsync();
            if (currentCampaign == null)
                return basePoints;
            return currentCampaign.IncentiveType switch
            {
                "Multiplier" => basePoints * currentCampaign.IncentiveValue,
                "Bonus" => basePoints + currentCampaign.IncentiveValue,
                _ => basePoints
            };
        }
    }
}