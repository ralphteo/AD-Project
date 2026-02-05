using ADWebApplication.Models;
using ADWebApplication.ViewModels;

namespace ADWebApplication.Services.Collector
{
    public class CollectorService : ICollectorService
    {
        private readonly ICollectorDashboardService _dashboardService;
        private readonly ICollectorAssignmentService _assignmentService;
        private readonly ICollectorIssueService _issueService;

        public CollectorService(
            ICollectorDashboardService dashboardService,
            ICollectorAssignmentService assignmentService,
            ICollectorIssueService issueService)
        {
            _dashboardService = dashboardService;
            _assignmentService = assignmentService;
            _issueService = issueService;
        }

        // ICollectorDashboardService
        public Task<CollectorRoute> GetDailyRouteAsync(string username) => _dashboardService.GetDailyRouteAsync(username);
        public Task<CollectionConfirmationVM?> GetCollectionConfirmationAsync(int stopId, string username) => _dashboardService.GetCollectionConfirmationAsync(stopId, username);
        public Task<bool> ConfirmCollectionAsync(CollectionConfirmationVM model, string username) => _dashboardService.ConfirmCollectionAsync(model, username);

        // ICollectorAssignmentService
        public Task<RouteAssignmentSearchViewModel> GetRouteAssignmentsAsync(string username, string? search, int? regionId, DateTime? date, string? status, int page, int pageSize) 
            => _assignmentService.GetRouteAssignmentsAsync(username, search, regionId, date, status, page, pageSize);
        public Task<RouteAssignmentDetailViewModel?> GetRouteAssignmentDetailsAsync(int assignmentId, string username) 
            => _assignmentService.GetRouteAssignmentDetailsAsync(assignmentId, username);
        public Task<NextStopsViewModel?> GetNextStopsAsync(string username, int top) 
            => _assignmentService.GetNextStopsAsync(username, top);

        // ICollectorIssueService
        public Task<ReportIssueVM> GetReportIssueViewModelAsync(string username, string? search, string? status, string? priority) 
            => _issueService.GetReportIssueViewModelAsync(username, search, status, priority);
        public Task<bool> SubmitIssueAsync(ReportIssueVM model, string username) 
            => _issueService.SubmitIssueAsync(model, username);
        public Task<string> StartIssueWorkAsync(int stopId, string username) 
            => _issueService.StartIssueWorkAsync(stopId, username);
    }
}
