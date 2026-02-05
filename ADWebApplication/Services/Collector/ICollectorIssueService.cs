using ADWebApplication.Models;
using ADWebApplication.ViewModels;

namespace ADWebApplication.Services.Collector
{
    public interface ICollectorIssueService
    {
        Task<ReportIssueVM> GetReportIssueViewModelAsync(string username, string? search, string? status, string? priority);
        Task<bool> SubmitIssueAsync(ReportIssueVM model, string username);
        Task<string> StartIssueWorkAsync(int stopId, string username);
    }
}
