using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ADWebApplication.Models;
using ADWebApplication.Data.Repository;
using ADWebApplication.Services;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace ADWebApplication.Controllers
{
    [Route("RewardCatalogue")]
    [Authorize(Roles = "Admin")]
    public class RewardCatalogueController : Controller
    {
        private readonly IRewardCatalogueService _rewardCatalogueService;
        private readonly ILogger<RewardCatalogueController> _logger;

        public RewardCatalogueController(IRewardCatalogueService rewardCatalogueService, ILogger<RewardCatalogueController> logger)
        {
            _rewardCatalogueService = rewardCatalogueService ?? throw new ArgumentNullException(nameof(rewardCatalogueService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // Action methods for managing rewards would go here
        //GET: RewardCatalogue
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            try
            {
                var rewards = await _rewardCatalogueService.GetAllRewardsAsync();
                return View(rewards);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accessing Reward Catalogue index page.");
                TempData["ErrorMessage"] = "An error occurred while loading the reward catalogue.";
                return View(new List<RewardCatalogue>());
            }
            
        }
        //GET: Create
        [HttpGet("Create")]
        public IActionResult Create()
        {
            var reward = new RewardCatalogue
            {
                Availability = true,
                StockQuantity = 0,
                Points = 50,
                RewardName = string.Empty,
                RewardCategory = string.Empty
            };
            return View(reward);
        }
        //POST: Create
        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RewardCatalogue reward)
        {
            try
            {
                _logger.LogInformation("Attempting to create a new reward: {RewardName}", reward.RewardName);
                if (!ModelState.IsValid)
                {
                    foreach (var modelState in ModelState.Values)
                    {
                        foreach (var error in modelState.Errors)
                        {
                            _logger.LogWarning("Model validation error: {ErrorMessage}", error.ErrorMessage);
                        }
                    }
                    return View(reward);
                }
                await _rewardCatalogueService.AddRewardAsync(reward);
                TempData["SuccessMessage"] = "Reward created successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Error creating reward: {RewardName}", reward.RewardName);
                ModelState.AddModelError(string.Empty, $"Error creating reward: {ex.Message}");
                return View(reward);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating reward: {RewardName}", reward.RewardName);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while creating the reward.");
                return View(reward);
            }
        }
        //Post: Edit
        [HttpGet("Edit/{id:int}")]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var reward = await _rewardCatalogueService.GetRewardByIdAsync(id);
                
                if (reward == null)
                {
                    TempData["Error"] = "Reward not found.";
                    return RedirectToAction(nameof(Index));
                }
                
                return View(reward);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading reward {RewardId}", id);
                TempData["Error"] = "Error loading reward.";
                return RedirectToAction(nameof(Index));
            }
        }
        [HttpPost("Edit/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, RewardCatalogue reward)
        {
            if (id != reward.RewardId)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return View(reward);
            }

            try
            {
                var success = await _rewardCatalogueService.UpdateRewardAsync(reward);

                if (!success)
                {
                    TempData["Error"] = "Failed to update reward.";
                    return View(reward);
                }

                TempData["Success"] = "Reward updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating reward {RewardId}", reward.RewardId);
                TempData["Error"] = "Error updating reward.";
                return View(reward);
            }
        }
        //Post: Delete
        [HttpPost("Delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
               await _rewardCatalogueService.DeleteRewardAsync(id);
                TempData["SuccessMessage"] = "Reward deleted successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting reward {RewardId}", id);
                TempData["ErrorMessage"] = "An error occurred while deleting the reward.";  
            }
            return RedirectToAction(nameof(Index));
        }
        [HttpGet("TestConnection")]
        public async Task<IActionResult> TestConnection()
        {
            try
                {
                    var rewards = await _rewardCatalogueService.GetAllRewardsAsync();
        
                    return Json(new
                    {
                        Success = true,
                        Count = rewards.Count(),
                        Data = rewards
                    });
                }
                catch (Exception ex)
                {
                    return Json(new
                    {
                        Success = false,
                        Error = ex.Message
                    });
                }
        }
    }
}  