using ADWebApplication.Models.ViewModels.BinPredictions;
using ADWebApplication.Models.DTOs;

namespace ADWebApplication.Services
{
    public interface IRouteAssignmentService
    {
        Task SavePlannedRouteAsync(List<RoutePlanDto> allStops, int routeKey, string officerUsername, string adminUsername, DateTime plannedDate);
    }
}