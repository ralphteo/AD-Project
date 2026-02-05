using ADWebApplication.Models;
using ADWebApplication.ViewModels;

namespace ADWebApplication.Services.Collector
{
    public interface ICollectorDashboardService
    {
        Task<CollectorRoute> GetDailyRouteAsync(string username);
        Task<CollectionConfirmationVM?> GetCollectionConfirmationAsync(int stopId, string username);
        Task<bool> ConfirmCollectionAsync(CollectionConfirmationVM model, string username);
    }
}
