using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ADWebApplication.Data.Repository;
using ADWebApplication.Data;
using ADWebApplication.Models;

namespace ADWebApplication.Controllers
{
    // [Authorize(Roles = "Admin")] * Uncomment this line to restrict access to Admins only
    public class AdminController : Controller
    {
        private readonly IAdminRepository _adminRepository;

        public AdminController(IAdminRepository adminRepository)
        {
            _adminRepository = adminRepository;
        }

        // ---------------------------------- Manage Collection Bins --------------------------------------------- //
        public async Task<IActionResult> Bins()
        {
            var bins = await _adminRepository.GetAllBinsAsync();
            ViewBag.Regions = await _adminRepository.GetAllRegionsAsync();
            return View(bins);
        }

        // POST: /Admin/EditBin
        [HttpPost]
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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBin(int binId)
        {
            var bin = await _adminRepository.GetBinByIdAsync(binId);
            if (bin == null)
                return NotFound();

            await _adminRepository.DeleteBinAsync(binId);

            TempData["DeletedBinId"] = bin.BinId;
            TempData["DeletedBinLocation"] = bin.LocationAddress;

            return RedirectToAction("BinDeleted");
        }

        public IActionResult BinDeleted()
        {
            return View(); // Bin deletion confirmation view
        }

        // POST: /Admin/CreateBin
        [HttpPost]
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

        public async Task<IActionResult> CollectionOfficerSchedule(string officerUsername)
        {
            // Get the current date minus one year
            var oneYearAgo = DateTime.Now.AddYears(-1);

            // Fetch route assignments for the officer within the last year
            var routeAssignments = await _adminRepository.GetRouteAssignmentsForOfficerAsync(officerUsername, oneYearAgo);
            var officer = await _adminRepository.GetEmployeeByUsernameAsync(officerUsername);
            ViewBag.OfficerFullName = officer?.FullName ?? officerUsername;

            return View(routeAssignments);
        }

        public async Task<IActionResult> CollectionOfficerRoster(DateTime? dateFrom, DateTime? dateTo)
        {
            if (dateFrom.HasValue && dateTo.HasValue)
            {
                var availableOfficers = await _adminRepository
                    .GetAvailableCollectionOfficersAsync(dateFrom.Value, dateTo.Value);

                return View(availableOfficers);
            }

            var officers = await _adminRepository.GetAllCollectionOfficersAsync();
            return View(officers);
        }


        public async Task<IActionResult> CollectionCalender(string username)
        {


            return View();
        }

    }
}
