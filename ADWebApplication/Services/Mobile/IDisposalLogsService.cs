using ADWebApplication.Models.DTOs;

namespace ADWebApplication.Services;

public interface IDisposalLogsService
{
    Task<(int LogId, int EarnedPoints)> CreateAsync(CreateDisposalLogRequest request);
    Task<List<DisposalHistoryDto>> GetHistoryAsync(int userId, string range);
}
