using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ADWebApplication.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ADWebApplication.Controllers;

// Mark Bins as collected, submit timestamp and update bin level
public class BinCollectionController : Controller
{

    [HttpGet]
    public IActionResult MarkCollected(int? routePlanId)
    {
        var model = new BinCollectionViewModel
        {
            RoutePlans = DummyRouteRepository.GetRoutePlans()
        };

        if (routePlanId.HasValue)
        {
            model.SelectedRoutePlanId = routePlanId;
            model.RouteStops = DummyRouteRepository.GetStopsForRoute(routePlanId.Value);
        }

        return View(model);
    }

    [HttpGet]
    public IActionResult GetRouteStops(int routePlanId)
    {
        // To replace with actual database logic
        var stops = DummyRouteRepository.GetStopsForRoute(routePlanId);

        // Return JSON
        return Json(stops.Select(s => new
        {
            StopId = s.StopId,
            StopSequence = s.StopSequence,
            LocationName = s.CollectionBin?.LocationName
        }));
    }

   [HttpPost]
    public IActionResult MarkCollected(BinCollectionViewModel model)
    {
        // Get route plans on Post
        model.RoutePlans = DummyRouteRepository.GetRoutePlans();

         // Validate Route Stop selection
        if (!model.SelectedRouteStopId.HasValue)
        {
            ModelState.AddModelError(nameof(model.SelectedRouteStopId), "Please select a Route Stop.");
        }

        // Convert local datetime to DateTimeOffset using client timezone offset
        if (model.CollectionDetails != null)
        {
            var local = model.CollectionDetails.CollectionDateTimeLocal;
            var offset = TimeSpan.FromMinutes(-model.TimezoneOffset);
            model.CollectionDetails.CollectionDateTimeOffset = new DateTimeOffset(local, offset);
        }

        // Validate
        if (!ModelState.IsValid)
        {
            if (model.SelectedRoutePlanId.HasValue)
            {
                model.RouteStops =
                    DummyRouteRepository.GetStopsForRoute(model.SelectedRoutePlanId.Value);
            }
            return View(model);
        }

        // Save to DB Method
        // ...

        return RedirectToAction(nameof(Success));
    }

    public IActionResult Success()
    {
        return View();
    }
}