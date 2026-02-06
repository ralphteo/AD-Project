using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using ADWebApplication.ViewComponents;
using ADWebApplication.Services;
using ADWebApplication.Models.ViewModels.BinPredictions;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ADWebApplication.Tests
{
    public class HighRiskBinBadgeViewComponentTests
    {
        private Mock<IBinPredictionService> CreateMockBinPredictionService()
        {
            return new Mock<IBinPredictionService>();
        }

        #region InvokeAsync Tests

        [Fact]
        public async Task InvokeAsync_CallsService_WithCorrectParameters()
        {
            // Arrange
            var mockService = CreateMockBinPredictionService();
            var expectedViewModel = new BinPredictionsPageViewModel
            {
                HighRiskUnscheduledCount = 5
            };

            mockService.Setup(s => s.BuildBinPredictionsPageAsync(1, "DaysToThreshold", "asc", "High", "3"))
                .ReturnsAsync(expectedViewModel)
                .Verifiable();

            var viewComponent = new HighRiskBinBadgeViewComponent(mockService.Object);

            // Act
            var result = await viewComponent.InvokeAsync();

            // Assert
            mockService.Verify(s => s.BuildBinPredictionsPageAsync(1, "DaysToThreshold", "asc", "High", "3"), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_ReturnsViewResult()
        {
            // Arrange
            var mockService = CreateMockBinPredictionService();
            var expectedViewModel = new BinPredictionsPageViewModel
            {
                HighRiskUnscheduledCount = 3
            };

            mockService.Setup(s => s.BuildBinPredictionsPageAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()
            )).ReturnsAsync(expectedViewModel);

            var viewComponent = new HighRiskBinBadgeViewComponent(mockService.Object);

            // Act
            var result = await viewComponent.InvokeAsync();

            // Assert
            Assert.IsType<ViewViewComponentResult>(result);
        }

        [Fact]
        public async Task InvokeAsync_PassesHighRiskUnscheduledCount_ToView()
        {
            // Arrange
            var mockService = CreateMockBinPredictionService();
            var expectedViewModel = new BinPredictionsPageViewModel
            {
                HighRiskUnscheduledCount = 7
            };

            mockService.Setup(s => s.BuildBinPredictionsPageAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()
            )).ReturnsAsync(expectedViewModel);

            var viewComponent = new HighRiskBinBadgeViewComponent(mockService.Object);

            // Act
            var result = await viewComponent.InvokeAsync();

            // Assert
            var viewResult = Assert.IsType<ViewViewComponentResult>(result);
            Assert.Equal(7, viewResult.ViewData!.Model);
        }

        [Fact]
        public async Task InvokeAsync_WithZeroHighRiskBins_ReturnsZero()
        {
            // Arrange
            var mockService = CreateMockBinPredictionService();
            var expectedViewModel = new BinPredictionsPageViewModel
            {
                HighRiskUnscheduledCount = 0
            };

            mockService.Setup(s => s.BuildBinPredictionsPageAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()
            )).ReturnsAsync(expectedViewModel);

            var viewComponent = new HighRiskBinBadgeViewComponent(mockService.Object);

            // Act
            var result = await viewComponent.InvokeAsync();

            // Assert
            var viewResult = Assert.IsType<ViewViewComponentResult>(result);
            Assert.Equal(0, viewResult.ViewData!.Model);
        }

        [Fact]
        public async Task InvokeAsync_WithLargeCount_ReturnsCorrectValue()
        {
            // Arrange
            var mockService = CreateMockBinPredictionService();
            var expectedViewModel = new BinPredictionsPageViewModel
            {
                HighRiskUnscheduledCount = 50
            };

            mockService.Setup(s => s.BuildBinPredictionsPageAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()
            )).ReturnsAsync(expectedViewModel);

            var viewComponent = new HighRiskBinBadgeViewComponent(mockService.Object);

            // Act
            var result = await viewComponent.InvokeAsync();

            // Assert
            var viewResult = Assert.IsType<ViewViewComponentResult>(result);
            Assert.Equal(50, viewResult.ViewData!.Model);
        }

        [Fact]
        public async Task InvokeAsync_AlwaysFilters_ByHighRisk()
        {
            // Arrange
            var mockService = CreateMockBinPredictionService();
            var expectedViewModel = new BinPredictionsPageViewModel
            {
                HighRiskUnscheduledCount = 10,
                SelectedRisk = "High"
            };

            mockService.Setup(s => s.BuildBinPredictionsPageAsync(1, "DaysToThreshold", "asc", "High", "3"))
                .ReturnsAsync(expectedViewModel);

            var viewComponent = new HighRiskBinBadgeViewComponent(mockService.Object);

            // Act
            await viewComponent.InvokeAsync();

            // Assert
            mockService.Verify(s => s.BuildBinPredictionsPageAsync(
                1,
                "DaysToThreshold",
                "asc",
                "High", // Must always filter by High risk
                "3"
            ), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_AlwaysFilters_By3DayTimeframe()
        {
            // Arrange
            var mockService = CreateMockBinPredictionService();
            var expectedViewModel = new BinPredictionsPageViewModel
            {
                HighRiskUnscheduledCount = 10,
                SelectedTimeframe = "3"
            };

            mockService.Setup(s => s.BuildBinPredictionsPageAsync(1, "DaysToThreshold", "asc", "High", "3"))
                .ReturnsAsync(expectedViewModel);

            var viewComponent = new HighRiskBinBadgeViewComponent(mockService.Object);

            // Act
            await viewComponent.InvokeAsync();

            // Assert
            mockService.Verify(s => s.BuildBinPredictionsPageAsync(
                1,
                "DaysToThreshold",
                "asc",
                "High",
                "3" // Must always filter by 3-day timeframe
            ), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_AlwaysSorts_ByDaysToThresholdAscending()
        {
            // Arrange
            var mockService = CreateMockBinPredictionService();
            var expectedViewModel = new BinPredictionsPageViewModel
            {
                HighRiskUnscheduledCount = 10,
                SortBy = "DaysToThreshold",
                SortDir = "asc"
            };

            mockService.Setup(s => s.BuildBinPredictionsPageAsync(1, "DaysToThreshold", "asc", "High", "3"))
                .ReturnsAsync(expectedViewModel);

            var viewComponent = new HighRiskBinBadgeViewComponent(mockService.Object);

            // Act
            await viewComponent.InvokeAsync();

            // Assert
            mockService.Verify(s => s.BuildBinPredictionsPageAsync(
                1,
                "DaysToThreshold", // Sort field
                "asc", // Sort direction
                "High",
                "3"
            ), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_AlwaysRequests_FirstPage()
        {
            // Arrange
            var mockService = CreateMockBinPredictionService();
            var expectedViewModel = new BinPredictionsPageViewModel
            {
                HighRiskUnscheduledCount = 10,
                CurrentPage = 1
            };

            mockService.Setup(s => s.BuildBinPredictionsPageAsync(1, "DaysToThreshold", "asc", "High", "3"))
                .ReturnsAsync(expectedViewModel);

            var viewComponent = new HighRiskBinBadgeViewComponent(mockService.Object);

            // Act
            await viewComponent.InvokeAsync();

            // Assert
            mockService.Verify(s => s.BuildBinPredictionsPageAsync(
                1, // Must always request page 1
                "DaysToThreshold",
                "asc",
                "High",
                "3"
            ), Times.Once);
        }

        #endregion

        #region Constructor Tests

        [Fact]
        public void Constructor_AcceptsBinPredictionService()
        {
            // Arrange
            var mockService = CreateMockBinPredictionService();

            // Act
            var viewComponent = new HighRiskBinBadgeViewComponent(mockService.Object);

            // Assert
            Assert.NotNull(viewComponent);
        }

        [Fact]
        public void Constructor_ThrowsException_WhenServiceIsNull()
        {
            // Arrange, Act & Assert
            Assert.Throws<System.ArgumentNullException>(() =>
                new HighRiskBinBadgeViewComponent(null!)
            );
        }

        #endregion

        #region Edge Cases

        [Fact]
        public async Task InvokeAsync_HandlesEmptyViewModel_Gracefully()
        {
            // Arrange
            var mockService = CreateMockBinPredictionService();
            var expectedViewModel = new BinPredictionsPageViewModel
            {
                HighRiskUnscheduledCount = 0,
                Rows = new List<BinPredictionsTableViewModel>()
            };

            mockService.Setup(s => s.BuildBinPredictionsPageAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()
            )).ReturnsAsync(expectedViewModel);

            var viewComponent = new HighRiskBinBadgeViewComponent(mockService.Object);

            // Act
            var result = await viewComponent.InvokeAsync();

            // Assert
            var viewResult = Assert.IsType<ViewViewComponentResult>(result);
            Assert.Equal(0, viewResult.ViewData!.Model);
        }

        [Fact]
        public async Task InvokeAsync_OnlyReturnsUnscheduledHighRiskCount()
        {
            // Arrange
            var mockService = CreateMockBinPredictionService();
            var expectedViewModel = new BinPredictionsPageViewModel
            {
                HighRiskUnscheduledCount = 8,
                HighPriorityBins = 15, // Total high priority (including scheduled)
                Rows = new List<BinPredictionsTableViewModel>
                {
                    new BinPredictionsTableViewModel
                    {
                        RiskLevel = "High",
                        PlanningStatus = "Not Scheduled"
                    },
                    new BinPredictionsTableViewModel
                    {
                        RiskLevel = "High",
                        PlanningStatus = "Scheduled"
                    }
                }
            };

            mockService.Setup(s => s.BuildBinPredictionsPageAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()
            )).ReturnsAsync(expectedViewModel);

            var viewComponent = new HighRiskBinBadgeViewComponent(mockService.Object);

            // Act
            var result = await viewComponent.InvokeAsync();

            // Assert
            var viewResult = Assert.IsType<ViewViewComponentResult>(result);
            Assert.Equal(8, viewResult.ViewData!.Model); // Only unscheduled count, not total high priority
        }

        #endregion

        #region Integration with Service

        [Fact]
        public async Task InvokeAsync_CalledMultipleTimes_QueriesServiceEachTime()
        {
            // Arrange
            var mockService = CreateMockBinPredictionService();
            var expectedViewModel = new BinPredictionsPageViewModel
            {
                HighRiskUnscheduledCount = 5
            };

            mockService.Setup(s => s.BuildBinPredictionsPageAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()
            )).ReturnsAsync(expectedViewModel);

            var viewComponent = new HighRiskBinBadgeViewComponent(mockService.Object);

            // Act
            await viewComponent.InvokeAsync();
            await viewComponent.InvokeAsync();
            await viewComponent.InvokeAsync();

            // Assert
            mockService.Verify(s => s.BuildBinPredictionsPageAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()
            ), Times.Exactly(3));
        }

        [Fact]
        public async Task InvokeAsync_ReflectsRealTimeData_FromService()
        {
            // Arrange
            var mockService = CreateMockBinPredictionService();
            
            // First call returns 5
            mockService.SetupSequence(s => s.BuildBinPredictionsPageAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()
            ))
            .ReturnsAsync(new BinPredictionsPageViewModel { HighRiskUnscheduledCount = 5 })
            .ReturnsAsync(new BinPredictionsPageViewModel { HighRiskUnscheduledCount = 10 });

            var viewComponent = new HighRiskBinBadgeViewComponent(mockService.Object);

            // Act
            var result1 = await viewComponent.InvokeAsync();
            var result2 = await viewComponent.InvokeAsync();

            // Assert
            var viewResult1 = Assert.IsType<ViewViewComponentResult>(result1);
            Assert.Equal(5, viewResult1.ViewData!.Model);

            var viewResult2 = Assert.IsType<ViewViewComponentResult>(result2);
            Assert.Equal(10, viewResult2.ViewData!.Model);
        }

        #endregion
    }
}
