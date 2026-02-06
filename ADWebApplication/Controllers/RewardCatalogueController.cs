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
        private const string SuccessMessageKey = "SuccessMessage";
        private const string ErrorMessageKey = "ErrorMessage";

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
                TempData[ErrorMessageKey] = "An error occurred while loading the reward catalogue.";
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
                if(_logger.IsEnabled(LogLevel.Information))
                {
                _logger.LogInformation("Attempting to create a new reward: {RewardName}", reward.RewardName);
                }
                if (!ModelState.IsValid)
                {
                    if(_logger.IsEnabled(LogLevel.Warning))
                    {
                        foreach (var modelState in ModelState.Values)
                        {
                            foreach (var error in modelState.Errors)
                            {
                            _logger.LogWarning("Model validation error: {ErrorMessage}", error.ErrorMessage);
                            }
                        }
                    }
                    return View(reward);
                }
                await _rewardCatalogueService.AddRewardAsync(reward);
                TempData[SuccessMessageKey] = "Reward created successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Error creating reward: {RewardName}", reward.RewardName);
                ModelState.AddModelError(string.Empty, "Unable to create reward. Please check your input and try again.");
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
            if (!ModelState.IsValid)
            {
                TempData[ErrorMessageKey] = "Invalid request.";
                return RedirectToAction(nameof(Index));
            }

            if (id <= 0)
            {
                TempData[ErrorMessageKey] = "Invalid reward ID.";
                return RedirectToAction(nameof(Index));
            }
            try
            {
                var reward = await _rewardCatalogueService.GetRewardByIdAsync(id);
                
                if (reward == null)
                {
                    TempData[ErrorMessageKey] = "Reward not found.";
                    return RedirectToAction(nameof(Index));
                }
                
                return View(reward);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading reward {RewardId}", id);
                TempData[ErrorMessageKey] = "Error loading reward.";
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
                    TempData[ErrorMessageKey] = "Failed to update reward.";
                    return View(reward);
                }

                TempData[SuccessMessageKey] = "Reward updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating reward {RewardId}", reward.RewardId);
                TempData[ErrorMessageKey] = "Error updating reward.";
                return View(reward);
            }
        }
        //Post: Delete
        [HttpPost("Delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if(!ModelState.IsValid)
            {
                TempData[ErrorMessageKey] = "Invalid request.";
                return RedirectToAction(nameof(Index));
            }
            if (id <= 0)
            {
                TempData[ErrorMessageKey] = "Invalid reward ID.";
                return RedirectToAction(nameof(Index));
            }
            try
            {
               await _rewardCatalogueService.DeleteRewardAsync(id);
                TempData[SuccessMessageKey] = "Reward deleted successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting reward {RewardId}", id);
                TempData[ErrorMessageKey] = "An error occurred while deleting the reward.";  
            }
            return RedirectToAction(nameof(Index));
        }
    }
}  