using ADWebApplication.Models.ViewModels.BinPredictions;
using ADWebApplication.Models.DTOs;

namespace ADWebApplication.Services
{
    public interface IRouteAssignmentService
    {
        Task SavePlannedRoutesAsync(List<UiRouteStopDto> allStops, Dictionary<int, string> routeAssignments, string adminUsername, DateTime date);    }
}