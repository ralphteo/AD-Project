using ADWebApplication.Models.DTOs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ADWebApplication.Services;

namespace ADWebApplication.Controllers
{
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/rewards")]
    [EnableRateLimiting("mobile")]
    public class RewardsController : ControllerBase
    {
        private readonly IMobileRewardsService _rewardsService;

        public RewardsController(IMobileRewardsService rewardsService)
        {
            _rewardsService = rewardsService;
        }

        // GET: /api/rewards/summary?userId=1
        [HttpGet("summary")]
        public async Task<ActionResult<RewardsSummaryDto>> GetSummary([FromQuery] int userId)
        {
            var tokenUserId = GetUserIdFromToken();
            if (tokenUserId == null)
                return Unauthorized();
            if (tokenUserId != userId)
                return Forbid();

            var summary = await _rewardsService.GetSummaryAsync(userId);
            return Ok(summary);
        }

        // GET: /api/rewards/history?userId=1
        [HttpGet("history")]
        public async Task<ActionResult<List<RewardsHistoryDto>>> GetHistory([FromQuery] int userId)
        {
            if (userId <= 0)
                return BadRequest("Invalid userId.");

            var tokenUserId = GetUserIdFromToken();
            if (tokenUserId == null)
                return Unauthorized();
            if (tokenUserId != userId)
                return Forbid();

            var history = await _rewardsService.GetHistoryAsync(userId);
            return Ok(history);
        }

        // GET: /api/rewards/wallet?userId=1
        [HttpGet("wallet")]
        public async Task<ActionResult<RewardWalletDto>> GetWallet([FromQuery] int userId)
        {
            var tokenUserId = GetUserIdFromToken();
            if (tokenUserId == null)
                return Unauthorized();
            if (tokenUserId != userId)
                return Forbid();

            var wallet = await _rewardsService.GetWalletAsync(userId);
            return Ok(wallet);
        }

        // GET: /api/rewards/catalogue
        [HttpGet("catalogue")]
        public async Task<ActionResult<List<RewardCatalogueDto>>> GetCatalogue()
        {
            var rewards = await _rewardsService.GetCatalogueAsync();
            return Ok(rewards);
        }

        // POST: /api/rewards/redeem
        [HttpPost("redeem")]
        public async Task<ActionResult<RedeemResponseDto>> Redeem([FromBody] RedeemRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (request == null)
            {
                return BadRequest(new RedeemResponseDto { Success = false, Message = "Invalid request" });
            }

            var tokenUserId = GetUserIdFromToken();
            if (tokenUserId == null)
                return Unauthorized(new RedeemResponseDto { Success = false, Message = "Unauthorized" });
            if (tokenUserId != request.UserId)
                return Forbid();

            var result = await _rewardsService.RedeemAsync(tokenUserId.Value, request);
            return Ok(result);
        }

        // GET: /api/rewards/redemptions?userId=1
        [HttpGet("redemptions")]
        public async Task<ActionResult<List<RewardRedemptionItemDto>>> GetRedemptions([FromQuery] int userId)
        {
            var tokenUserId = GetUserIdFromToken();
            if (tokenUserId == null)
                return Unauthorized();
            if (tokenUserId != userId)
                return Forbid();

            var redemptions = await _rewardsService.GetRedemptionsAsync(userId);
            return Ok(redemptions);
        }

        // POST: /api/rewards/redemptions/use
        [HttpPost("redemptions/use")]
        public async Task<ActionResult<UseRedemptionResponseDto>> UseRedemption(
            [FromBody] UseRedemptionRequestDto request)
        {
            var tokenUserId = GetUserIdFromToken();
            if (tokenUserId == null)
                return Unauthorized(new UseRedemptionResponseDto { Success = false, Message = "Unauthorized" });

            var result = await _rewardsService.UseRedemptionAsync(tokenUserId.Value, request);
            if (result.Message == "Forbidden")
                return Forbid();
            if (result.Message == "Invalid request" || result.Message == "Vendor code required")
                return BadRequest(result);
            return Ok(result);
        }
        
        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok(new { Message = "Test endpoint working" });
        }

        private int? GetUserIdFromToken()
        {
            var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claimValue, out var userId) ? userId : null;
        }

    }
}
