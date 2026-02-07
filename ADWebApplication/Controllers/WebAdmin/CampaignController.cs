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
        private const string SuccessMessageKey = "SuccessMessage";
        private const string ErrorMessageKey = "ErrorMessage";
        private const string IndexAction = "Index";
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
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var campaigns = await _campaignService.GetAllCampaignsAsync();
            return View(campaigns);
        }
        //GET: Campaigns/Create
        [HttpGet("Create")]
        public IActionResult Create()
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

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
                TempData[SuccessMessageKey] = "Campaign created successfully.";
                return RedirectToAction(nameof(Index));       
            }
            catch (InvalidOperationException)
            {
                TempData[ErrorMessageKey] = "Error creating campaign";
                ModelState.AddModelError(string.Empty, "Unable to create campaign.");
                return View(campaign);
            }
        }

        //GET: Campaigns/Edit/
        [HttpGet("Edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            //Validate Id parameter
            if (id <= 0)
            {
                TempData[ErrorMessageKey] = "Invalid campaign ID.";
                return RedirectToAction(IndexAction);
            }
            var campaign = await _campaignService.GetCampaignByIdAsync(id);
            if (campaign == null)
            {
                TempData[ErrorMessageKey] = "Campaign not found.";
                return RedirectToAction(IndexAction);
            }
            return View(campaign);
        }
        //POST: Campaigns/Edit/
        [HttpPost("Edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Campaign campaign)
        {
            if (!ModelState.IsValid)
            {
                return View(campaign);
            }
            try
            {
                await _campaignService.UpdateCampaignAsync(campaign);
                TempData[SuccessMessageKey] = "Campaign updated successfully.";
                return RedirectToAction(IndexAction);
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
            if (!ModelState.IsValid)
            {
                return RedirectToAction(IndexAction);
            }
            if (id <= 0)
            {
                TempData[ErrorMessageKey] = "Invalid campaign ID.";
                return RedirectToAction(IndexAction);
            }
            try
            {
                await _campaignService.DeleteCampaignAsync(id);
                TempData[SuccessMessageKey] = "Campaign deleted successfully.";
            }
            catch (InvalidOperationException ex)
            {
                TempData[ErrorMessageKey] = $"Error deleting campaign: {ex.Message}";
            }
            return RedirectToAction(IndexAction);
        }
    
    //Post: Campaign/Activate
    [HttpPost("Activate/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(int id)
    {
        if (!ModelState.IsValid)
            {
                return RedirectToAction(IndexAction);
            }
            if (id <= 0)
            {
                TempData[ErrorMessageKey] = "Invalid campaign ID.";
                return RedirectToAction(IndexAction);
            }
        try
        {
            await _campaignService.ActivateCampaignAsync(id);
            TempData[SuccessMessageKey] = "Campaign activated successfully.";
        }
        catch (InvalidOperationException ex)
        {
            TempData[ErrorMessageKey] = $"Error activating campaign: {ex.Message}";
        }
        return RedirectToAction(IndexAction);
    }
    //Post: Campaign/Deactivate
    [HttpPost("Deactivate/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id)
    {
        if (!ModelState.IsValid)
            {
                return RedirectToAction(IndexAction);
            }
            if (id <= 0)
            {
                TempData[ErrorMessageKey] = "Invalid campaign ID.";
                return RedirectToAction(IndexAction);
            }
        try
        {
            await _campaignService.DeactivateCampaignAsync(id);
            TempData[SuccessMessageKey] = "Campaign deactivated successfully.";
        }
        catch (InvalidOperationException ex)
        {
            TempData[ErrorMessageKey] = $"Error deactivating campaign: {ex.Message}";
        }
        return RedirectToAction(IndexAction);
    }
}
}
