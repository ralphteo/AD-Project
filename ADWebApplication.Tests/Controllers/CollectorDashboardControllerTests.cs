using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using ADWebApplication.Controllers;
using ADWebApplication.Services.Collector;
using ADWebApplication.Models;
using ADWebApplication.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Linq;

namespace ADWebApplication.Tests.Controllers
{
    public class CollectorDashboardControllerTests
    {
        private static Mock<ICollectorService> CreateMockCollectorService()
        {
            return new Mock<ICollectorService>();
        }

        private static Mock<IConfiguration> CreateMockConfiguration()
        {
            var mockConfig = new Mock<IConfiguration>();
            mockConfig.Setup(c => c["GOOGLE_MAPS_API_KEY"]).Returns("test-api-key");
            return mockConfig;
        }

        private static CollectorDashboardController CreateController(
            Mock<ICollectorService> mockService,
            Mock<IConfiguration> mockConfig,
            string? username = "testcollector")
        {
            var controller = new CollectorDashboardController(mockService.Object, mockConfig.Object);

            // Setup HttpContext for TempData
            var httpContext = new DefaultHttpContext();
            var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
            controller.TempData = tempData;

            // Setup User claims for authorization
            if (username != null)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, username),
                    new Claim(ClaimTypes.Role, "Collector")
                };
                var identity = new ClaimsIdentity(claims, "TestAuthType");
                var claimsPrincipal = new ClaimsPrincipal(identity);
                controller.ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = claimsPrincipal }
                };
            }
            else
            {
                controller.ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
                };
            }

            return controller;
        }

        #region Index Action Tests

        [Fact]
        public async Task Index_ReturnsViewResult_WithRoute()
        {
            // Arrange
            var mockService = CreateMockCollectorService();
            var mockConfig = CreateMockConfiguration();
            var expectedRoute = new CollectorRoute
            {
                RouteId = "1",
                RouteName = "Route #1",
                Zone = "North",
                Status = "In Progress",
                CollectionPoints = new List<CollectionPoint>()
            };

            mockService.Setup(s => s.GetDailyRouteAsync("testcollector"))
                .ReturnsAsync(expectedRoute);

            var controller = CreateController(mockService, mockConfig);

            // Act
            var result = await controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<CollectorRoute>(viewResult.Model);
            Assert.Equal("Route #1", model.RouteName);
            Assert.Equal("North", model.Zone);
            Assert.Equal("In Progress", model.Status);
        }

        [Fact]
        public async Task Index_SetsGoogleMapsApiKey_InViewBag()
        {
            // Arrange
            var mockService = CreateMockCollectorService();
            var mockConfig = CreateMockConfiguration();
            mockService.Setup(s => s.GetDailyRouteAsync(It.IsAny<string>()))
                .ReturnsAsync(new CollectorRoute());

            var controller = CreateController(mockService, mockConfig);

            // Act
            var result = await controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("test-api-key", controller.ViewBag.GoogleMapsKey);
        }

        [Fact]
        public async Task Index_ReturnsUnauthorized_WhenUsernameIsNull()
        {
            // Arrange
            var mockService = CreateMockCollectorService();
            var mockConfig = CreateMockConfiguration();
            var controller = CreateController(mockService, mockConfig, null!);

            // Override with null username
            controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

            // Act
            var result = await controller.Index();

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        #endregion

        #region ConfirmCollection GET Tests

        [Fact]
        public async Task ConfirmCollectionGet_ReturnsView_WithViewModel()
        {
            // Arrange
            var mockService = CreateMockCollectorService();
            var mockConfig = CreateMockConfiguration();
            var expectedVM = new CollectionConfirmationVM
            {
                StopId = 1,
                PointId = "B001",
                LocationName = "Test Location",
                BinFillLevel = 75
            };

            mockService.Setup(s => s.GetCollectionConfirmationAsync(1, "testcollector"))
                .ReturnsAsync(expectedVM);

            var controller = CreateController(mockService, mockConfig);

            // Act
            var result = await controller.ConfirmCollection(1);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<CollectionConfirmationVM>(viewResult.Model);
            Assert.Equal("B001", model.PointId);
            Assert.Equal(75, model.BinFillLevel);
        }

        [Fact]
        public async Task ConfirmCollectionGet_RedirectsToReportIssue_WhenViewModelIsNull()
        {
            // Arrange
            var mockService = CreateMockCollectorService();
            var mockConfig = CreateMockConfiguration();
            
            mockService.Setup(s => s.GetCollectionConfirmationAsync(99, "testcollector"))
                .ReturnsAsync((CollectionConfirmationVM?)null);

            var controller = CreateController(mockService, mockConfig);

            // Act
            var result = await controller.ConfirmCollection(99);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("ReportIssue", redirectResult.ActionName);
            Assert.Equal("Issue not found for this route.", controller.TempData["ErrorMessage"]);
        }

        [Fact]
        public async Task ConfirmCollectionGet_ReturnsUnauthorized_WhenUsernameIsNull()
        {
            // Arrange
            var mockService = CreateMockCollectorService();
            var mockConfig = CreateMockConfiguration();
            var controller = CreateController(mockService, mockConfig, null!);
            controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

            // Act
            var result = await controller.ConfirmCollection(1);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        #endregion

        #region ConfirmCollection POST Tests

        [Fact]
        public async Task ConfirmCollectionPost_ReturnsJson_WhenAjaxRequestAndValid()
        {
            // Arrange
            var mockService = CreateMockCollectorService();
            var mockConfig = CreateMockConfiguration();
            var model = new CollectionConfirmationVM
            {
                StopId = 1,
                BinFillLevel = 80,
                CollectionTime = DateTime.Now
            };

            mockService.Setup(s => s.ConfirmCollectionAsync(model, "testcollector"))
                .ReturnsAsync(true);

            var controller = CreateController(mockService, mockConfig);
            controller.Request.Headers[HeaderNames.XRequestedWith] = "XMLHttpRequest";

            // Act
            var result = await controller.ConfirmCollection(model);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            var value = jsonResult.Value;
            Assert.NotNull(value);
            var successProp = value.GetType().GetProperty("success");
            var stopIdProp = value.GetType().GetProperty("stopId");
            Assert.NotNull(successProp);
            Assert.NotNull(stopIdProp);
            Assert.True((bool)successProp.GetValue(value)!);
            Assert.Equal(1, (int)stopIdProp.GetValue(value)!);
        }

        [Fact]
        public async Task ConfirmCollectionPost_ReturnsView_WhenNotAjaxAndValid()
        {
            // Arrange
            var mockService = CreateMockCollectorService();
            var mockConfig = CreateMockConfiguration();
            var model = new CollectionConfirmationVM
            {
                StopId = 1,
                BinFillLevel = 80,
                CollectionTime = DateTime.Now
            };

            mockService.Setup(s => s.ConfirmCollectionAsync(model, "testcollector"))
                .ReturnsAsync(true);

            var controller = CreateController(mockService, mockConfig);

            // Act
            var result = await controller.ConfirmCollection(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("CollectionConfirmed", viewResult.ViewName);
        }

        [Fact]
        public async Task ConfirmCollectionPost_ReturnsBadRequest_WhenAjaxAndInvalid()
        {
            // Arrange
            var mockService = CreateMockCollectorService();
            var mockConfig = CreateMockConfiguration();
            var model = new CollectionConfirmationVM();

            var controller = CreateController(mockService, mockConfig);
            controller.ModelState.AddModelError("BinFillLevel", "Required");
            controller.Request.Headers[HeaderNames.XRequestedWith] = "XMLHttpRequest";

            // Act
            var result = await controller.ConfirmCollection(model);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var value = badRequestResult.Value;
            Assert.NotNull(value);
            var successProp = value.GetType().GetProperty("success");
            var errorsProp = value.GetType().GetProperty("errors");
            Assert.NotNull(successProp);
            Assert.NotNull(errorsProp);
            Assert.False((bool)successProp.GetValue(value)!);
            Assert.NotNull(errorsProp.GetValue(value));
        }

        [Fact]
        public async Task ConfirmCollectionPost_ReturnsView_WhenNotAjaxAndInvalid()
        {
            // Arrange
            var mockService = CreateMockCollectorService();
            var mockConfig = CreateMockConfiguration();
            var model = new CollectionConfirmationVM();

            var controller = CreateController(mockService, mockConfig);
            controller.ModelState.AddModelError("BinFillLevel", "Required");

            // Act
            var result = await controller.ConfirmCollection(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("ConfirmCollection", viewResult.ViewName);
        }

        [Fact]
        public async Task ConfirmCollectionPost_ReturnsUnauthorized_WhenUsernameIsNull()
        {
            // Arrange
            var mockService = CreateMockCollectorService();
            var mockConfig = CreateMockConfiguration();
            var model = new CollectionConfirmationVM();
            var controller = CreateController(mockService, mockConfig, null!);
            controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

            // Act
            var result = await controller.ConfirmCollection(model);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        #endregion

        #region CollectionConfirmed Tests

        [Fact]
        public void CollectionConfirmed_ReturnsView_WithModel()
        {
            // Arrange
            var mockService = CreateMockCollectorService();
            var mockConfig = CreateMockConfiguration();
            var model = new CollectionConfirmationVM { StopId = 1 };
            var controller = CreateController(mockService, mockConfig);

            // Act
            var result = controller.CollectionConfirmed(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(model, viewResult.Model);
        }

        #endregion

        #region ReportIssue GET Tests

        [Fact]
        public async Task ReportIssueGet_ReturnsView_WithViewModel()
        {
            // Arrange
            var mockService = CreateMockCollectorService();
            var mockConfig = CreateMockConfiguration();
            var expectedVM = new ReportIssueVM
            {
                TotalIssues = 10,
                OpenIssues = 5,
                InProgressIssues = 3,
                ResolvedIssues = 2
            };

            mockService.Setup(s => s.GetReportIssueViewModelAsync("testcollector", null, null, null))
                .ReturnsAsync(expectedVM);

            var controller = CreateController(mockService, mockConfig);

            // Act
            var result = await controller.ReportIssue(null, null, null);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ReportIssueVM>(viewResult.Model);
            Assert.Equal(10, model.TotalIssues);
            Assert.Equal(5, model.OpenIssues);
        }

        [Fact]
        public async Task ReportIssueGet_PassesFilters_ToService()
        {
            // Arrange
            var mockService = CreateMockCollectorService();
            var mockConfig = CreateMockConfiguration();
            
            mockService.Setup(s => s.GetReportIssueViewModelAsync("testcollector", "search", "Open", "High"))
                .ReturnsAsync(new ReportIssueVM())
                .Verifiable();

            var controller = CreateController(mockService, mockConfig);

            // Act
            await controller.ReportIssue("search", "Open", "High");

            // Assert
            mockService.Verify();
        }

        [Fact]
        public async Task ReportIssueGet_ReturnsUnauthorized_WhenUsernameIsNull()
        {
            // Arrange
            var mockService = CreateMockCollectorService();
            var mockConfig = CreateMockConfiguration();
            var controller = CreateController(mockService, mockConfig, null!);
            controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

            // Act
            var result = await controller.ReportIssue(null, null, null);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        #endregion

        #region ReportIssue POST Tests

        [Fact]
        public async Task ReportIssuePost_RedirectsToIndex_WhenValid()
        {
            // Arrange
            var mockService = CreateMockCollectorService();
            var mockConfig = CreateMockConfiguration();
            var model = new ReportIssueVM
            {
                BinId = 1,
                LocationName = "Test Location",
                IssueType = "Overflow",
                Description = "Test issue"
            };

            mockService.Setup(s => s.SubmitIssueAsync(model, "testcollector"))
                .ReturnsAsync(true);

            var controller = CreateController(mockService, mockConfig);

            // Act
            var result = await controller.ReportIssue(model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("ReportIssue", redirectResult.ActionName);
            Assert.Contains("Bin #1", controller.TempData["SuccessMessage"]!.ToString());
        }

        [Fact]
        public async Task ReportIssuePost_ReturnsView_WhenInvalid()
        {
            // Arrange
            var mockService = CreateMockCollectorService();
            var mockConfig = CreateMockConfiguration();
            var model = new ReportIssueVM();
            var freshVM = new ReportIssueVM
            {
                AvailableBins = new List<BinOption>(),
                Issues = new List<IssueLogItem>(),
                TotalIssues = 5
            };

            mockService.Setup(s => s.GetReportIssueViewModelAsync("testcollector", null, null, null))
                .ReturnsAsync(freshVM);

            var controller = CreateController(mockService, mockConfig);
            controller.ModelState.AddModelError("IssueType", "Required");

            // Act
            var result = await controller.ReportIssue(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var returnedModel = Assert.IsType<ReportIssueVM>(viewResult.Model);
            Assert.Equal(5, returnedModel.TotalIssues);
        }

        [Fact]
        public async Task ReportIssuePost_ReturnsUnauthorized_WhenUsernameIsNull()
        {
            // Arrange
            var mockService = CreateMockCollectorService();
            var mockConfig = CreateMockConfiguration();
            var model = new ReportIssueVM();
            var controller = CreateController(mockService, mockConfig, null!);
            controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

            // Act
            var result = await controller.ReportIssue(model);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        #endregion

        #region StartIssueWork Tests

        [Fact]
        public async Task StartIssueWork_RedirectsToReportIssue_WithSuccessMessage()
        {
            // Arrange
            var mockService = CreateMockCollectorService();
            var mockConfig = CreateMockConfiguration();
            
            mockService.Setup(s => s.StartIssueWorkAsync(1, "testcollector"))
                .ReturnsAsync("In Progress");

            var controller = CreateController(mockService, mockConfig);

            // Act
            var result = await controller.StartIssueWork(1);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("ReportIssue", redirectResult.ActionName);
            Assert.Equal("Issue marked as In Progress.", controller.TempData["SuccessMessage"]);
        }

        [Fact]
        public async Task StartIssueWork_SetsErrorMessage_WhenIssueNotFound()
        {
            // Arrange
            var mockService = CreateMockCollectorService();
            var mockConfig = CreateMockConfiguration();
            
            mockService.Setup(s => s.StartIssueWorkAsync(99, "testcollector"))
                .ReturnsAsync("Issue not found for this route.");

            var controller = CreateController(mockService, mockConfig);

            // Act
            var result = await controller.StartIssueWork(99);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Issue not found for this route.", controller.TempData["ErrorMessage"]);
        }

        [Fact]
        public async Task StartIssueWork_SetsSuccessMessage_WhenAlreadyResolved()
        {
            // Arrange
            var mockService = CreateMockCollectorService();
            var mockConfig = CreateMockConfiguration();
            
            mockService.Setup(s => s.StartIssueWorkAsync(1, "testcollector"))
                .ReturnsAsync("Issue is already resolved.");

            var controller = CreateController(mockService, mockConfig);

            // Act
            var result = await controller.StartIssueWork(1);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Issue is already resolved.", controller.TempData["SuccessMessage"]);
        }

        [Fact]
        public async Task StartIssueWork_SetsResolvedMessage_WhenStatusIsResolved()
        {
            // Arrange
            var mockService = CreateMockCollectorService();
            var mockConfig = CreateMockConfiguration();
            
            mockService.Setup(s => s.StartIssueWorkAsync(1, "testcollector"))
                .ReturnsAsync("Resolved");

            var controller = CreateController(mockService, mockConfig);

            // Act
            var result = await controller.StartIssueWork(1);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Issue marked as Resolved.", controller.TempData["SuccessMessage"]);
        }

        [Fact]
        public async Task StartIssueWork_ReturnsUnauthorized_WhenUsernameIsNull()
        {
            // Arrange
            var mockService = CreateMockCollectorService();
            var mockConfig = CreateMockConfiguration();
            var controller = CreateController(mockService, mockConfig, null!);
            controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

            // Act
            var result = await controller.StartIssueWork(1);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        #endregion

        #region MyRouteAssignments Tests

        [Fact]
        public async Task MyRouteAssignments_ReturnsView_WithViewModel()
        {
            // Arrange
            var mockService = CreateMockCollectorService();
            var mockConfig = CreateMockConfiguration();
            var expectedVM = new RouteAssignmentSearchViewModel
            {
                CurrentPage = 1,
                PageSize = 10,
                TotalItems = 5
            };

            mockService.Setup(s => s.GetRouteAssignmentsAsync(
                "testcollector", null, null, null, null, 1, 10))
                .ReturnsAsync(expectedVM);

            var controller = CreateController(mockService, mockConfig);

            // Act
            var result = await controller.MyRouteAssignments(null, null, null, null, 1, 10);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<RouteAssignmentSearchViewModel>(viewResult.Model);
            Assert.Equal(5, model.TotalItems);
        }

        [Fact]
        public async Task MyRouteAssignments_PassesAllFilters_ToService()
        {
            // Arrange
            var mockService = CreateMockCollectorService();
            var mockConfig = CreateMockConfiguration();
            var date = DateTime.Today;
            
            mockService.Setup(s => s.GetRouteAssignmentsAsync(
                "testcollector", "search", 1, date, "Completed", 2, 20))
                .ReturnsAsync(new RouteAssignmentSearchViewModel())
                .Verifiable();

            var controller = CreateController(mockService, mockConfig);

            // Act
            await controller.MyRouteAssignments("search", 1, date, "Completed", 2, 20);

            // Assert
            mockService.Verify();
        }

        [Fact]
        public async Task MyRouteAssignments_ReturnsUnauthorized_WhenUsernameIsNull()
        {
            // Arrange
            var mockService = CreateMockCollectorService();
            var mockConfig = CreateMockConfiguration();
            var controller = CreateController(mockService, mockConfig, null!);
            controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

            // Act
            var result = await controller.MyRouteAssignments(null, null, null, null);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        #endregion

        #region RouteAssignmentDetails Tests

        [Fact]
        public async Task RouteAssignmentDetails_ReturnsView_WithViewModel()
        {
            // Arrange
            var mockService = CreateMockCollectorService();
            var mockConfig = CreateMockConfiguration();
            var expectedVM = new RouteAssignmentDetailViewModel
            {
                AssignmentId = 1,
                RouteId = 5,
                RouteStatus = "In Progress"
            };

            mockService.Setup(s => s.GetRouteAssignmentDetailsAsync(1, "testcollector"))
                .ReturnsAsync(expectedVM);

            var controller = CreateController(mockService, mockConfig);

            // Act
            var result = await controller.RouteAssignmentDetails(1);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<RouteAssignmentDetailViewModel>(viewResult.Model);
            Assert.Equal(5, model.RouteId);
        }

        [Fact]
        public async Task RouteAssignmentDetails_ReturnsNotFound_WhenViewModelIsNull()
        {
            // Arrange
            var mockService = CreateMockCollectorService();
            var mockConfig = CreateMockConfiguration();
            
            mockService.Setup(s => s.GetRouteAssignmentDetailsAsync(99, "testcollector"))
                .ReturnsAsync((RouteAssignmentDetailViewModel?)null);

            var controller = CreateController(mockService, mockConfig);

            // Act
            var result = await controller.RouteAssignmentDetails(99);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task RouteAssignmentDetails_ReturnsUnauthorized_WhenUsernameIsNull()
        {
            // Arrange
            var mockService = CreateMockCollectorService();
            var mockConfig = CreateMockConfiguration();
            var controller = CreateController(mockService, mockConfig, null!);
            controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

            // Act
            var result = await controller.RouteAssignmentDetails(1);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        #endregion

        #region GetNextStops Tests

        [Fact]
        public async Task GetNextStops_ReturnsJson_WithViewModel()
        {
            // Arrange
            var mockService = CreateMockCollectorService();
            var mockConfig = CreateMockConfiguration();
            var expectedVM = new NextStopsViewModel
            {
                RouteId = 1,
                TotalPendingStops = 5,
                NextStops = new List<RouteStopDisplayItem>()
            };

            mockService.Setup(s => s.GetNextStopsAsync("testcollector", 10))
                .ReturnsAsync(expectedVM);

            var controller = CreateController(mockService, mockConfig);

            // Act
            var result = await controller.GetNextStops(10);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            var model = Assert.IsType<NextStopsViewModel>(jsonResult.Value);
            Assert.Equal(1, model.RouteId);
            Assert.Equal(5, model.TotalPendingStops);
        }

        [Fact]
        public async Task GetNextStops_UsesDefaultTop_WhenNotProvided()
        {
            // Arrange
            var mockService = CreateMockCollectorService();
            var mockConfig = CreateMockConfiguration();
            
            mockService.Setup(s => s.GetNextStopsAsync("testcollector", 10))
                .ReturnsAsync(new NextStopsViewModel())
                .Verifiable();

            var controller = CreateController(mockService, mockConfig);

            // Act
            await controller.GetNextStops(null);

            // Assert
            mockService.Verify();
        }

        [Fact]
        public async Task GetNextStops_ReturnsNotFound_WhenViewModelIsNull()
        {
            // Arrange
            var mockService = CreateMockCollectorService();
            var mockConfig = CreateMockConfiguration();
            
            mockService.Setup(s => s.GetNextStopsAsync("testcollector", 10))
                .ReturnsAsync((NextStopsViewModel?)null);

            var controller = CreateController(mockService, mockConfig);

            // Act
            var result = await controller.GetNextStops(10);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var value = notFoundResult.Value;
            Assert.NotNull(value);
            var messageProp = value.GetType().GetProperty("message");
            Assert.NotNull(messageProp);
            Assert.Equal("No active route assignment for today", (string)messageProp.GetValue(value)!);
        }

        [Fact]
        public async Task GetNextStops_ReturnsUnauthorized_WhenUsernameIsNull()
        {
            // Arrange
            var mockService = CreateMockCollectorService();
            var mockConfig = CreateMockConfiguration();
            var controller = CreateController(mockService, mockConfig, null!);
            controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

            // Act
            var result = await controller.GetNextStops(10);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        #endregion
    }
}
