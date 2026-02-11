using System.Reflection.Metadata;
using ADWebApplication.Data;
using ADWebApplication.Models;
using ADWebApplication.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ADWebApplication.Services;

namespace ADWebApplication.Controllers;

[ApiController]
[Route("api/auth")]
[EnableRateLimiting("mobile")]
public class AuthController : ControllerBase
{
    private readonly In5niteDbContext _db;
    private readonly JwtTokenService _jwtTokenService;

    public AuthController(In5niteDbContext db, JwtTokenService jwtTokenService)
    {
        _db = db;
        _jwtTokenService = jwtTokenService;
    }

    [AllowAnonymous]
    [HttpGet("regions")]
    public async Task<ActionResult<List<Region>>> GetRegions()
    {
        var regions = await _db.Regions
            .AsNoTracking()
            .OrderBy(r => r.RegionName)
            .ToListAsync();

        return Ok(regions);
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<RegisterResponse>> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var email = request.Email.Trim().ToLowerInvariant();
        var emailExists = await _db.PublicUser.AnyAsync(u => u.Email == email);
        if (emailExists)
        {
            return Conflict(new RegisterResponse
            {
                Success = false,
                Message = "Email already registered"
            });
        }

        var user = new PublicUser
        {
            Email = email,
            Name = request.FullName.Trim(),
            PhoneNumber = request.Phone.Trim(),
            RegionId = request.RegionId,
            IsActive = true,
            Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
            RewardWallet = new RewardWallet
            {
                AvailablePoints = 0
            }
        };

        _db.PublicUser.Add(user);
        await _db.SaveChangesAsync();

        var token = _jwtTokenService.CreateToken(user);

        return Ok(new RegisterResponse
        {
            Success = true,
            Message = "Registered",
            UserId = user.Id,
            UserName = user.Name,
            Token = token
        });
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.PublicUser.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
        {
            return Unauthorized(new LoginResponse
            {
                Success = false,
                Message = "Invalid email or password"
            });
        }

        if (!user.IsActive)
        {
            return Unauthorized(new LoginResponse
            {
                Success = false,
                Message = "Account inactive"
            });
        }

        var validPassword = BCrypt.Net.BCrypt.Verify(request.Password, user.Password);
        if (!validPassword)
        {
            return Unauthorized(new LoginResponse
            {
                Success = false,
                Message = "Invalid email or password"
            });
        }

        var token = _jwtTokenService.CreateToken(user);

        return Ok(new LoginResponse
        {
            Success = true,
            Message = "Logged in",
            UserId = user.Id,
            UserName = user.Name,
            Token = token
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

        var targetUserId = userId ?? tokenUserId.Value;
        if (targetUserId != tokenUserId.Value)
        {
            return Forbid();
        }

        var user = await _db.PublicUser
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == targetUserId);

        if (user == null)
        {
            return NotFound();
        }

        string? regionName = null;
        if (user.RegionId.HasValue)
        {
            regionName = await _db.Regions
                .AsNoTracking()
                .Where(r => r.RegionId == user.RegionId.Value)
                .Select(r => r.RegionName)
                .FirstOrDefaultAsync();
        }

        return Ok(new UserProfileDto
        {
            UserId = user.Id,
            UserName = user.Name,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            RegionId = user.RegionId,
            RegionName = regionName
        });
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

        var user = await _db.PublicUser.FirstOrDefaultAsync(u => u.Id == tokenUserId.Value);
        if (user == null)
        {
            return NotFound();
        }

        if (request != null)
        {
            user.PhoneNumber = request.PhoneNumber;
            if (request.RegionId.HasValue)
            {
                var exists = await _db.Regions
                    .AsNoTracking()
                    .AnyAsync(r => r.RegionId == request.RegionId.Value);
                if (!exists)
                {
                    return BadRequest("Invalid region");
                }
                user.RegionId = request.RegionId.Value;
            }
            else
            {
                user.RegionId = null;
            }
        }

        await _db.SaveChangesAsync();

        string? regionName = null;
        if (user.RegionId.HasValue)
        {
            regionName = await _db.Regions
                .AsNoTracking()
                .Where(r => r.RegionId == user.RegionId.Value)
                .Select(r => r.RegionName)
                .FirstOrDefaultAsync();
        }

        return Ok(new UserProfileDto
        {
            UserId = user.Id,
            UserName = user.Name,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            RegionId = user.RegionId,
            RegionName = regionName
        });
    }

    private int? GetUserIdFromToken()
    {
        var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claimValue, out var userId) ? userId : null;
    }
}
