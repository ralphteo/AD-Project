using ADWebApplication.Controllers;
using ADWebApplication.Data.Repository;
using ADWebApplication.Models;
using ADWebApplication.Models.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ADWebApplication.Tests
{
    public class AdminBinCrudControllerTests
    {
        private static AdminBinCrudController CreateController(Mock<IAdminRepository> mockRepository)
        {
            var controller = new AdminBinCrudController(mockRepository.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };

            controller.TempData = new TempDataDictionary(
                controller.HttpContext,
                Mock.Of<ITempDataProvider>());

            return controller;
        }

        [Fact]
        public async Task Bins_ReturnsViewWithBinsAndRegions()
        {
            // Arrange
            var mockRepository = new Mock<IAdminRepository>();
            var bins = new List<CollectionBin> { new CollectionBin { BinId = 1 } };
            var regions = new List<Region> { new Region { RegionId = 1, RegionName = "North" } };

            mockRepository.Setup(r => r.GetAllBinsAsync()).ReturnsAsync(bins);
            mockRepository.Setup(r => r.GetAllRegionsAsync()).ReturnsAsync(regions);

            var controller = CreateController(mockRepository);

            // Act
            var result = await controller.Bins();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("~/Views/Admin/Bins.cshtml", viewResult.ViewName);
            Assert.Same(bins, viewResult.Model);
            Assert.Same(regions, controller.ViewBag.Regions);
        }

        [Fact]
        public async Task EditBin_ReturnsView_WhenModelStateInvalid()
        {
            // Arrange
            var mockRepository = new Mock<IAdminRepository>();
            var controller = CreateController(mockRepository);
            var editedBin = new CollectionBin { BinId = 1 };

            controller.ModelState.AddModelError("BinId", "Invalid");

            // Act
            var result = await controller.EditBin(editedBin);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Same(editedBin, viewResult.Model);
            mockRepository.Verify(r => r.GetBinByIdAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task EditBin_ReturnsNotFound_WhenBinDoesNotExist()
        {
            // Arrange
            var mockRepository = new Mock<IAdminRepository>();
            mockRepository.Setup(r => r.GetBinByIdAsync(1)).ReturnsAsync((CollectionBin?)null);

            var controller = CreateController(mockRepository);
            var editedBin = new CollectionBin { BinId = 1 };

            // Act
            var result = await controller.EditBin(editedBin);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            mockRepository.Verify(r => r.UpdateBinAsync(It.IsAny<CollectionBin>()), Times.Never);
        }

        [Fact]
        public async Task EditBin_UpdatesBinAndRedirects_WhenValid()
        {
            // Arrange
            var mockRepository = new Mock<IAdminRepository>();
            var existingBin = new CollectionBin
            {
                BinId = 5,
                RegionId = 1,
                LocationName = "Old",
                LocationAddress = "Old Address",
                BinCapacity = 10,
                BinStatus = "Active",
                Latitude = 1.2,
                Longitude = 3.4
            };
            var editedBin = new CollectionBin
            {
                BinId = 5,
                RegionId = 2,
                LocationName = "New",
                LocationAddress = "New Address",
                BinCapacity = 20,
                BinStatus = "Inactive",
                Latitude = 5.6,
                Longitude = 7.8
            };

            mockRepository.Setup(r => r.GetBinByIdAsync(5)).ReturnsAsync(existingBin);
            mockRepository.Setup(r => r.UpdateBinAsync(It.IsAny<CollectionBin>())).Returns(Task.CompletedTask);

            var controller = CreateController(mockRepository);

            // Act
            var result = await controller.EditBin(editedBin);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Bins", redirect.ActionName);

            mockRepository.Verify(r => r.UpdateBinAsync(It.Is<CollectionBin>(bin =>
                bin.BinId == 5 &&
                bin.RegionId == 2 &&
                bin.LocationName == "New" &&
                bin.LocationAddress == "New Address" &&
                bin.BinCapacity == 20 &&
                bin.BinStatus == "Inactive" &&
                bin.Latitude == 5.6 &&
                bin.Longitude == 7.8)), Times.Once);

            Assert.Equal("Bin 'New Address' has been updated successfully.", controller.TempData["SuccessMessage"]);
        }

        [Fact]
        public async Task DeleteBin_ReturnsNotFound_WhenBinDoesNotExist()
        {
            // Arrange
            var mockRepository = new Mock<IAdminRepository>();
            mockRepository.Setup(r => r.GetBinByIdAsync(1)).ReturnsAsync((CollectionBin?)null);

            var controller = CreateController(mockRepository);

            // Act
            var result = await controller.DeleteBin(1);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            mockRepository.Verify(r => r.DeleteBinAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task DeleteBin_RedirectsAndSetsTempData_WhenSuccessful()
        {
            // Arrange
            var mockRepository = new Mock<IAdminRepository>();
            var bin = new CollectionBin { BinId = 2, LocationAddress = "Street 1" };

            mockRepository.Setup(r => r.GetBinByIdAsync(2)).ReturnsAsync(bin);
            mockRepository.Setup(r => r.DeleteBinAsync(2)).Returns(Task.CompletedTask);

            var controller = CreateController(mockRepository);

            // Act
            var result = await controller.DeleteBin(2);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("BinDeleted", redirect.ActionName);
            Assert.Equal(2, controller.TempData["DeletedBinId"]);
            Assert.Equal("Street 1", controller.TempData["DeletedBinLocation"]);
        }

        [Fact]
        public void BinDeleted_ReturnsView()
        {
            // Arrange
            var mockRepository = new Mock<IAdminRepository>();
            var controller = CreateController(mockRepository);

            // Act
            var result = controller.BinDeleted();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("~/Views/Admin/BinDeleted.cshtml", viewResult.ViewName);
        }

        [Fact]
        public async Task CreateBin_ReturnsView_WhenModelStateInvalid()
        {
            // Arrange
            var mockRepository = new Mock<IAdminRepository>();
            var regions = new List<Region> { new Region { RegionId = 3, RegionName = "West" } };

            mockRepository.Setup(r => r.GetAllRegionsAsync()).ReturnsAsync(regions);

            var controller = CreateController(mockRepository);
            var newBin = new CollectionBin { LocationAddress = "Invalid" };

            controller.ModelState.AddModelError("LocationAddress", "Invalid");

            // Act
            var result = await controller.CreateBin(newBin);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Same(newBin, viewResult.Model);
            Assert.Same(regions, controller.ViewBag.Regions);
            mockRepository.Verify(r => r.CreateBinAsync(It.IsAny<CollectionBin>()), Times.Never);
        }

        [Fact]
        public async Task CreateBin_CreatesAndRedirects_WhenValid()
        {
            // Arrange
            var mockRepository = new Mock<IAdminRepository>();
            var newBin = new CollectionBin { LocationAddress = "Street 2" };

            mockRepository.Setup(r => r.CreateBinAsync(newBin)).Returns(Task.CompletedTask);

            var controller = CreateController(mockRepository);

            // Act
            var result = await controller.CreateBin(newBin);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Bins", redirect.ActionName);
            Assert.Equal("Bin 'Street 2' has been created successfully.", controller.TempData["SuccessMessage"]);

            mockRepository.Verify(r => r.CreateBinAsync(newBin), Times.Once);
        }

        [Fact]
        public async Task CollectionOfficerSchedule_ReturnsViewWithAssignments()
        {
            // Arrange
            var mockRepository = new Mock<IAdminRepository>();
            var assignments = new List<RouteAssignment> { new RouteAssignment { AssignmentId = 1 } };
            var officer = new Employee { Username = "CO-1", FullName = "Officer One" };

            mockRepository
                .Setup(r => r.GetRouteAssignmentsForOfficerAsync("CO-1", It.IsAny<DateTime>()))
                .ReturnsAsync(assignments);
            mockRepository
                .Setup(r => r.GetEmployeeByUsernameAsync("CO-1"))
                .ReturnsAsync(officer);

            var controller = CreateController(mockRepository);

            // Act
            var result = await controller.CollectionOfficerSchedule("CO-1");

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("~/Views/Admin/CollectionOfficerSchedule.cshtml", viewResult.ViewName);
            Assert.Same(assignments, viewResult.Model);
            Assert.Equal("Officer One", controller.ViewBag.OfficerFullName);
        }

        [Fact]
        public async Task CollectionOfficerRoster_ReturnsAvailableOfficers_WhenDatesProvided()
        {
            // Arrange
            var mockRepository = new Mock<IAdminRepository>();
            var officers = new List<Employee> { new Employee { Username = "CO-2" } };

            mockRepository
                .Setup(r => r.GetAvailableCollectionOfficersAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(officers);

            var controller = CreateController(mockRepository);

            // Act
            var result = await controller.CollectionOfficerRoster(DateTime.Today.AddDays(-1), DateTime.Today);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("~/Views/Admin/CollectionOfficerRoster.cshtml", viewResult.ViewName);
            Assert.Same(officers, viewResult.Model);
            mockRepository.Verify(r => r.GetAllCollectionOfficersAsync(), Times.Never);
        }

        [Fact]
        public async Task CollectionOfficerRoster_ReturnsAllOfficers_WhenNoDatesProvided()
        {
            // Arrange
            var mockRepository = new Mock<IAdminRepository>();
            var officers = new List<Employee> { new Employee { Username = "CO-3" } };

            mockRepository
                .Setup(r => r.GetAllCollectionOfficersAsync())
                .ReturnsAsync(officers);

            var controller = CreateController(mockRepository);

            // Act
            var result = await controller.CollectionOfficerRoster(null, null);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("~/Views/Admin/CollectionOfficerRoster.cshtml", viewResult.ViewName);
            Assert.Same(officers, viewResult.Model);
        }

        [Fact]
        public async Task CollectionCalendar_ReturnsView()
        {
            // Arrange
            var mockRepository = new Mock<IAdminRepository>();
            var controller = CreateController(mockRepository);

            // Act
            var result = await controller.CollectionCalendar("CO-4");

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("~/Views/Admin/CollectionCalendar.cshtml", viewResult.ViewName);
        }

        [Fact]
        public async Task GetOfficerAvailability_ReturnsJsonResult()
        {
            // Arrange
            var mockRepository = new Mock<IAdminRepository>();
            var available = new List<CollectionOfficerDto>
            {
                new CollectionOfficerDto { Username = "CO-5", FullName = "Officer Five" }
            };
            var assigned = new List<AssignedCollectionOfficerDto>
            {
                new AssignedCollectionOfficerDto { Username = "CO-6", FullName = "Officer Six" }
            };

            mockRepository
                .Setup(r => r.GetAvailableCollectionOfficersCalendarAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(available);
            mockRepository
                .Setup(r => r.GetAssignedCollectionOfficersCalendarAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(assigned);

            var controller = CreateController(mockRepository);

            // Act
            var result = await controller.GetOfficerAvailability(DateTime.Today.AddDays(-7), DateTime.Today);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            var value = jsonResult.Value;
            var availableProperty = value!.GetType().GetProperty("available");
            var assignedProperty = value.GetType().GetProperty("assigned");

            var availableValue = availableProperty!.GetValue(value) as List<CollectionOfficerDto>;
            var assignedValue = assignedProperty!.GetValue(value) as List<AssignedCollectionOfficerDto>;

            Assert.Same(available, availableValue);
            Assert.Same(assigned, assignedValue);
        }
    }
}
