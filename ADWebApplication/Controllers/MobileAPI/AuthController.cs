using ADWebApplication.Models;
using ADWebApplication.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ADWebApplication.Services;

namespace ADWebApplication.Controllers;

[ApiController]
[Route("api/auth")]
[EnableRateLimiting("mobile")]
public class AuthController : ControllerBase
{
    private readonly IMobileAuthService _mobileAuthService;

    public AuthController(IMobileAuthService mobileAuthService)
    {
        _mobileAuthService = mobileAuthService;
    }

    [AllowAnonymous]
    [HttpGet("regions")]
    public async Task<ActionResult<List<Region>>> GetRegions()
    {
        var regions = await _mobileAuthService.GetRegionsAsync();
        return Ok(regions);
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<RegisterResponse>> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _mobileAuthService.RegisterAsync(request);
        if (result.Success && result.Data != null)
        {
            return Ok(result.Data);
        }

        if (result.Error == MobileAuthError.EmailAlreadyRegistered)
        {
            return Conflict(new RegisterResponse
            {
                Success = false,
                Message = result.Message ?? "Email already registered"
            });
        }

        return BadRequest(new RegisterResponse
        {
            Success = false,
            Message = result.Message ?? "Registration failed"
        });
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _mobileAuthService.LoginAsync(request);
        if (result.Success && result.Data != null)
        {
            return Ok(result.Data);
        }

        var message = result.Message ?? "Invalid email or password";
        if (result.Error == MobileAuthError.AccountInactive)
        {
            message = result.Message ?? "Account inactive";
        }

        return Unauthorized(new LoginResponse
        {
            Success = false,
            Message = message
        });
    }

    // GET: /api/auth/profile?userId=1
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpGet("profile")]
    public async Task<ActionResult<UserProfileDto>> GetProfile([FromQuery] int? userId)
    {
        var tokenUserId = GetUserIdFromToken();
        if (tokenUserId == null)
        {
            return Unauthorized();
        }

        var result = await _mobileAuthService.GetProfileAsync(tokenUserId.Value, userId);
        if (result.Success && result.Data != null)
        {
            return Ok(result.Data);
        }

        if (result.Error == MobileAuthError.Forbidden)
        {
            return Forbid();
        }

        return NotFound();
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpPut("profile")]
    public async Task<ActionResult<UserProfileDto>> UpdateProfile([FromBody] UpdateProfileRequestDto request)
    {
        var tokenUserId = GetUserIdFromToken();
        if (tokenUserId == null)
        {
            return Unauthorized();
        }

        var result = await _mobileAuthService.UpdateProfileAsync(tokenUserId.Value, request);
        if (result.Success && result.Data != null)
        {
            return Ok(result.Data);
        }

        if (result.Error == MobileAuthError.InvalidRegion)
        {
            return BadRequest("Invalid region");
        }

        return NotFound();
    }

    private int? GetUserIdFromToken()
    {
        var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claimValue, out var userId) ? userId : null;
    }
}
