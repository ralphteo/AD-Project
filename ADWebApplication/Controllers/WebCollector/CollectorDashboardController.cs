using ADWebApplication.Models;
using ADWebApplication.ViewModels;
using ADWebApplication.Services.Collector;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ADWebApplication.Controllers
{
    [Authorize(Roles = "Collector")]
    public class CollectorDashboardController : Controller
    {
        private readonly ICollectorService _collectorService;
    private readonly IConfiguration _config;

    public CollectorDashboardController(
        ICollectorService collectorService,
        IConfiguration config)
    {
        _collectorService = collectorService;
        _config = config;
    }

    public async Task<IActionResult> Index()
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        ViewBag.GoogleMapsKey = _config["GOOGLE_MAPS_API_KEY"];

        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username)) return Unauthorized();

        var route = await _collectorService.GetDailyRouteAsync(username);
        return View(route);
    }

        [HttpGet]
        public async Task<IActionResult> ConfirmCollection(int id)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return Unauthorized();

            var viewModel = await _collectorService.GetCollectionConfirmationAsync(id, username);
            if (viewModel == null)
            {
                TempData["ErrorMessage"] = "Issue not found for this route.";
                return RedirectToAction("ReportIssue");
            }

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmCollection(CollectionConfirmationVM model)
        {
            if (!ModelState.IsValid)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    var errors = ModelState
                        .Where(entry => entry.Value?.Errors.Count > 0)
                        .ToDictionary(
                            entry => entry.Key,
                            entry => entry.Value!.Errors.Select(error => error.ErrorMessage).ToArray()
                        );
                    return BadRequest(new { success = false, errors });
                }
                return View("ConfirmCollection", model);
            }

            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return Unauthorized();

            await _collectorService.ConfirmCollectionAsync(model, username);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new
                {
                    success = true,
                    stopId = model.StopId,
                    collectedAt = model.CollectionTime.ToString("HH:mm")
                });
            }

            return View("CollectionConfirmed", model);
        }

        public IActionResult CollectionConfirmed(CollectionConfirmationVM model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ReportIssue(string? search, string? status, string? priority)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return Unauthorized();

            var model = await _collectorService.GetReportIssueViewModelAsync(username, search, status, priority);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ReportIssue(ReportIssueVM model)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return Unauthorized();

            if (!ModelState.IsValid)
            {
                var freshModel = await _collectorService.GetReportIssueViewModelAsync(username, model.Search, model.StatusFilter, model.PriorityFilter);
                model.AvailableBins = freshModel.AvailableBins;
                model.Issues = freshModel.Issues;
                model.TotalIssues = freshModel.TotalIssues;
                model.OpenIssues = freshModel.OpenIssues;
                model.InProgressIssues = freshModel.InProgressIssues;
                model.ResolvedIssues = freshModel.ResolvedIssues;
                return View(model);
            }

            await _collectorService.SubmitIssueAsync(model, username);
            TempData["SuccessMessage"] = $"Issue reported for Bin #{model.BinId} - {model.LocationName}";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartIssueWork(int stopId)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return Unauthorized();

            var result = await _collectorService.StartIssueWorkAsync(stopId, username);
            
            if (result == "Issue not found for this route.")
            {
                TempData["ErrorMessage"] = result;
            }
            else if (result == "Issue is already resolved.")
            {
                TempData["SuccessMessage"] = result;
            }
            else
            {
                TempData["SuccessMessage"] = result == "Resolved"
                    ? "Issue marked as Resolved."
                    : "Issue marked as In Progress.";
            }

            return RedirectToAction("ReportIssue");
        }

        [HttpGet]
        public async Task<IActionResult> MyRouteAssignments(string? search, int? regionId, DateTime? date, string? status, int page = 1, int pageSize = 10)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return Unauthorized();

            var viewModel = await _collectorService.GetRouteAssignmentsAsync(username, search, regionId, date, status, page, pageSize);
            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> RouteAssignmentDetails(int id)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return Unauthorized();

            var viewModel = await _collectorService.GetRouteAssignmentDetailsAsync(id, username);
            if (viewModel == null) return NotFound();

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> GetNextStops(int? top = 10)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return Unauthorized();

            var viewModel = await _collectorService.GetNextStopsAsync(username, top ?? 10);
            if (viewModel == null) return NotFound(new { message = "No active route assignment for today" });

            return Json(viewModel);
        }
    }
}
