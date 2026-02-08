using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ADWebApplication.Models;
using ADWebApplication.Data.Repository;
using Microsoft.Extensions.Logging;

namespace ADWebApplication.Services
{
    public class CampaignService : ICampaignService
    {
        private readonly ICampaignRepository _campaignRepository;
        private readonly ILogger<CampaignService> _logger;

        public CampaignService(ICampaignRepository campaignRepository, ILogger<CampaignService> logger)
        {
            _campaignRepository = campaignRepository ?? throw new ArgumentNullException(nameof(campaignRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<Campaign>> GetAllCampaignsAsync()
        {
            if(_logger.IsEnabled(LogLevel.Information))
            {
            _logger.LogInformation("Retrieving all campaigns.");
            }
            return await _campaignRepository.GetAllCampaignsAsync();
        }

        public async Task<Campaign?> GetCampaignByIdAsync(int campaignId)
        {
            if(_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Retrieving campaign with ID: {campaignId}", campaignId);
            }
            
            return await _campaignRepository.GetCampaignByIdAsync(campaignId);
        }

        public async Task<int> AddCampaignAsync(Campaign campaign)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Adding a new campaign: {CampaignName}", campaign.CampaignName);
            }
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
            var campaignId = await _campaignRepository.AddCampaignAsync(campaign);
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Campaign added with ID: {campaignId}", campaignId);
            }
            return campaignId;
        }
        public async Task<bool> UpdateCampaignAsync(Campaign campaign)
        {
            if(_logger.IsEnabled(LogLevel.Information))
            {
            _logger.LogInformation("Updating campaign with ID: {campaignId}", campaign.CampaignId);
            }
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
                if( _logger.IsEnabled(LogLevel.Information))
                {
                _logger.LogInformation("Campaign with ID: {campaignId} updated successfully.", campaign.CampaignId);
                }
            }
            else
            {
                if( _logger.IsEnabled(LogLevel.Warning))
                {
                _logger.LogWarning("Failed to update campaign with ID: {campaignId}. It may not exist.", campaign.CampaignId);
                }
            }
            return result;
        }
        public async Task<bool> DeleteCampaignAsync(int campaignId)
        {
            if(_logger.IsEnabled(LogLevel.Information))
            {
            _logger.LogInformation("Deleting campaign with ID: {campaignId}", campaignId);
            }

            var campaign = await _campaignRepository.GetCampaignByIdAsync(campaignId);
            if (campaign == null)
            {
                if (_logger.IsEnabled(LogLevel.Warning))
                {
                    _logger.LogWarning("Campaign with ID: {campaignId} not found.", campaignId);
                }
                return false;
            }
            if (campaign.Status == "Active")
            {
                throw new InvalidOperationException("Cannot delete an active campaign.");
            }
            var result = await _campaignRepository.DeleteCampaignAsync(campaignId);
    
            if (result)
            {
                if(_logger.IsEnabled(LogLevel.Information))
                {
                _logger.LogInformation("Campaign with ID: {CampaignId} deleted successfully", campaignId);
                }
            }
            else
            {
                if(_logger.IsEnabled(LogLevel.Warning))
                {
                _logger.LogWarning("Failed to delete campaign with ID: {CampaignId}", campaignId);
                }
            }
            
            return result;
        }
        public async Task<IEnumerable<Campaign>> GetActiveCampaignsAsync()
        {
            if(_logger.IsEnabled(LogLevel.Information))
            {
            _logger.LogInformation("Retrieving active campaigns.");
            }
            return await _campaignRepository.GetActiveCampaignsAsync();
        }
        public async Task<Campaign?> GetCurrentCampaignAsync()
        {
            if(_logger.IsEnabled(LogLevel.Information))
            {
            _logger.LogInformation("Retrieving current campaign.");
            }
            return await _campaignRepository.GetCurrentCampaignAsync();
        }
        public async Task<bool> ActivateCampaignAsync(int campaignId)
        {
            if(_logger.IsEnabled(LogLevel.Information))
            {   
            _logger.LogInformation("Activating campaign with ID: {CampaignId}", campaignId);
            }
            var campaign = await _campaignRepository.GetCampaignByIdAsync(campaignId);
            if (campaign == null)
            {
                if(_logger.IsEnabled(LogLevel.Warning))
                {
                _logger.LogWarning("Campaign with ID: {CampaignId} not found.", campaignId);
                }
                return false;
            }
            var now = DateTime.UtcNow;
            if (now < campaign.StartDate || now > campaign.EndDate)
            {
                throw new InvalidOperationException("Campaign cannot be activated outside its start and end dates.");
            }
            campaign.Status = "ACTIVE";
            var result = await _campaignRepository.UpdateCampaignAsync(campaign);
    
            if (result)
            {
                if(_logger.IsEnabled(LogLevel.Information))
                {
                _logger.LogInformation("Campaign with ID: {CampaignId} activated successfully", campaignId);
                }
            }
            else
            {
                if(_logger.IsEnabled(LogLevel.Warning))
                {   
                _logger.LogWarning("Failed to activate campaign with ID: {CampaignId}", campaignId);
                }
            }
            
            return result;
        }
        public async Task<bool> DeactivateCampaignAsync(int campaignId)
        {
            if(_logger.IsEnabled(LogLevel.Information))
            {   
            _logger.LogInformation("Deactivating campaign with ID: {CampaignId}", campaignId);
            }
            var campaign = await _campaignRepository.GetCampaignByIdAsync(campaignId);
            if (campaign == null)
                return false;
            
            campaign.Status = "INACTIVE";
            var result = await _campaignRepository.UpdateCampaignAsync(campaign);
    
            if (result)
            {
                if(_logger.IsEnabled(LogLevel.Information))
                {
                _logger.LogInformation("Campaign with ID: {CampaignId} deactivated successfully", campaignId);
                }
            }
            else
            {
                if(_logger.IsEnabled(LogLevel.Warning))
                {
                _logger.LogWarning("Failed to deactivate campaign with ID: {CampaignId}", campaignId);
                }
            }
            
            return result;
        }
        public async Task<decimal> CalculateTotalIncentivesAsync(int basePoints, int? campaignId = null)
        {
            Campaign? currentCampaign = null;
            if (campaignId.HasValue)
            {
                currentCampaign = await GetCampaignByIdAsync(campaignId.Value);
            }
            else
            {
                currentCampaign = await GetCurrentCampaignAsync();
            }
            if (currentCampaign == null)
                return (decimal)basePoints;
            return currentCampaign.IncentiveType switch
            {
                "Multiplier" => basePoints * currentCampaign.IncentiveValue,
                "Bonus" => basePoints + currentCampaign.IncentiveValue,
                _ => (decimal)basePoints
            };
        }
    }
}