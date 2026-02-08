using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ADWebApplication.Controllers;
using ADWebApplication.Data.Repository;
using ADWebApplication.Models.DTOs;
using ADWebApplication.Models.ViewModels.BinPredictions;
using ADWebApplication.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ADWebApplication.Tests.Controllers
{
    public class AdminDashboardControllerTests
    {
        private readonly Mock<IDashboardRepository> _mockRepository;
        private readonly Mock<ILogger<AdminDashboardController>> _mockLogger;
        private readonly Mock<IBinPredictionService> _mockBinPredictionService;
        private readonly Mock<IUrlHelper> _mockUrlHelper;

        public AdminDashboardControllerTests()
        {
            _mockRepository = new Mock<IDashboardRepository>();
            _mockLogger = new Mock<ILogger<AdminDashboardController>>();
            _mockBinPredictionService = new Mock<IBinPredictionService>();
            _mockUrlHelper = new Mock<IUrlHelper>();
        }

        private AdminDashboardController CreateController()
        {
            var controller = new AdminDashboardController(
                _mockRepository.Object,
                _mockLogger.Object,
                _mockBinPredictionService.Object
            );

            // Setup UrlHelper
            controller.Url = _mockUrlHelper.Object;

            return controller;
        }

        #region Index Tests

        [Fact]
        public async Task Index_ReturnsViewResult_WithViewModel()
        {
            // Arrange
            SetupMockRepositoryData();
            SetupMockBinPredictionService(0, 0);
            _mockUrlHelper.Setup(u => u.Action(It.IsAny<UrlActionContext>())).Returns("/test");

            var controller = CreateController();

            // Act
            var result = await controller.Index();

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            viewResult!.Model.Should().BeOfType<AdminDashboardViewModel>();
        }

        [Fact]
        public async Task Index_PopulatesViewModel_WithAllData()
        {
            // Arrange
            var kpis = new DashboardKPIs { TotalUsers = 100 };
            var trends = new List<CollectionTrend> { new CollectionTrend { Month = "Jan" } };
            var categories = new List<CategoryBreakdown> { new CategoryBreakdown { Category = "Electronics", Color = "Blue" } };
            var performance = new List<AvgPerformance> { new AvgPerformance { Area = "North" } };

            _mockRepository.Setup(r => r.GetAdminDashboardAsync(null)).ReturnsAsync(kpis);
            _mockRepository.Setup(r => r.GetCollectionTrendsAsync(It.IsAny<int>())).ReturnsAsync(trends);
            _mockRepository.Setup(r => r.GetCategoryBreakdownAsync()).ReturnsAsync(categories);
            _mockRepository.Setup(r => r.GetAvgPerformanceMetricsAsync()).ReturnsAsync(performance);
            _mockRepository.Setup(r => r.GetBinCountsAsync()).ReturnsAsync((10, 50));

            SetupMockBinPredictionService(0, 0);
            _mockUrlHelper.Setup(u => u.Action(It.IsAny<UrlActionContext>())).Returns("/test");

            var controller = CreateController();

            // Act
            var result = await controller.Index();

            // Assert
            var viewResult = result as ViewResult;
            var model = viewResult!.Model as AdminDashboardViewModel;

            model.Should().NotBeNull();
            model!.KPIs.Should().Be(kpis);
            model.KPIs.TotalUsers.Should().Be(100);
            model.CollectionTrends.Should().HaveCount(1);
            model.CategoryBreakdowns.Should().HaveCount(1);
            model.PerformanceMetrics.Should().HaveCount(1);
            model.ActiveBinsCount.Should().Be(10);
            model.TotalBinsCount.Should().Be(50);
        }

        [Fact]
        public async Task Index_CreatesHighRiskAlert_WhenHighRiskCountIsGreaterThanZero()
        {
            // Arrange
            SetupMockRepositoryData();
            SetupMockBinPredictionService(highRiskCount: 5, mlRefreshCount: 0);
            _mockUrlHelper.Setup(u => u.Action(It.IsAny<UrlActionContext>())).Returns("/AdminBinPredictions");

            var controller = CreateController();

            // Act
            var result = await controller.Index();

            // Assert
            var viewResult = result as ViewResult;
            var model = viewResult!.Model as AdminDashboardViewModel;

            model!.HighRiskUnscheduledCount.Should().Be(5);
            model.Alerts.Should().HaveCount(1);
            model.Alerts[0].Type.Should().Be("HighRisk");
            model.Alerts[0].Title.Should().Be("High overflow risk predicted");
            model.Alerts[0].Message.Should().Contain("5 bins");
            model.Alerts[0].LinkText.Should().Be("View Bin Predictions");
        }

        [Fact]
        public async Task Index_CreatesMLRefreshAlert_WhenMLRefreshCountIsGreaterThanZero()
        {
            // Arrange
            SetupMockRepositoryData();
            SetupMockBinPredictionService(highRiskCount: 0, mlRefreshCount: 3);
            _mockUrlHelper.Setup(u => u.Action(It.IsAny<UrlActionContext>())).Returns("/AdminBinPredictions");

            var controller = CreateController();

            // Act
            var result = await controller.Index();

            // Assert
            var viewResult = result as ViewResult;
            var model = viewResult!.Model as AdminDashboardViewModel;

            model!.Alerts.Should().HaveCount(1);
            model.Alerts[0].Type.Should().Be("MLRefresh");
            model.Alerts[0].Title.Should().Be("Bin Predictions need refresh");
            model.Alerts[0].Message.Should().Contain("3 bins");
            model.Alerts[0].LinkText.Should().Be("Refresh Predictions");
        }

        [Fact]
        public async Task Index_CreatesBothAlerts_WhenBothCountsAreGreaterThanZero()
        {
            // Arrange
            SetupMockRepositoryData();
            SetupMockBinPredictionService(highRiskCount: 7, mlRefreshCount: 4);
            _mockUrlHelper.Setup(u => u.Action(It.IsAny<UrlActionContext>())).Returns("/AdminBinPredictions");

            var controller = CreateController();

            // Act
            var result = await controller.Index();

            // Assert
            var viewResult = result as ViewResult;
            var model = viewResult!.Model as AdminDashboardViewModel;

            model!.HighRiskUnscheduledCount.Should().Be(7);
            model.Alerts.Should().HaveCount(2);
            model.Alerts[0].Type.Should().Be("HighRisk");
            model.Alerts[1].Type.Should().Be("MLRefresh");
        }

        [Fact]
        public async Task Index_CreatesNoAlerts_WhenBothCountsAreZero()
        {
            // Arrange
            SetupMockRepositoryData();
            SetupMockBinPredictionService(highRiskCount: 0, mlRefreshCount: 0);
            _mockUrlHelper.Setup(u => u.Action(It.IsAny<UrlActionContext>())).Returns("/test");

            var controller = CreateController();

            // Act
            var result = await controller.Index();

            // Assert
            var viewResult = result as ViewResult;
            var model = viewResult!.Model as AdminDashboardViewModel;

            model!.HighRiskUnscheduledCount.Should().Be(0);
            model.Alerts.Should().BeEmpty();
        }

        [Fact]
        public async Task Index_CallsBinPredictionService_WithCorrectParameters()
        {
            // Arrange
            SetupMockRepositoryData();
            SetupMockBinPredictionService(0, 0);
            _mockUrlHelper.Setup(u => u.Action(It.IsAny<UrlActionContext>())).Returns("/test");

            var controller = CreateController();

            // Act
            await controller.Index();

            // Assert
            _mockBinPredictionService.Verify(
                s => s.BuildBinPredictionsPageAsync(1, "DaysToThreshold", "asc", "High", "3"),
                Times.Once
            );
        }

        [Fact]
        public async Task Index_HandlesException_ReturnsViewWithEmptyViewModel()
        {
            // Arrange
            _mockRepository.Setup(r => r.GetAdminDashboardAsync(null))
                .ThrowsAsync(new Exception("Database error"));

            var controller = CreateController();

            // Act
            var result = await controller.Index();

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            var model = viewResult!.Model as AdminDashboardViewModel;

            model.Should().NotBeNull();
            model!.KPIs.Should().NotBeNull();
            model.CollectionTrends.Should().BeEmpty();
            model.CategoryBreakdowns.Should().BeEmpty();
            model.PerformanceMetrics.Should().BeEmpty();
            model.HighRiskUnscheduledCount.Should().Be(0);
            model.ActiveBinsCount.Should().Be(0);
            model.TotalBinsCount.Should().Be(0);
        }

        [Fact]
        public async Task Index_LogsError_WhenExceptionOccurs()
        {
            // Arrange
            var exception = new Exception("Test exception");
            _mockRepository.Setup(r => r.GetAdminDashboardAsync(null))
                .ThrowsAsync(exception);

            var controller = CreateController();

            // Act
            await controller.Index();

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error retrieving admin dashboard data")),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region RefreshKPIs Tests

        [Fact]
        public async Task RefreshKPIs_ReturnsPartialViewResult()
        {
            // Arrange
            var kpis = new DashboardKPIs { TotalUsers = 100 };
            _mockRepository.Setup(r => r.GetAdminDashboardAsync(null)).ReturnsAsync(kpis);

            var controller = CreateController();

            // Act
            var result = await controller.RefreshKPIs();

            // Assert
            result.Should().BeOfType<PartialViewResult>();
        }

        [Fact]
        public async Task RefreshKPIs_ReturnsCorrectPartialViewName()
        {
            // Arrange
            var kpis = new DashboardKPIs { TotalUsers = 100 };
            _mockRepository.Setup(r => r.GetAdminDashboardAsync(null)).ReturnsAsync(kpis);

            var controller = CreateController();

            // Act
            var result = await controller.RefreshKPIs();

            // Assert
            var partialViewResult = result as PartialViewResult;
            partialViewResult!.ViewName.Should().Be("_KPICards");
        }

        [Fact]
        public async Task RefreshKPIs_PassesKPIsAsModel()
        {
            // Arrange
            var kpis = new DashboardKPIs { TotalUsers = 150 };
            _mockRepository.Setup(r => r.GetAdminDashboardAsync(null)).ReturnsAsync(kpis);

            var controller = CreateController();

            // Act
            var result = await controller.RefreshKPIs();

            // Assert
            var partialViewResult = result as PartialViewResult;
            partialViewResult!.Model.Should().Be(kpis);
            var model = partialViewResult.Model as DashboardKPIs;
            model!.TotalUsers.Should().Be(150);
        }

        [Fact]
        public async Task RefreshKPIs_HandlesException_ReturnsStatusCode500()
        {
            // Arrange
            _mockRepository.Setup(r => r.GetAdminDashboardAsync(null))
                .ThrowsAsync(new Exception("Database error"));

            var controller = CreateController();

            // Act
            var result = await controller.RefreshKPIs();

            // Assert
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(500);
            objectResult.Value.Should().Be("Internal server error");
        }

        [Fact]
        public async Task RefreshKPIs_LogsError_WhenExceptionOccurs()
        {
            // Arrange
            var exception = new Exception("Test exception");
            _mockRepository.Setup(r => r.GetAdminDashboardAsync(null))
                .ThrowsAsync(exception);

            var controller = CreateController();

            // Act
            await controller.RefreshKPIs();

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error retrieving admin dashboard KPIs")),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task RefreshKPIs_CallsRepository_OnlyOnce()
        {
            // Arrange
            var kpis = new DashboardKPIs();
            _mockRepository.Setup(r => r.GetAdminDashboardAsync(null)).ReturnsAsync(kpis);

            var controller = CreateController();

            // Act
            await controller.RefreshKPIs();

            // Assert
            _mockRepository.Verify(r => r.GetAdminDashboardAsync(null), Times.Once);
        }

        #endregion

        #region Helper Methods

        private void SetupMockRepositoryData()
        {
            _mockRepository.Setup(r => r.GetAdminDashboardAsync(null))
                .ReturnsAsync(new DashboardKPIs());
            _mockRepository.Setup(r => r.GetCollectionTrendsAsync(It.IsAny<int>()))
                .ReturnsAsync(new List<CollectionTrend>());
            _mockRepository.Setup(r => r.GetCategoryBreakdownAsync())
                .ReturnsAsync(new List<CategoryBreakdown>());
            _mockRepository.Setup(r => r.GetAvgPerformanceMetricsAsync())
                .ReturnsAsync(new List<AvgPerformance>());
            _mockRepository.Setup(r => r.GetBinCountsAsync())
                .ReturnsAsync((0, 0));
        }

        private void SetupMockBinPredictionService(int highRiskCount, int mlRefreshCount)
        {
            var predictionVm = new BinPredictionsPageViewModel
            {
                HighRiskUnscheduledCount = highRiskCount,
                NewCycleDetectedCount = mlRefreshCount,
                Rows = new List<BinPredictionsTableViewModel>()
            };

            _mockBinPredictionService
                .Setup(s => s.BuildBinPredictionsPageAsync(1, "DaysToThreshold", "asc", "High", "3"))
                .ReturnsAsync(predictionVm);
        }

        #endregion
    }
}
