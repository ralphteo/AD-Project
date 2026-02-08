using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ADWebApplication.Data.Repository;
using ADWebApplication.Data;
using ADWebApplication.Models;

namespace ADWebApplication.Controllers
{
    [Authorize(Roles = "Admin")] 
    [Route("Admin")]
    public class AdminBinCrudController : Controller
    {
        private readonly IAdminRepository _adminRepository;

        public AdminBinCrudController(IAdminRepository adminRepository)
        {
            _adminRepository = adminRepository;
        }

        // ---------------------------------- Manage Collection Bins --------------------------------------------- //
        [HttpGet("Bins")]
        public async Task<IActionResult> Bins()
        {
            var bins = await _adminRepository.GetAllBinsAsync();
            ViewBag.Regions = await _adminRepository.GetAllRegionsAsync();
            return View("~/Views/Admin/Bins.cshtml", bins);
        }

        // POST: /Admin/EditBin
        [HttpPost("EditBin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBin(CollectionBin editedBin)
        {
            if (!ModelState.IsValid)
            {
                // If model is invalid, return the view with the current data.
                return View(editedBin);
            }

            // Find the existing bin to be edited by its BinId
            var bin = await _adminRepository.GetBinByIdAsync(editedBin.BinId);
            if (bin == null)
            {
                // If no bin with the provided BinId is found, return NotFound
                return NotFound();
            }

            // Update bin object properties
            bin.RegionId = editedBin.RegionId;
            bin.LocationName = editedBin.LocationName;
            bin.LocationAddress = editedBin.LocationAddress;
            bin.BinCapacity = editedBin.BinCapacity;
            bin.BinStatus = editedBin.BinStatus;
            bin.Latitude = editedBin.Latitude;
            bin.Longitude = editedBin.Longitude;

            // Save to database
            await _adminRepository.UpdateBinAsync(bin);

            // Set a success message in TempData
            TempData["SuccessMessage"] = $"Bin '{editedBin.LocationAddress}' has been updated successfully.";

            // Redirect back to bins list page after successful edit
            return RedirectToAction("Bins");
        }


        // POST: /Admin/DeleteBin/{id}
        [HttpPost("DeleteBin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBin(int binId)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction("Bins");
            }

            var bin = await _adminRepository.GetBinByIdAsync(binId);
            if (bin == null)
                return NotFound();

            await _adminRepository.DeleteBinAsync(binId);

            TempData["DeletedBinId"] = bin.BinId;
            TempData["DeletedBinLocation"] = bin.LocationAddress;

            return RedirectToAction("BinDeleted");
        }

        [HttpGet("BinDeleted")]
        public IActionResult BinDeleted()
        {
            return View("~/Views/Admin/BinDeleted.cshtml"); // Bin deletion confirmation view
        }

        // POST: /Admin/CreateBin
        [HttpPost("CreateBin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBin(CollectionBin newBin)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Regions = await _adminRepository.GetAllRegionsAsync();
                return View(newBin);
            }

            await _adminRepository.CreateBinAsync(newBin);

            TempData["SuccessMessage"] = $"Bin '{newBin.LocationAddress}' has been created successfully.";

            // Redirect back to bins list page after successful creation
            return RedirectToAction("Bins");
        }

        // ---------------------------------- Manage Collection Officers --------------------------------------------- //

        /*public async Task<IActionResult> CollectionOfficerRoster()
        {
            var officers = await _adminRepository.GetAllCollectionOfficersAsync();
            return View(officers);
        }*/

        [HttpGet("CollectionOfficerSchedule")]
        public async Task<IActionResult> CollectionOfficerSchedule(string officerUsername)
        {
            // Get the current date minus one year
            var oneYearAgo = DateTime.Now.AddYears(-1);

            // Fetch route assignments for the officer within the last year
            var routeAssignments = await _adminRepository.GetRouteAssignmentsForOfficerAsync(officerUsername, oneYearAgo);
            var officer = await _adminRepository.GetEmployeeByUsernameAsync(officerUsername);
            ViewBag.OfficerFullName = officer?.FullName ?? officerUsername;

            return View("~/Views/Admin/CollectionOfficerSchedule.cshtml", routeAssignments);
        }

        [HttpGet("CollectionOfficerRoster")]
       
        public async Task<IActionResult> CollectionOfficerRoster(DateTime? dateFrom, DateTime? dateTo)
        {
            if (!ModelState.IsValid)
            {
                var allOfficers = await _adminRepository.GetAllCollectionOfficersAsync();
                return View("~/Views/Admin/CollectionOfficerRoster.cshtml", allOfficers);
            }

            if (dateFrom.HasValue && dateTo.HasValue)
            {
                var availableOfficers = await _adminRepository
                    .GetAvailableCollectionOfficersAsync(dateFrom.Value, dateTo.Value);

                return View("~/Views/Admin/CollectionOfficerRoster.cshtml", availableOfficers);
            }

            var officers = await _adminRepository.GetAllCollectionOfficersAsync();
            return View("~/Views/Admin/CollectionOfficerRoster.cshtml", officers);
        }

        [HttpGet("CollectionCalendar")]
        public async Task<IActionResult> CollectionCalendar (string username)
        {


            return View("~/Views/Admin/CollectionCalendar.cshtml");
        }

        [HttpGet("GetOfficerAvailability")]
        public async Task<IActionResult> GetOfficerAvailability(DateTime from, DateTime to)
        {
            if (!ModelState.IsValid)
            {
                var allOfficers = await _adminRepository.GetAllCollectionOfficersAsync();
                return View("~/Views/Admin/CollectionOfficerRoster.cshtml", allOfficers);
            }
            var available = await _adminRepository
                .GetAvailableCollectionOfficersCalendarAsync(from, to);

            var assigned = await _adminRepository
                .GetAssignedCollectionOfficersCalendarAsync(from, to);

            return Json(new { available, assigned });
        }


    }
}
