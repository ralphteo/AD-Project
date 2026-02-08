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
        private readonly In5niteDbContext _db;

        public HrController(In5niteDbContext db)
        {
            _db = db;
        }

        // LIST
        [HttpGet]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var list = await _db.Employees
                .Include(e => e.Role)
                .OrderBy(e => e.Username)
                .Select(e => new EmployeeRowViewModel
                {
                    Id = e.Id,

                    Username = e.Username,
                    FullName = e.FullName,
                    Email = e.Email,
                    IsActive = e.IsActive,
                    RoleName = e.Role != null ? e.Role.Name : "-"
                })
                .ToListAsync();

            return View(list);
        }

        // CREATE (GET)
        [HttpGet]
        public async Task<IActionResult> CreateEmployee()
        {
            ViewBag.Roles = await _db.Roles
                .Where(r => r.Name != "HR")
                .OrderBy(r => r.Name)
                .ToListAsync();

            return View(new CreateEmployeeViewModel());
        }

        // CREATE (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEmployee(CreateEmployeeViewModel vm)
        {
            ViewBag.Roles = await _db.Roles
                .Where(r => r.Name != "HR")
                .OrderBy(r => r.Name)
                .ToListAsync();

            if (!ModelState.IsValid) return View(vm);

            var username = vm.Username.Trim();

            if (await _db.Employees.AnyAsync(e => e.Username == username))
            {
                ModelState.AddModelError(nameof(vm.Username), "Employee ID already exists");
                return View(vm);
            }

            // validate role
            var role = await _db.Roles.FirstOrDefaultAsync(r => r.RoleId == vm.RoleId && r.Name != "HR");
            if (role == null)
            {
                ModelState.AddModelError(nameof(vm.RoleId), "Invalid role");
                return View(vm);
            }

            var emp = new Employee
            {
                Username = username,
                FullName = vm.FullName.Trim(),
                Email = vm.Email.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(vm.Password),
                RoleId = role.RoleId,
                IsActive = true
            };

            _db.Employees.Add(emp);
            await _db.SaveChangesAsync();

            TempData["Message"] = "Employee created";
            return RedirectToAction(nameof(Index));
        }

        // EDIT (GET) - use Username PK
        [HttpGet]
        public async Task<IActionResult> Edit(string username)
        {
            if (string.IsNullOrWhiteSpace(username)) return NotFound();

            var emp = await _db.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Username == username);

            if (emp == null) return NotFound();

            ViewBag.Roles = await _db.Roles
                .Where(r => r.Name != "HR")
                .OrderBy(r => r.Name)
                .ToListAsync();

            var vm = new EditEmployeeViewModel
            {
                Username = emp.Username,
                FullName = emp.FullName,
                Email = emp.Email,
                RoleId = emp.RoleId,
                IsActive = emp.IsActive
            };

            return View(vm);
        }

        // EDIT (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditEmployeeViewModel vm)
        {
            ViewBag.Roles = await _db.Roles
                .Where(r => r.Name != "HR")
                .OrderBy(r => r.Name)
                .ToListAsync();

            if (!ModelState.IsValid) return View(vm);

            var emp = await _db.Employees.FirstOrDefaultAsync(e => e.Username == vm.Username);
            if (emp == null) return NotFound();

            // validate role
            var role = await _db.Roles.FirstOrDefaultAsync(r => r.RoleId == vm.RoleId && r.Name != "HR");
            if (role == null)
            {
                ModelState.AddModelError(nameof(vm.RoleId), "Invalid role");
                return View(vm);
            }

            emp.FullName = vm.FullName.Trim();
            emp.Email = vm.Email.Trim();
            emp.RoleId = vm.RoleId;
            emp.IsActive = vm.IsActive;

            // optional password reset
            if (!string.IsNullOrWhiteSpace(vm.NewPassword))
            {
                emp.PasswordHash = BCrypt.Net.BCrypt.HashPassword(vm.NewPassword);
            }

            await _db.SaveChangesAsync();

            TempData["Message"] = "Employee updated";
            return RedirectToAction(nameof(Index));
        }

        // DELETE (GET) - use Username PK
        [HttpGet]
        public async Task<IActionResult> Delete(string username)
        {
            if (string.IsNullOrWhiteSpace(username)) return NotFound();

            var emp = await _db.Employees
                .Include(e => e.Role)
                .FirstOrDefaultAsync(e => e.Username == username);

            if (emp == null) return NotFound();

            return View(emp);
        }

        // DELETE (POST)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string username)
        {
            if (string.IsNullOrWhiteSpace(username)) return NotFound();

            var emp = await _db.Employees.FirstOrDefaultAsync(e => e.Username == username);
            if (emp == null) return NotFound();

            _db.Employees.Remove(emp);
            await _db.SaveChangesAsync();

            TempData["Message"] = "Employee deleted";
            return RedirectToAction(nameof(Index));
        }
    }
}
