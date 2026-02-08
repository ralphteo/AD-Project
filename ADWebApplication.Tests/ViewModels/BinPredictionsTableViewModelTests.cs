using ADWebApplication.Models.ViewModels.BinPredictions;
using Xunit;

namespace ADWebApplication.Tests.ViewModels
{
    public class BinPredictionsTableViewModelTests
    {
        [Fact]
        public void BinPredictionsTableViewModel_DefaultValues_AreSetCorrectly()
        {
            // Arrange & Act
            var viewModel = new BinPredictionsTableViewModel();

            // Assert
            Assert.Equal(0, viewModel.BinId);
            Assert.Equal("", viewModel.Region);
            Assert.Equal(default, viewModel.LastCollectionDateTime);
            Assert.Null(viewModel.PredictedNextAvgDailyGrowth);
            Assert.Equal(0, viewModel.EstimatedFillToday);
            Assert.Null(viewModel.EstimatedDaysToThreshold);
            Assert.False(viewModel.AutoSelected);
            Assert.Equal("", viewModel.RiskLevel);
            Assert.Equal("Not Scheduled", viewModel.PlanningStatus);
            Assert.Null(viewModel.RouteId);
            Assert.False(viewModel.IsNewCycleDetected);
            Assert.False(viewModel.NeedsPredictionRefresh);
            Assert.False(viewModel.CollectionDone);
            Assert.False(viewModel.IsActualFillLevel);
        }

        [Fact]
        public void BinPredictionsTableViewModel_AllProperties_CanBeSet()
        {
            // Arrange
            var viewModel = new BinPredictionsTableViewModel();
            var testDate = new DateTimeOffset(2026, 1, 15, 10, 30, 0, TimeSpan.Zero);

            // Act
            viewModel.BinId = 123;
            viewModel.Region = "Central";
            viewModel.LastCollectionDateTime = testDate;
            viewModel.PredictedNextAvgDailyGrowth = 5.5;
            viewModel.EstimatedFillToday = 75.5;
            viewModel.EstimatedDaysToThreshold = 3;
            viewModel.AutoSelected = true;
            viewModel.RiskLevel = "High";
            viewModel.PlanningStatus = "Scheduled";
            viewModel.RouteId = "ROUTE-001";
            viewModel.IsNewCycleDetected = true;
            viewModel.NeedsPredictionRefresh = true;
            viewModel.CollectionDone = true;
            viewModel.IsActualFillLevel = true;

            // Assert
            Assert.Equal(123, viewModel.BinId);
            Assert.Equal("Central", viewModel.Region);
            Assert.Equal(testDate, viewModel.LastCollectionDateTime);
            Assert.Equal(5.5, viewModel.PredictedNextAvgDailyGrowth);
            Assert.Equal(75.5, viewModel.EstimatedFillToday);
            Assert.Equal(3, viewModel.EstimatedDaysToThreshold);
            Assert.True(viewModel.AutoSelected);
            Assert.Equal("High", viewModel.RiskLevel);
            Assert.Equal("Scheduled", viewModel.PlanningStatus);
            Assert.Equal("ROUTE-001", viewModel.RouteId);
            Assert.True(viewModel.IsNewCycleDetected);
            Assert.True(viewModel.NeedsPredictionRefresh);
            Assert.True(viewModel.CollectionDone);
            Assert.True(viewModel.IsActualFillLevel);
        }

        [Fact]
        public void CycleStartDisplay_FormatsDateCorrectly()
        {
            // Arrange
            var viewModel = new BinPredictionsTableViewModel
            {
                LastCollectionDateTime = new DateTimeOffset(2026, 2, 7, 14, 30, 0, TimeSpan.Zero)
            };

            // Act
            var display = viewModel.CycleStartDisplay;

            // Assert
            Assert.Equal("Cycle started: 07 Feb 2026", display);
        }

        [Fact]
        public void CycleStartDisplay_HandlesJanuaryDate()
        {
            // Arrange
            var viewModel = new BinPredictionsTableViewModel
            {
                LastCollectionDateTime = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
            };

            // Act
            var display = viewModel.CycleStartDisplay;

            // Assert
            Assert.Equal("Cycle started: 01 Jan 2026", display);
        }

        [Fact]
        public void CycleStartDisplay_HandlesDecemberDate()
        {
            // Arrange
            var viewModel = new BinPredictionsTableViewModel
            {
                LastCollectionDateTime = new DateTimeOffset(2025, 12, 31, 23, 59, 59, TimeSpan.Zero)
            };

            // Act
            var display = viewModel.CycleStartDisplay;

            // Assert
            Assert.Equal("Cycle started: 31 Dec 2025", display);
        }

        [Fact]
        public void RiskLevel_CanBeNull()
        {
            // Arrange & Act
            var viewModel = new BinPredictionsTableViewModel
            {
                RiskLevel = null
            };

            // Assert
            Assert.Null(viewModel.RiskLevel);
        }

        [Fact]
        public void RouteId_CanBeNull()
        {
            // Arrange & Act
            var viewModel = new BinPredictionsTableViewModel
            {
                RouteId = null
            };

            // Assert
            Assert.Null(viewModel.RouteId);
        }

        [Fact]
        public void EstimatedDaysToThreshold_CanBeNull()
        {
            // Arrange & Act
            var viewModel = new BinPredictionsTableViewModel
            {
                EstimatedDaysToThreshold = null
            };

            // Assert
            Assert.Null(viewModel.EstimatedDaysToThreshold);
        }

        [Fact]
        public void PredictedNextAvgDailyGrowth_CanBeNull()
        {
            // Arrange & Act
            var viewModel = new BinPredictionsTableViewModel
            {
                PredictedNextAvgDailyGrowth = null
            };

            // Assert
            Assert.Null(viewModel.PredictedNextAvgDailyGrowth);
        }

        [Fact]
        public void BooleanFlags_CanBeToggled()
        {
            // Arrange
            var viewModel = new BinPredictionsTableViewModel();

            // Act & Assert - Initially false
            Assert.False(viewModel.AutoSelected);
            Assert.False(viewModel.IsNewCycleDetected);
            Assert.False(viewModel.NeedsPredictionRefresh);
            Assert.False(viewModel.CollectionDone);
            Assert.False(viewModel.IsActualFillLevel);

            // Toggle to true
            viewModel.AutoSelected = true;
            viewModel.IsNewCycleDetected = true;
            viewModel.NeedsPredictionRefresh = true;
            viewModel.CollectionDone = true;
            viewModel.IsActualFillLevel = true;

            Assert.True(viewModel.AutoSelected);
            Assert.True(viewModel.IsNewCycleDetected);
            Assert.True(viewModel.NeedsPredictionRefresh);
            Assert.True(viewModel.CollectionDone);
            Assert.True(viewModel.IsActualFillLevel);
        }
    }
}
