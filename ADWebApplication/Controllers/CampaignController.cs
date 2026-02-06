using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ADWebApplication.Services;
using ADWebApplication.Models;

namespace ADWebApplication.Controllers
{
    [Route("Campaigns")]
     [Authorize(Roles = "Admin")]
    public class CampaignController : Controller
    {
        private readonly ICampaignService _campaignService;

        public CampaignController(ICampaignService campaignService)
        {
            _campaignService = campaignService?? throw new ArgumentNullException(nameof(campaignService));
        }
        // Action methods for managing campaigns would go here
        //GET: Campaigns
        [HttpGet("")]
        public async  Task<IActionResult> Index()
        {
            var campaigns = await _campaignService.GetAllCampaignsAsync();
            return View(campaigns);
        }
        //GET: Campaigns/Create
        [HttpGet("Create")]
        public IActionResult Create()
        {
            return View(new Campaign
            {
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(30),
                Status = "INACTIVE",
                IncentiveType = "POINTS_MULTIPLIER",
                IncentiveValue = 1.0M
            });
        }
        //POST: Campaigns/Create
        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Campaign campaign)
        {
        
            if (!ModelState.IsValid)
            {
            return View(campaign);
            }
            try
            {
                await _campaignService.AddCampaignAsync(campaign);
                TempData["SuccessMessage"] = "Campaign created successfully.";
                return RedirectToAction(nameof(Index));       
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, $"Error creating campaign: {ex.Message}");
                return View(campaign);
            }
        }

        //GET: Campaigns/Edit/
        [HttpGet("Edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            var campaign = await _campaignService.GetCampaignByIdAsync(id);
            if (campaign == null)
            {
                TempData["ErrorMessage"] = "Campaign not found.";
                return RedirectToAction("Index");
            }
            return View(campaign);
        }
        //POST: Campaigns/Edit/
        [HttpPost("Edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Campaign campaign)
        {
            if (!ModelState.IsValid)
            {
                return View(campaign);
            }
            try
            {
                await _campaignService.UpdateCampaignAsync(campaign);
                TempData["SuccessMessage"] = "Campaign updated successfully.";
                return RedirectToAction("Index");
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, $"Error updating campaign: {ex.Message}");
                return View(campaign);
            }
        }
        //POST: Campaigns/Delete/
        [HttpPost("Delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _campaignService.DeleteCampaignAsync(id);
                TempData["SuccessMessage"] = "Campaign deleted successfully.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = $"Error deleting campaign: {ex.Message}";
            }
            return RedirectToAction("Index");
        }
    
    //Post: Campaign/Activate
    [HttpPost("Activate/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(int id)
    {
        try
        {
            await _campaignService.ActivateCampaignAsync(id);
            TempData["SuccessMessage"] = "Campaign activated successfully.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = $"Error activating campaign: {ex.Message}";
        }
        return RedirectToAction("Index");
    }
    //Post: Campaign/Deactivate
    [HttpPost("Deactivate/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id)
    {
        try
        {
            await _campaignService.DeactivateCampaignAsync(id);
            TempData["SuccessMessage"] = "Campaign deactivated successfully.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = $"Error deactivating campaign: {ex.Message}";
        }
        return RedirectToAction("Index");
    }
}
}