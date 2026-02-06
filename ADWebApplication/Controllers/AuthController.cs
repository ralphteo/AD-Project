using System.Reflection.Metadata;
using ADWebApplication.Data;
using ADWebApplication.Models;
using ADWebApplication.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ADWebApplication.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly In5niteDbContext _db;

    public AuthController(In5niteDbContext db)
    {
        _db = db;
    }

    [HttpGet("regions")]
    public async Task<ActionResult<List<Region>>> GetRegions()
    {
        var regions = await _db.Regions
            .AsNoTracking()
            .OrderBy(r => r.RegionName)
            .ToListAsync();

        return Ok(regions);
    }
    [HttpPost("register")]
    public async Task<ActionResult<RegisterResponse>> Register([FromBody] RegisterRequest request)
    {
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

        return Ok(new RegisterResponse
        {
            Success = true,
            Message = "Registered",
            UserId = user.Id
        });
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
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

        return Ok(new LoginResponse
        {
            Success = true,
            Message = "Logged in",
            UserId = user.Id,
        });
    }

    // GET: /api/auth/profile?userId=1
    [HttpGet("profile")]
    public async Task<ActionResult<UserProfileDto>> GetProfile([FromQuery] int userId)
    {
        var user = await _db.PublicUser
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            return NotFound();
        }

        return Ok(new UserProfileDto
        {
            UserId = user.Id,
            UserName = user.Name,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber
        });
    }
}
