using System.ComponentModel.DataAnnotations;
using ADWebApplication.Data;
using ADWebApplication.Models;
using ADWebApplication.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ADWebApplication.Controllers
{
    [Authorize(Roles = "HR")]
    public class HrController : Controller
    {
        private readonly EmpDbContext _db;

        public HrController(EmpDbContext empDb)
        {
            _db = empDb;
        }

        [HttpGet]
        public async Task<IActionResult> CreateEmployee()
        {
            ViewBag.Roles = await _db.Roles
                .Where(r => r.Name != "HR")
                .Select(r => r.Name)
                .ToListAsync();

            return View(new CreateEmployeeViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEmployee(CreateEmployeeViewModel model)
        {
            ViewBag.Roles = await _db.Roles
                .Where(r => r.Name != "HR")
                .Select(r => r.Name)
                .ToListAsync();

            if (!ModelState.IsValid) return View(model);

            bool exists = await _db.EmpAccounts.AnyAsync(u => u.Username == model.Username);
            if (exists)
            {
                ModelState.AddModelError(nameof(model.Username), "This Employee ID already exists.");
                return View(model);
            }

            var role = await _db.Roles.FirstOrDefaultAsync(r => r.Name == model.RoleName);
            if (role == null)
            {
                ModelState.AddModelError(nameof(model.RoleName), "Invalid role.");
                return View(model);
            }

            string tempPassword = "Temp@" + new Random().Next(100000, 999999);

            var user = new EmpAccount
            {
                Username = model.Username,
                FullName = model.FullName,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                IsActive = true,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password)

            };

            _db.EmpAccounts.Add(user);
            await _db.SaveChangesAsync();

            _db.EmpRoles.Add(new EmpRole { EmpAccountId = user.Id, RoleId = role.Id });
            await _db.SaveChangesAsync();

            TempData["Message"] = "Employee created successfully.";
            return RedirectToAction(nameof(CreateEmployee));
        }
    }
}