using ADWebApplication.Models;
using ADWebApplication.Models.DTOs;

namespace ADWebApplication.Services;

public interface IMobileAuthService
{
    Task<List<Region>> GetRegionsAsync();
    Task<MobileAuthResult<RegisterResponse>> RegisterAsync(RegisterRequest request);
    Task<MobileAuthResult<LoginResponse>> LoginAsync(LoginRequest request);
    Task<MobileAuthResult<UserProfileDto>> GetProfileAsync(int tokenUserId, int? targetUserId);
    Task<MobileAuthResult<UserProfileDto>> UpdateProfileAsync(int tokenUserId, UpdateProfileRequestDto request);
}
