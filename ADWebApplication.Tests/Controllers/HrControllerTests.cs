using Xunit;
using ADWebApplication.Controllers;
using ADWebApplication.Data;
using ADWebApplication.Models;
using ADWebApplication.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ADWebApplication.Tests.Controllers
{
    public class HrControllerTests : IDisposable
    {
        private readonly DbContextOptions<In5niteDbContext> _options;

        public HrControllerTests()
        {
            _options = new DbContextOptionsBuilder<In5niteDbContext>()
                .UseInMemoryDatabase(databaseName: $"HrTestDb_{Guid.NewGuid()}")
                .Options;
        }

        public void Dispose()
        {
            using var context = new In5niteDbContext(_options);
            context.Database.EnsureDeleted();
            GC.SuppressFinalize(this);
        }

        private In5niteDbContext CreateContext() => new In5niteDbContext(_options);

        private async Task SeedTestData()
        {
            using var context = CreateContext();

            var hrRole = new Role { Name = "HR" };
            var adminRole = new Role { Name = "Admin" };
            var collectorRole = new Role { Name = "Collector" };
            context.Roles.AddRange(hrRole, adminRole, collectorRole);
            await context.SaveChangesAsync();

            var employee1 = new Employee
            {
                Username = "EMP001",
                FullName = "John Doe",
                Email = "john@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123"),
                RoleId = adminRole.RoleId,
                IsActive = true
            };

            var employee2 = new Employee
            {
                Username = "EMP002",
                FullName = "Jane Smith",
                Email = "jane@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password456"),
                RoleId = collectorRole.RoleId,
                IsActive = false
            };

            context.Employees.AddRange(employee1, employee2);
            await context.SaveChangesAsync();
        }

        private HrController CreateController()
        {
            var context = CreateContext();
            var controller = new HrController(context);

            // Setup HttpContext and TempData
            var httpContext = new DefaultHttpContext();
            var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
            controller.TempData = tempData;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "HR001"),
                new Claim(ClaimTypes.Name, "HR001"),
                new Claim(ClaimTypes.Role, "HR")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };

            return controller;
        }

        #region Index Tests

        [Fact]
        public async Task Index_ReturnsView_WithListOfEmployees()
        {
            // Arrange
            await SeedTestData();
            var controller = CreateController();

            // Act
            var result = await controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<List<EmployeeRowViewModel>>(viewResult.Model, exactMatch: false);
            Assert.Equal(2, model.Count);
        }

        [Fact]
        public async Task Index_OrdersEmployeesByUsername()
        {
            // Arrange
            await SeedTestData();
            var controller = CreateController();

            // Act
            var result = await controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<List<EmployeeRowViewModel>>(viewResult.Model, exactMatch: false);
            Assert.Equal("EMP001", model.First().Username);
            Assert.Equal("EMP002", model.Last().Username);
        }

        [Fact]
        public async Task Index_IncludesRoleInformation()
        {
            // Arrange
            await SeedTestData();
            var controller = CreateController();

            // Act
            var result = await controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<List<EmployeeRowViewModel>>(viewResult.Model, exactMatch: false);
            Assert.Equal("Admin", model.First().RoleName);
            Assert.Equal("Collector", model.Last().RoleName);
        }

        #endregion

        #region CreateEmployee GET Tests

        [Fact]
        public async Task CreateEmployee_ReturnsView_WithRolesInViewBag()
        {
            // Arrange
            await SeedTestData();
            var controller = CreateController();

            // Act
            var result = await controller.CreateEmployee();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.IsType<CreateEmployeeViewModel>(viewResult.Model);
            var roles = Assert.IsAssignableFrom<List<Role>>(controller.ViewBag.Roles);
            Assert.Equal(2, roles.Count); // Should exclude HR role
            foreach (var role in roles)
            {
                Assert.NotEqual("HR", role.Name);
            }
        }

        #endregion

        #region CreateEmployee POST Tests

        [Fact]
        public async Task CreateEmployee_ReturnsView_WhenModelInvalid()
        {
            // Arrange
            await SeedTestData();
            var controller = CreateController();
            controller.ModelState.AddModelError("Username", "Required");

            var model = new CreateEmployeeViewModel();

            // Act
            var result = await controller.CreateEmployee(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.IsType<CreateEmployeeViewModel>(viewResult.Model);
        }

        [Fact]
        public async Task CreateEmployee_ReturnsError_WhenUsernameExists()
        {
            // Arrange
            await SeedTestData();
            var controller = CreateController();

            var model = new CreateEmployeeViewModel
            {
                Username = "EMP001",
                FullName = "Duplicate User",
                Email = "duplicate@example.com",
                Password = "Password123",
                RoleId = 1
            };

            // Act
            var result = await controller.CreateEmployee(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            Assert.Contains("Employee ID already exists", 
                controller.ModelState[nameof(model.Username)]!.Errors.Select(e => e.ErrorMessage));
        }

        [Fact]
        public async Task CreateEmployee_ReturnsError_WhenRoleInvalid()
        {
            // Arrange
            await SeedTestData();
            var controller = CreateController();

            var model = new CreateEmployeeViewModel
            {
                Username = "EMP003",
                FullName = "New User",
                Email = "new@example.com",
                Password = "Password123",
                RoleId = 999 // Invalid role ID
            };

            // Act
            var result = await controller.CreateEmployee(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            Assert.Contains("Invalid role", 
                controller.ModelState[nameof(model.RoleId)]!.Errors.Select(e => e.ErrorMessage));
        }

        [Fact]
        public async Task CreateEmployee_ReturnsError_WhenAttemptingToCreateHRRole()
        {
            // Arrange
            await SeedTestData();
            var controller = CreateController();

            // Get HR role ID
            int hrRoleId;
            using (var context = CreateContext())
            {
                hrRoleId = context.Roles.First(r => r.Name == "HR").RoleId;
            }

            var model = new CreateEmployeeViewModel
            {
                Username = "EMP003",
                FullName = "New HR",
                Email = "newhr@example.com",
                Password = "Password123",
                RoleId = hrRoleId
            };

            // Act
            var result = await controller.CreateEmployee(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            Assert.Contains("Invalid role", 
                controller.ModelState[nameof(model.RoleId)]!.Errors.Select(e => e.ErrorMessage));
        }

        [Fact]
        public async Task CreateEmployee_CreatesEmployee_WhenValid()
        {
            // Arrange
            await SeedTestData();
            var controller = CreateController();

            int adminRoleId;
            using (var context = CreateContext())
            {
                adminRoleId = context.Roles.First(r => r.Name == "Admin").RoleId;
            }

            var model = new CreateEmployeeViewModel
            {
                Username = "EMP003",
                FullName = "New Employee",
                Email = "newemp@example.com",
                Password = "Password123",
                RoleId = adminRoleId
            };

            // Act
            var result = await controller.CreateEmployee(model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Employee created", controller.TempData["Message"]);

            // Verify employee was created
            using (var context = CreateContext())
            {
                var employee = await context.Employees.FirstOrDefaultAsync(e => e.Username == "EMP003");
                Assert.NotNull(employee);
                Assert.Equal("New Employee", employee.FullName);
                Assert.Equal("newemp@example.com", employee.Email);
                Assert.True(employee.IsActive);
            }
        }

        [Fact]
        public async Task CreateEmployee_HashesPassword()
        {
            // Arrange
            await SeedTestData();
            var controller = CreateController();

            int adminRoleId;
            using (var context = CreateContext())
            {
                adminRoleId = context.Roles.First(r => r.Name == "Admin").RoleId;
            }

            var model = new CreateEmployeeViewModel
            {
                Username = "EMP004",
                FullName = "Test User",
                Email = "test@example.com",
                Password = "PlainPassword",
                RoleId = adminRoleId
            };

            // Act
            await controller.CreateEmployee(model);

            // Assert
            using (var context = CreateContext())
            {
                var employee = await context.Employees.FirstAsync(e => e.Username == "EMP004");
                Assert.NotEqual("PlainPassword", employee.PasswordHash);
                Assert.True(BCrypt.Net.BCrypt.Verify("PlainPassword", employee.PasswordHash));
            }
        }

        #endregion

        #region Edit GET Tests

        [Fact]
        public async Task Edit_ReturnsNotFound_WhenUsernameNull()
        {
            // Arrange
            await SeedTestData();
            var controller = CreateController();

            // Act
            var result = await controller.Edit((string)null!);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Edit_ReturnsNotFound_WhenEmployeeNotFound()
        {
            // Arrange
            await SeedTestData();
            var controller = CreateController();

            // Act
            var result = await controller.Edit("INVALID");

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Edit_ReturnsView_WithEmployee()
        {
            // Arrange
            await SeedTestData();
            var controller = CreateController();

            // Act
            var result = await controller.Edit("EMP001");

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<EditEmployeeViewModel>(viewResult.Model);
            Assert.Equal("EMP001", model.Username);
            Assert.Equal("John Doe", model.FullName);
            Assert.Equal("john@example.com", model.Email);
        }

        [Fact]
        public async Task Edit_PopulatesRolesInViewBag()
        {
            // Arrange
            await SeedTestData();
            var controller = CreateController();

            // Act
            var result = await controller.Edit("EMP001");

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var roles = Assert.IsAssignableFrom<List<Role>>(controller.ViewBag.Roles);
            foreach (var role in roles)
            {
                Assert.NotEqual("HR", role.Name);
            }
        }

        #endregion

        #region Edit POST Tests

        [Fact]
        public async Task Edit_ReturnsView_WhenModelInvalid()
        {
            // Arrange
            await SeedTestData();
            var controller = CreateController();
            controller.ModelState.AddModelError("FullName", "Required");

            var model = new EditEmployeeViewModel { Username = "EMP001" };

            // Act
            var result = await controller.Edit(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.IsType<EditEmployeeViewModel>(viewResult.Model);
        }

        [Fact]
        public async Task EditPost_ReturnsNotFound_WhenEmployeeNotFound()
        {
            // Arrange
            await SeedTestData();
            var controller = CreateController();

            var model = new EditEmployeeViewModel
            {
                Username = "INVALID",
                FullName = "Test",
                Email = "test@example.com",
                RoleId = 1,
                IsActive = true
            };

            // Act
            var result = await controller.Edit(model);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task EditPost_ReturnsError_WhenRoleInvalid()
        {
            // Arrange
            await SeedTestData();
            var controller = CreateController();

            var model = new EditEmployeeViewModel
            {
                Username = "EMP001",
                FullName = "Updated Name",
                Email = "updated@example.com",
                RoleId = 999, // Invalid
                IsActive = true
            };

            // Act
            var result = await controller.Edit(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
        }

        [Fact]
        public async Task EditPost_UpdatesEmployee_WithoutPasswordChange()
        {
            // Arrange
            await SeedTestData();
            var controller = CreateController();

            string originalPasswordHash;
            int adminRoleId;
            using (var context = CreateContext())
            {
                var emp = await context.Employees.FirstAsync(e => e.Username == "EMP001");
                originalPasswordHash = emp.PasswordHash;
                adminRoleId = context.Roles.First(r => r.Name == "Admin").RoleId;
            }

            var model = new EditEmployeeViewModel
            {
                Username = "EMP001",
                FullName = "Updated Name",
                Email = "updated@example.com",
                RoleId = adminRoleId,
                IsActive = false,
                NewPassword = null // No password change
            };

            // Act
            var result = await controller.Edit(model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);

            using (var context = CreateContext())
            {
                var employee = await context.Employees.FirstAsync(e => e.Username == "EMP001");
                Assert.Equal("Updated Name", employee.FullName);
                Assert.Equal("updated@example.com", employee.Email);
                Assert.False(employee.IsActive);
                Assert.Equal(originalPasswordHash, employee.PasswordHash); // Password unchanged
            }
        }

        [Fact]
        public async Task EditPost_UpdatesPassword_WhenNewPasswordProvided()
        {
            // Arrange
            await SeedTestData();
            var controller = CreateController();

            string originalPasswordHash;
            int adminRoleId;
            using (var context = CreateContext())
            {
                var emp = await context.Employees.FirstAsync(e => e.Username == "EMP001");
                originalPasswordHash = emp.PasswordHash;
                adminRoleId = context.Roles.First(r => r.Name == "Admin").RoleId;
            }

            var model = new EditEmployeeViewModel
            {
                Username = "EMP001",
                FullName = "John Doe",
                Email = "john@example.com",
                RoleId = adminRoleId,
                IsActive = true,
                NewPassword = "NewPassword456"
            };

            // Act
            await controller.Edit(model);

            // Assert
            using (var context = CreateContext())
            {
                var employee = await context.Employees.FirstAsync(e => e.Username == "EMP001");
                Assert.NotEqual(originalPasswordHash, employee.PasswordHash);
                Assert.True(BCrypt.Net.BCrypt.Verify("NewPassword456", employee.PasswordHash));
            }
        }

        #endregion

        #region Delete GET Tests

        [Fact]
        public async Task Delete_ReturnsNotFound_WhenUsernameNull()
        {
            // Arrange
            await SeedTestData();
            var controller = CreateController();

            // Act
            var result = await controller.Delete(null!);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Delete_ReturnsNotFound_WhenEmployeeNotFound()
        {
            // Arrange
            await SeedTestData();
            var controller = CreateController();

            // Act
            var result = await controller.Delete("INVALID");

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Delete_ReturnsView_WithEmployee()
        {
            // Arrange
            await SeedTestData();
            var controller = CreateController();

            // Act
            var result = await controller.Delete("EMP001");

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Employee>(viewResult.Model);
            Assert.Equal("EMP001", model.Username);
        }

        #endregion

        #region Delete POST Tests

        [Fact]
        public async Task DeleteConfirmed_ReturnsNotFound_WhenUsernameNull()
        {
            // Arrange
            await SeedTestData();
            var controller = CreateController();

            // Act
            var result = await controller.DeleteConfirmed(null!);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DeleteConfirmed_ReturnsNotFound_WhenEmployeeNotFound()
        {
            // Arrange
            await SeedTestData();
            var controller = CreateController();

            // Act
            var result = await controller.DeleteConfirmed("INVALID");

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DeleteConfirmed_DeletesEmployee()
        {
            // Arrange
            await SeedTestData();
            var controller = CreateController();

            // Act
            var result = await controller.DeleteConfirmed("EMP001");

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Employee deleted", controller.TempData["Message"]);

            using (var context = CreateContext())
            {
                var employee = await context.Employees.FirstOrDefaultAsync(e => e.Username == "EMP001");
                Assert.Null(employee);
            }
        }

        #endregion
    }
}
