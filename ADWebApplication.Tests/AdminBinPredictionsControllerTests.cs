using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using ADWebApplication.Services;
using ADWebApplication.Models.ViewModels.BinPredictions;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Claims;

namespace ADWebApplication.Tests
{
    public class AdminBinPredictionsControllerTests
    {
        private Mock<IBinPredictionService> CreateMockBinPredictionService()
        {
            return new Mock<IBinPredictionService>();
        }

        private AdminBinPredictionsController CreateController(Mock<IBinPredictionService> mockService)
        {
            var controller = new AdminBinPredictionsController(mockService.Object);

            // Setup HttpContext for TempData
            var httpContext = new DefaultHttpContext();
            var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
            controller.TempData = tempData;

            // Setup User claims for authorization
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "admin@test.com"),
                new Claim(ClaimTypes.Role, "Admin")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            return controller;
        }

        #region Index Action Tests

        [Fact]
        public async Task Index_ReturnsViewResult_WithViewModel()
        {
            // Arrange
            var mockService = CreateMockBinPredictionService();
            var expectedViewModel = new BinPredictionsPageViewModel
            {
                Rows = new List<BinPredictionsTableViewModel>(),
                TotalBins = 10,
                CurrentPage = 1,
                TotalPages = 1
            };

            mockService.Setup(s => s.BuildBinPredictionsPageAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()
            )).ReturnsAsync(expectedViewModel);

            var controller = CreateController(mockService);

            // Act
            var result = await controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<BinPredictionsPageViewModel>(viewResult.Model);
            Assert.Equal(expectedViewModel, model);
            Assert.Equal(10, model.TotalBins);
        }

        [Fact]
        public async Task Index_CallsService_WithDefaultParameters()
        {
            // Arrange
            var mockService = CreateMockBinPredictionService();
            var expectedViewModel = new BinPredictionsPageViewModel();

            mockService.Setup(s => s.BuildBinPredictionsPageAsync(1, "EstimatedFill", "desc", "All", "All"))
                .ReturnsAsync(expectedViewModel)
                .Verifiable();

            var controller = CreateController(mockService);

            // Act
            await controller.Index();

            // Assert
            mockService.Verify(s => s.BuildBinPredictionsPageAsync(1, "EstimatedFill", "desc", "All", "All"), Times.Once);
        }

        [Fact]
        public async Task Index_CallsService_WithCustomParameters()
        {
            // Arrange
            var mockService = CreateMockBinPredictionService();
            var expectedViewModel = new BinPredictionsPageViewModel();

            mockService.Setup(s => s.BuildBinPredictionsPageAsync(2, "DaysToThreshold", "asc", "High", "3"))
                .ReturnsAsync(expectedViewModel)
                .Verifiable();

            var controller = CreateController(mockService);

            // Act
            await controller.Index(page: 2, sort: "DaysToThreshold", sortDir: "asc", risk: "High", timeframe: "3");

            // Assert
            mockService.Verify(s => s.BuildBinPredictionsPageAsync(2, "DaysToThreshold", "asc", "High", "3"), Times.Once);
        }

        [Fact]
        public async Task Index_HandlesPageParameter_Correctly()
        {
            // Arrange
            var mockService = CreateMockBinPredictionService();
            var expectedViewModel = new BinPredictionsPageViewModel
            {
                CurrentPage = 5,
                TotalPages = 10
            };

            mockService.Setup(s => s.BuildBinPredictionsPageAsync(5, "EstimatedFill", "desc", "All", "All"))
                .ReturnsAsync(expectedViewModel);

            var controller = CreateController(mockService);

            // Act
            var result = await controller.Index(page: 5);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<BinPredictionsPageViewModel>(viewResult.Model);
            Assert.Equal(5, model.CurrentPage);
        }

        [Fact]
        public async Task Index_HandlesSortParameter_Correctly()
        {
            // Arrange
            var mockService = CreateMockBinPredictionService();
            var expectedViewModel = new BinPredictionsPageViewModel
            {
                SortBy = "AvgGrowth",
                SortDir = "asc"
            };

            mockService.Setup(s => s.BuildBinPredictionsPageAsync(1, "AvgGrowth", "asc", "All", "All"))
                .ReturnsAsync(expectedViewModel);

            var controller = CreateController(mockService);

            // Act
            var result = await controller.Index(sort: "AvgGrowth", sortDir: "asc");

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<BinPredictionsPageViewModel>(viewResult.Model);
            Assert.Equal("AvgGrowth", model.SortBy);
            Assert.Equal("asc", model.SortDir);
        }

        [Fact]
        public async Task Index_HandlesRiskFilter_Correctly()
        {
            // Arrange
            var mockService = CreateMockBinPredictionService();
            var expectedViewModel = new BinPredictionsPageViewModel
            {
                SelectedRisk = "High",
                Rows = new List<BinPredictionsTableViewModel>
                {
                    new BinPredictionsTableViewModel { RiskLevel = "High" }
                }
            };

            mockService.Setup(s => s.BuildBinPredictionsPageAsync(1, "EstimatedFill", "desc", "High", "All"))
                .ReturnsAsync(expectedViewModel);

            var controller = CreateController(mockService);

            // Act
            var result = await controller.Index(risk: "High");

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<BinPredictionsPageViewModel>(viewResult.Model);
            Assert.Equal("High", model.SelectedRisk);
        }

        [Fact]
        public async Task Index_HandlesTimeframeFilter_Correctly()
        {
            // Arrange
            var mockService = CreateMockBinPredictionService();
            var expectedViewModel = new BinPredictionsPageViewModel
            {
                SelectedTimeframe = "3"
            };

            mockService.Setup(s => s.BuildBinPredictionsPageAsync(1, "EstimatedFill", "desc", "All", "3"))
                .ReturnsAsync(expectedViewModel);

            var controller = CreateController(mockService);

            // Act
            var result = await controller.Index(timeframe: "3");

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<BinPredictionsPageViewModel>(viewResult.Model);
            Assert.Equal("3", model.SelectedTimeframe);
        }

        [Fact]
        public async Task Index_HandlesMediumRiskFilter_Correctly()
        {
            // Arrange
            var mockService = CreateMockBinPredictionService();
            var expectedViewModel = new BinPredictionsPageViewModel
            {
                SelectedRisk = "Medium"
            };

            mockService.Setup(s => s.BuildBinPredictionsPageAsync(1, "EstimatedFill", "desc", "Medium", "All"))
                .ReturnsAsync(expectedViewModel);

            var controller = CreateController(mockService);

            // Act
            var result = await controller.Index(risk: "Medium");

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<BinPredictionsPageViewModel>(viewResult.Model);
            Assert.Equal("Medium", model.SelectedRisk);
        }

        [Fact]
        public async Task Index_Handles7DayTimeframe_Correctly()
        {
            // Arrange
            var mockService = CreateMockBinPredictionService();
            var expectedViewModel = new BinPredictionsPageViewModel
            {
                SelectedTimeframe = "7"
            };

            mockService.Setup(s => s.BuildBinPredictionsPageAsync(1, "EstimatedFill", "desc", "All", "7"))
                .ReturnsAsync(expectedViewModel);

            var controller = CreateController(mockService);

            // Act
            var result = await controller.Index(timeframe: "7");

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<BinPredictionsPageViewModel>(viewResult.Model);
            Assert.Equal("7", model.SelectedTimeframe);
        }

        #endregion

        #region Refresh Action Tests

        [Fact]
        public async Task Refresh_CallsRefreshService_AndRedirects()
        {
            // Arrange
            var mockService = CreateMockBinPredictionService();
            mockService.Setup(s => s.RefreshPredictionsForNewCyclesAsync())
                .ReturnsAsync(5)
                .Verifiable();

            var controller = CreateController(mockService);

            // Act
            var result = await controller.Refresh();

            // Assert
            mockService.Verify(s => s.RefreshPredictionsForNewCyclesAsync(), Times.Once);
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(AdminBinPredictionsController.Index), redirectResult.ActionName);
        }

        [Fact]
        public async Task Refresh_SetsTempData_WithSuccessMessage()
        {
            // Arrange
            var mockService = CreateMockBinPredictionService();
            mockService.Setup(s => s.RefreshPredictionsForNewCyclesAsync())
                .ReturnsAsync(3);

            var controller = CreateController(mockService);

            // Act
            var result = await controller.Refresh();

            // Assert
            Assert.True(controller.TempData.ContainsKey("PredictionRefreshSuccess"));
            Assert.Equal("3 bin prediction(s) refreshed successfully.", controller.TempData["PredictionRefreshSuccess"]);
        }

        [Fact]
        public async Task Refresh_HandlesZeroRefreshes_Correctly()
        {
            // Arrange
            var mockService = CreateMockBinPredictionService();
            mockService.Setup(s => s.RefreshPredictionsForNewCyclesAsync())
                .ReturnsAsync(0);

            var controller = CreateController(mockService);

            // Act
            var result = await controller.Refresh();

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(AdminBinPredictionsController.Index), redirectResult.ActionName);
            Assert.Equal("0 bin prediction(s) refreshed successfully.", controller.TempData["PredictionRefreshSuccess"]);
        }

        [Fact]
        public async Task Refresh_HandlesMultipleRefreshes_Correctly()
        {
            // Arrange
            var mockService = CreateMockBinPredictionService();
            mockService.Setup(s => s.RefreshPredictionsForNewCyclesAsync())
                .ReturnsAsync(15);

            var controller = CreateController(mockService);

            // Act
            var result = await controller.Refresh();

            // Assert
            Assert.Equal("15 bin prediction(s) refreshed successfully.", controller.TempData["PredictionRefreshSuccess"]);
        }

        [Fact]
        public async Task Refresh_RedirectsToIndex_WithNoRouteValues()
        {
            // Arrange
            var mockService = CreateMockBinPredictionService();
            mockService.Setup(s => s.RefreshPredictionsForNewCyclesAsync())
                .ReturnsAsync(5);

            var controller = CreateController(mockService);

            // Act
            var result = await controller.Refresh();

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(AdminBinPredictionsController.Index), redirectResult.ActionName);
            Assert.Null(redirectResult.ControllerName); // Should redirect to same controller
        }

        #endregion

        #region Authorization Tests

        [Fact]
        public void Controller_HasAuthorizeAttribute_WithAdminRole()
        {
            // Arrange & Act
            var type = typeof(AdminBinPredictionsController);
            var attributes = type.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), true);

            // Assert
            Assert.NotEmpty(attributes);
            var authorizeAttribute = attributes[0] as Microsoft.AspNetCore.Authorization.AuthorizeAttribute;
            Assert.NotNull(authorizeAttribute);
            Assert.Equal("Admin", authorizeAttribute!.Roles);
        }

        [Fact]
        public void Controller_HasRouteAttribute_WithCorrectPath()
        {
            // Arrange & Act
            var type = typeof(AdminBinPredictionsController);
            var attributes = type.GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.RouteAttribute), true);

            // Assert
            Assert.NotEmpty(attributes);
            var routeAttribute = attributes[0] as Microsoft.AspNetCore.Mvc.RouteAttribute;
            Assert.NotNull(routeAttribute);
            Assert.Equal("Admin/BinPredictions", routeAttribute!.Template);
        }

        [Fact]
        public void Index_HasHttpGetAttribute()
        {
            // Arrange
            var type = typeof(AdminBinPredictionsController);
            var method = type.GetMethod("Index");

            // Act
            var attributes = method!.GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.HttpGetAttribute), true);

            // Assert
            Assert.NotEmpty(attributes);
        }

        [Fact]
        public void Refresh_HasHttpPostAttribute()
        {
            // Arrange
            var type = typeof(AdminBinPredictionsController);
            var method = type.GetMethod("Refresh");

            // Act
            var attributes = method!.GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.HttpPostAttribute), true);

            // Assert
            Assert.NotEmpty(attributes);
        }

        [Fact]
        public void Refresh_HasValidateAntiForgeryTokenAttribute()
        {
            // Arrange
            var type = typeof(AdminBinPredictionsController);
            var method = type.GetMethod("Refresh");

            // Act
            var attributes = method!.GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.ValidateAntiForgeryTokenAttribute), true);

            // Assert
            Assert.NotEmpty(attributes);
        }

        #endregion

        #region Edge Cases and Error Handling

        [Fact]
        public async Task Index_HandlesNullViewModel_Gracefully()
        {
            // Arrange
            var mockService = CreateMockBinPredictionService();
            mockService.Setup(s => s.BuildBinPredictionsPageAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()
            )).ReturnsAsync((BinPredictionsPageViewModel)null!);

            var controller = CreateController(mockService);

            // Act
            var result = await controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Null(viewResult.Model);
        }

        [Fact]
        public async Task Index_WithNegativePage_CallsServiceWithValue()
        {
            // Arrange
            var mockService = CreateMockBinPredictionService();
            var expectedViewModel = new BinPredictionsPageViewModel();

            mockService.Setup(s => s.BuildBinPredictionsPageAsync(-1, "EstimatedFill", "desc", "All", "All"))
                .ReturnsAsync(expectedViewModel);

            var controller = CreateController(mockService);

            // Act
            await controller.Index(page: -1);

            // Assert
            mockService.Verify(s => s.BuildBinPredictionsPageAsync(-1, "EstimatedFill", "desc", "All", "All"), Times.Once);
        }

        [Fact]
        public async Task Index_WithLargePage_CallsServiceWithValue()
        {
            // Arrange
            var mockService = CreateMockBinPredictionService();
            var expectedViewModel = new BinPredictionsPageViewModel
            {
                CurrentPage = 100,
                TotalPages = 10
            };

            mockService.Setup(s => s.BuildBinPredictionsPageAsync(100, "EstimatedFill", "desc", "All", "All"))
                .ReturnsAsync(expectedViewModel);

            var controller = CreateController(mockService);

            // Act
            await controller.Index(page: 100);

            // Assert
            mockService.Verify(s => s.BuildBinPredictionsPageAsync(100, "EstimatedFill", "desc", "All", "All"), Times.Once);
        }

        [Fact]
        public async Task Index_WithEmptyStringFilters_PassesToService()
        {
            // Arrange
            var mockService = CreateMockBinPredictionService();
            var expectedViewModel = new BinPredictionsPageViewModel();

            mockService.Setup(s => s.BuildBinPredictionsPageAsync(1, "EstimatedFill", "desc", "", ""))
                .ReturnsAsync(expectedViewModel);

            var controller = CreateController(mockService);

            // Act
            await controller.Index(risk: "", timeframe: "");

            // Assert
            mockService.Verify(s => s.BuildBinPredictionsPageAsync(1, "EstimatedFill", "desc", "", ""), Times.Once);
        }

        #endregion
    }
}
