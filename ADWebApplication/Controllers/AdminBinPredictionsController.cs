using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ADWebApplication.Services;

[Authorize(Roles = "Admin")]
[Route("Admin/BinPredictions")]
public class AdminBinPredictionsController : Controller
{
    private readonly IBinPredictionService _binPredictionService;

    public AdminBinPredictionsController(IBinPredictionService binPredictionService)
    {
        _binPredictionService = binPredictionService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(int page = 1, string sort = "DaysToThreshold", string sortDir = "asc", string risk = "All", string timeframe = "All")
    {
        var viewModel = await _binPredictionService
            .BuildBinPredictionsPageAsync(page, sort, sortDir, risk, timeframe);

        return View(viewModel);
    }

    [HttpPost("Refresh")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Refresh()
    {
        int refreshed = await _binPredictionService.RefreshPredictionsForNewCyclesAsync();

        TempData["PredictionRefreshSuccess"] = $"{refreshed} bin prediction(s) refreshed successfully.";
        
        return RedirectToAction(nameof(Index));
    }
}