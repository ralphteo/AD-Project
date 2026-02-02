using Microsoft.AspNetCore.Mvc;
using ADWebApplication.Services;

namespace ADWebApplication.ViewComponents;

public class HighRiskBinBadgeViewComponent : ViewComponent
{
    private readonly BinPredictionService _predictionService;

    public HighRiskBinBadgeViewComponent(BinPredictionService predictionService)
    {
        _predictionService = predictionService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var vm = await _predictionService.BuildBinPredictionsPageAsync(
            page: 1,
            sort: "DaysToThreshold",
            sortDir: "asc",
            risk: "High",
            timeframe: "3"
        );

        return View(vm.HighRiskUnscheduledCount);
    }
}