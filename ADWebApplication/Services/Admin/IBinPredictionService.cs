using ADWebApplication.Models.ViewModels.BinPredictions;

namespace ADWebApplication.Services
{
    public interface IBinPredictionService
    {
        Task<BinPredictionsPageViewModel> BuildBinPredictionsPageAsync(int page, string sort, string sortDir, string risk, string timeframe);

        Task<int> RefreshPredictionsForNewCyclesAsync();
    }
}