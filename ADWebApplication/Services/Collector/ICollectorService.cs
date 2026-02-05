using ADWebApplication.Models;
using ADWebApplication.ViewModels;

namespace ADWebApplication.Services.Collector
{
    public interface ICollectorService : ICollectorDashboardService, ICollectorAssignmentService, ICollectorIssueService
    {
    }
}
