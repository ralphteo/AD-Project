using ADWebApplication.Data;
using ADWebApplication.Models;
using ADWebApplication.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace ADWebApplication.Services;

public class MobileAuthService : IMobileAuthService
{
    private readonly In5niteDbContext _db;
    private readonly JwtTokenService _jwtTokenService;

    public MobileAuthService(In5niteDbContext db, JwtTokenService jwtTokenService)
    {
        _db = db;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<List<Region>> GetRegionsAsync()
    {
        return await _db.Regions
            .AsNoTracking()
            .OrderBy(r => r.RegionName)
            .ToListAsync();
    }

    public async Task<MobileAuthResult<RegisterResponse>> RegisterAsync(RegisterRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var emailExists = await _db.PublicUser.AnyAsync(u => u.Email == email);
        if (emailExists)
        {
            return MobileAuthResult<RegisterResponse>.Fail(
                MobileAuthError.EmailAlreadyRegistered,
                "Email already registered"
            );
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

        return MobileAuthResult<RegisterResponse>.Ok(new RegisterResponse
        {
            Success = true,
            Message = "Registered",
            UserId = user.Id,
            UserName = user.Name,
            Token = token
        });
    }

    public async Task<MobileAuthResult<LoginResponse>> LoginAsync(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.PublicUser.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
        {
            return MobileAuthResult<LoginResponse>.Fail(
                MobileAuthError.InvalidCredentials,
                "Invalid email or password"
            );
        }

        if (!user.IsActive)
        {
            return MobileAuthResult<LoginResponse>.Fail(
                MobileAuthError.AccountInactive,
                "Account inactive"
            );
        }

        var validPassword = BCrypt.Net.BCrypt.Verify(request.Password, user.Password);
        if (!validPassword)
        {
            return MobileAuthResult<LoginResponse>.Fail(
                MobileAuthError.InvalidCredentials,
                "Invalid email or password"
            );
        }

        var token = _jwtTokenService.CreateToken(user);

        return MobileAuthResult<LoginResponse>.Ok(new LoginResponse
        {
            Success = true,
            Message = "Logged in",
            UserId = user.Id,
            UserName = user.Name,
            Token = token
        });
    }

    public async Task<MobileAuthResult<UserProfileDto>> GetProfileAsync(int tokenUserId, int? targetUserId)
    {
        var resolvedUserId = targetUserId ?? tokenUserId;
        if (resolvedUserId != tokenUserId)
        {
            return MobileAuthResult<UserProfileDto>.Fail(MobileAuthError.Forbidden);
        }

        var user = await _db.PublicUser
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == resolvedUserId);

        if (user == null)
        {
            return MobileAuthResult<UserProfileDto>.Fail(MobileAuthError.NotFound);
        }

        var regionName = await GetRegionNameAsync(user.RegionId);

        return MobileAuthResult<UserProfileDto>.Ok(new UserProfileDto
        {
            UserId = user.Id,
            UserName = user.Name,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            RegionId = user.RegionId,
            RegionName = regionName
        });
    }

    public async Task<MobileAuthResult<UserProfileDto>> UpdateProfileAsync(
        int tokenUserId,
        UpdateProfileRequestDto request)
    {
        var user = await _db.PublicUser.FirstOrDefaultAsync(u => u.Id == tokenUserId);
        if (user == null)
        {
            return MobileAuthResult<UserProfileDto>.Fail(MobileAuthError.NotFound);
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
                    return MobileAuthResult<UserProfileDto>.Fail(
                        MobileAuthError.InvalidRegion,
                        "Invalid region"
                    );
                }
                user.RegionId = request.RegionId.Value;
            }
            else
            {
                user.RegionId = null;
            }
        }

        await _db.SaveChangesAsync();

        var regionName = await GetRegionNameAsync(user.RegionId);

        return MobileAuthResult<UserProfileDto>.Ok(new UserProfileDto
        {
            UserId = user.Id,
            UserName = user.Name,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            RegionId = user.RegionId,
            RegionName = regionName
        });
    }

    private async Task<string?> GetRegionNameAsync(int? regionId)
    {
        if (!regionId.HasValue)
        {
            return null;
        }

        return await _db.Regions
            .AsNoTracking()
            .Where(r => r.RegionId == regionId.Value)
            .Select(r => r.RegionName)
            .FirstOrDefaultAsync();
    }
}
