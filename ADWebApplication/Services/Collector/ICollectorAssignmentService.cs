using ADWebApplication.Models;
using ADWebApplication.ViewModels;

namespace ADWebApplication.Services.Collector
{
    public interface ICollectorAssignmentService
    {
        Task<RouteAssignmentSearchViewModel> GetRouteAssignmentsAsync(string username, string? search, int? regionId, DateTime? date, string? status, int page, int pageSize);
        Task<RouteAssignmentDetailViewModel?> GetRouteAssignmentDetailsAsync(int assignmentId, string username);
        Task<NextStopsViewModel?> GetNextStopsAsync(string username, int top);
    }
}
