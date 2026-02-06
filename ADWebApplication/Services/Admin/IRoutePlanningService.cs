using ADWebApplication.Models.ViewModels.RoutePlanning;

namespace ADWebApplication.Services
{
    public interface IRoutePlanningService
    {
        Task<List<RoutePlanDto>> PlanRouteAsync();
        Task<List<SavedRouteStopDto>> GetPlannedRoutesAsync(DateTime date);

    }
}