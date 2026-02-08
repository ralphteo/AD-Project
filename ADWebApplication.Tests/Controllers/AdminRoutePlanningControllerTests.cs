using Moq;
using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ADWebApplication.Data;
using ADWebApplication.Controllers.WebAdmin;
using ADWebApplication.Services;
using ADWebApplication.Models;
using ADWebApplication.Models.DTOs;
using ADWebApplication.Models.ViewModels;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace ADWebApplication.Tests;

public class AdminRoutePlanningControllerTests
{
    private readonly Mock<IRoutePlanningService> _mockPlanningService;
    private readonly Mock<IRouteAssignmentService> _mockAssignmentService;
    private readonly In5niteDbContext _dbContext;
    private readonly AdminRoutePlanningController _controller;

    public AdminRoutePlanningControllerTests()
    {
        _mockPlanningService = new Mock<IRoutePlanningService>();
        _mockAssignmentService = new Mock<IRouteAssignmentService>();

        // Setup In-Memory DB with unique name per test run
        var options = new DbContextOptionsBuilder<In5niteDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new In5niteDbContext(options);

        // Seed data for Role filtering tests
        _dbContext.Employees.Add(new Employee { Username = "officer1", FullName = "Officer One", RoleId = 3 });
        _dbContext.SaveChanges();

        _controller = new AdminRoutePlanningController(
            _mockPlanningService.Object, 
            _mockAssignmentService.Object, 
            _dbContext);

        // Setup User Identity and Controller Context
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
            new Claim(ClaimTypes.Name, "test-admin"),
        }, "mock"));

        _controller.ControllerContext = new ControllerContext()
        {
            HttpContext = new DefaultHttpContext() { User = user }
        };

        // Setup TempData
        _controller.TempData = new TempDataDictionary(_controller.HttpContext, Mock.Of<ITempDataProvider>());
    }

    [Fact]
    public async Task Index_ReturnsViewWithSavedRoutes_WhenRoutesExist()
    {
        // Arrange
        var savedRoutes = new List<SavedRouteStopDto> {
            new SavedRouteStopDto { RouteKey = 1, BinId = 1, StopNumber = 1, AssignedOfficerName = "John" }
        };
        _mockPlanningService.Setup(s => s.GetPlannedRoutesAsync(It.IsAny<DateTime>()))
            .ReturnsAsync(savedRoutes);

        // Act
        var result = await _controller.Index();

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeOfType<RoutePlanningViewModel>().Subject;
        model.Routes.Should().HaveCount(1);
    }

    [Fact]
    public async Task Index_CallsPlanRouteAsync_WhenNoSavedRoutesExist()
    {
        // Arrange
        _mockPlanningService.Setup(s => s.GetPlannedRoutesAsync(It.IsAny<DateTime>()))
            .ReturnsAsync(new List<SavedRouteStopDto>()); 
        
        _mockPlanningService.Setup(s => s.PlanRouteAsync())
            .ReturnsAsync(new List<RoutePlanDto> {
                new RoutePlanDto { AssignedCO = 1, BinId = 2, StopNumber = 1 }
            });

        // Act
        var result = await _controller.Index();

        // Assert
        _mockPlanningService.Verify(s => s.PlanRouteAsync(), Times.Once);
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeOfType<RoutePlanningViewModel>().Subject;
        model.Routes.First().RouteKey.Should().Be(1);
    }

    [Fact]
    public async Task AssignAllRoutes_RedirectsToIndex_OnSuccess()
    {
        // Arrange
        var request = new AssignAllRoutesRequestDto {
            Assignments = new List<RouteAssignmentDto> {
                new RouteAssignmentDto { RouteKey = 1, OfficerUsername = "officer1" }
            }
        };

        _mockPlanningService.Setup(s => s.GetPlannedRoutesAsync(It.IsAny<DateTime>()))
            .ReturnsAsync(new List<SavedRouteStopDto>());

        _mockPlanningService.Setup(s => s.PlanRouteAsync())
            .ReturnsAsync(new List<RoutePlanDto>());

        // Act
        var result = await _controller.AssignAllRoutes(request);

        // Assert
        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.ActionName.Should().Be(nameof(_controller.Index));
        
        _mockAssignmentService.Verify(s => s.SavePlannedRoutesAsync(
            It.IsAny<List<UiRouteStopDto>>(),
            It.IsAny<Dictionary<int, string>>(),
            "test-admin",
            It.IsAny<DateTime>()), Times.Once);
    }

    [Fact]
    public async Task Index_FiltersOnlyCollectionOfficers_AndHandlesNullCoords()
    {
        // Arrange
        var admin = new Employee { Username = "admin_user", FullName = "Admin", RoleId = 1 };
        _dbContext.Employees.Add(admin);
        await _dbContext.SaveChangesAsync();

        _mockPlanningService.Setup(s => s.GetPlannedRoutesAsync(It.IsAny<DateTime>()))
            .ReturnsAsync(new List<SavedRouteStopDto>());
    
        _mockPlanningService.Setup(s => s.PlanRouteAsync())
            .ReturnsAsync(new List<RoutePlanDto> {
                new RoutePlanDto { AssignedCO = 1, Latitude = null, Longitude = null, StopNumber = 1 } 
            });

        // Act
        var result = await _controller.Index();

        // Assert
        var model = (result as ViewResult)?.Model as RoutePlanningViewModel;
        model!.AvailableOfficers.Should().NotContain(e => e.Username == "admin_user");
        model.AllStops.First().Latitude.Should().Be(0);
    }

    [Fact]
    public async Task AssignAllRoutes_SetsSuccessMessageAndCallsServiceCorrectly()
    {
        // Arrange
        var request = new AssignAllRoutesRequestDto {
            Assignments = new List<RouteAssignmentDto> {
                new RouteAssignmentDto { RouteKey = 1, OfficerUsername = "officer1" }
            }
        };

        var mockStops = new List<RoutePlanDto> {
            new RoutePlanDto { AssignedCO = 1, BinId = 101, StopNumber = 1, AssignedOfficerName = "officer1" }
        };

        _mockPlanningService.Setup(s => s.GetPlannedRoutesAsync(It.IsAny<DateTime>()))
            .ReturnsAsync(new List<SavedRouteStopDto>()); 
        _mockPlanningService.Setup(s => s.PlanRouteAsync())
            .ReturnsAsync(mockStops);

        // Act
        var result = await _controller.AssignAllRoutes(request);

        // Assert
        _mockAssignmentService.Verify(s => s.SavePlannedRoutesAsync(
            It.IsAny<List<UiRouteStopDto>>(),
            It.Is<Dictionary<int, string>>(dict => dict[1] == "officer1"),
            "test-admin",
            It.IsAny<DateTime>()), Times.Once);

        _controller.TempData["SuccessMessage"]!.ToString().Should().Contain("1 route");
    }
}