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
    [Route("api/disposallogs")]
    [EnableRateLimiting("mobile")]
    public class DisposalLogsController : ControllerBase
    {
        private readonly IDisposalLogsService _disposalLogsService;

        public DisposalLogsController(IDisposalLogsService disposalLogsService)
        {
            _disposalLogsService = disposalLogsService;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateDisposalLogRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var tokenUserId = GetUserIdFromToken();
            if (tokenUserId == null)
                return Unauthorized();
            if (tokenUserId != request.UserId)
                return Forbid();

            try
            {
                var result = await _disposalLogsService.CreateAsync(request);
                return Ok(new { logId = result.LogId, earnedPoints = result.EarnedPoints });
            }
            catch
            {
                return StatusCode(500, "Failed to create disposal log");
            }
        }
        [HttpGet("history")]
        public async Task<ActionResult<List<DisposalHistoryDto>>> GetHistory(
            [FromQuery] int userId,
            [FromQuery] string range = "all"
        )
        {
            var tokenUserId = GetUserIdFromToken();
            if (tokenUserId == null)
                return Unauthorized();
            if (tokenUserId != userId)
                return Forbid();

            var result = await _disposalLogsService.GetHistoryAsync(userId, range);
            return Ok(result);
        }

        private int? GetUserIdFromToken()
        {
            var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claimValue, out var userId) ? userId : null;
        }
    }
}
