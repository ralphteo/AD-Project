using ADWebApplication.ViewModels;
using Xunit;

namespace ADWebApplication.Tests.ViewModels
{
    public class RouteAssignmentDetailViewModelTests : IDisposable
    {
        public RouteAssignmentDetailViewModelTests()
        {
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        [Fact]
        public void TotalStops_WithEmptyRouteStops_ReturnsZero()
        {
            // Arrange
            var viewModel = new RouteAssignmentDetailViewModel
            {
                RouteStops = new List<RouteStopDisplayItem>()
            };

            // Act
            var result = viewModel.TotalStops;

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public void TotalStops_WithMultipleStops_ReturnsCorrectCount()
        {
            // Arrange
            var viewModel = new RouteAssignmentDetailViewModel
            {
                RouteStops = new List<RouteStopDisplayItem>
                {
                    new RouteStopDisplayItem { StopId = 1 },
                    new RouteStopDisplayItem { StopId = 2 },
                    new RouteStopDisplayItem { StopId = 3 }
                }
            };

            // Act
            var result = viewModel.TotalStops;

            // Assert
            Assert.Equal(3, result);
        }

        [Fact]
        public void CompletedStops_WithNoCollectedStops_ReturnsZero()
        {
            // Arrange
            var viewModel = new RouteAssignmentDetailViewModel
            {
                RouteStops = new List<RouteStopDisplayItem>
                {
                    new RouteStopDisplayItem { StopId = 1, IsCollected = false },
                    new RouteStopDisplayItem { StopId = 2, IsCollected = false }
                }
            };

            // Act
            var result = viewModel.CompletedStops;

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public void CompletedStops_WithAllCollectedStops_ReturnsCorrectCount()
        {
            // Arrange
            var viewModel = new RouteAssignmentDetailViewModel
            {
                RouteStops = new List<RouteStopDisplayItem>
                {
                    new RouteStopDisplayItem { StopId = 1, IsCollected = true },
                    new RouteStopDisplayItem { StopId = 2, IsCollected = true },
                    new RouteStopDisplayItem { StopId = 3, IsCollected = true }
                }
            };

            // Act
            var result = viewModel.CompletedStops;

            // Assert
            Assert.Equal(3, result);
        }

        [Fact]
        public void CompletedStops_WithMixedStops_ReturnsOnlyCollectedCount()
        {
            // Arrange
            var viewModel = new RouteAssignmentDetailViewModel
            {
                RouteStops = new List<RouteStopDisplayItem>
                {
                    new RouteStopDisplayItem { StopId = 1, IsCollected = true },
                    new RouteStopDisplayItem { StopId = 2, IsCollected = false },
                    new RouteStopDisplayItem { StopId = 3, IsCollected = true }
                }
            };

            // Act
            var result = viewModel.CompletedStops;

            // Assert
            Assert.Equal(2, result);
        }

        [Fact]
        public void PendingStops_WithAllCollectedStops_ReturnsZero()
        {
            // Arrange
            var viewModel = new RouteAssignmentDetailViewModel
            {
                RouteStops = new List<RouteStopDisplayItem>
                {
                    new RouteStopDisplayItem { StopId = 1, IsCollected = true },
                    new RouteStopDisplayItem { StopId = 2, IsCollected = true }
                }
            };

            // Act
            var result = viewModel.PendingStops;

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public void PendingStops_WithNoPendingStops_ReturnsCorrectCount()
        {
            // Arrange
            var viewModel = new RouteAssignmentDetailViewModel
            {
                RouteStops = new List<RouteStopDisplayItem>
                {
                    new RouteStopDisplayItem { StopId = 1, IsCollected = false },
                    new RouteStopDisplayItem { StopId = 2, IsCollected = false },
                    new RouteStopDisplayItem { StopId = 3, IsCollected = false }
                }
            };

            // Act
            var result = viewModel.PendingStops;

            // Assert
            Assert.Equal(3, result);
        }

        [Fact]
        public void PendingStops_WithMixedStops_ReturnsOnlyPendingCount()
        {
            // Arrange
            var viewModel = new RouteAssignmentDetailViewModel
            {
                RouteStops = new List<RouteStopDisplayItem>
                {
                    new RouteStopDisplayItem { StopId = 1, IsCollected = true },
                    new RouteStopDisplayItem { StopId = 2, IsCollected = false },
                    new RouteStopDisplayItem { StopId = 3, IsCollected = true },
                    new RouteStopDisplayItem { StopId = 4, IsCollected = false }
                }
            };

            // Act
            var result = viewModel.PendingStops;

            // Assert
            Assert.Equal(2, result);
        }

        [Fact]
        public void ProgressPercentage_WithEmptyStops_ReturnsZero()
        {
            // Arrange
            var viewModel = new RouteAssignmentDetailViewModel
            {
                RouteStops = new List<RouteStopDisplayItem>()
            };

            // Act
            var result = viewModel.ProgressPercentage;

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public void ProgressPercentage_WithZeroCompleted_ReturnsZero()
        {
            // Arrange
            var viewModel = new RouteAssignmentDetailViewModel
            {
                RouteStops = new List<RouteStopDisplayItem>
                {
                    new RouteStopDisplayItem { StopId = 1, IsCollected = false },
                    new RouteStopDisplayItem { StopId = 2, IsCollected = false }
                }
            };

            // Act
            var result = viewModel.ProgressPercentage;

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public void ProgressPercentage_WithAllCompleted_Returns100()
        {
            // Arrange
            var viewModel = new RouteAssignmentDetailViewModel
            {
                RouteStops = new List<RouteStopDisplayItem>
                {
                    new RouteStopDisplayItem { StopId = 1, IsCollected = true },
                    new RouteStopDisplayItem { StopId = 2, IsCollected = true },
                    new RouteStopDisplayItem { StopId = 3, IsCollected = true }
                }
            };

            // Act
            var result = viewModel.ProgressPercentage;

            // Assert
            Assert.Equal(100, result);
        }

        [Fact]
        public void ProgressPercentage_WithHalfCompleted_Returns50()
        {
            // Arrange
            var viewModel = new RouteAssignmentDetailViewModel
            {
                RouteStops = new List<RouteStopDisplayItem>
                {
                    new RouteStopDisplayItem { StopId = 1, IsCollected = true },
                    new RouteStopDisplayItem { StopId = 2, IsCollected = true },
                    new RouteStopDisplayItem { StopId = 3, IsCollected = false },
                    new RouteStopDisplayItem { StopId = 4, IsCollected = false }
                }
            };

            // Act
            var result = viewModel.ProgressPercentage;

            // Assert
            Assert.Equal(50, result);
        }

        [Fact]
        public void ProgressPercentage_WithPartialCompleted_ReturnsCorrectPercentage()
        {
            // Arrange
            var viewModel = new RouteAssignmentDetailViewModel
            {
                RouteStops = new List<RouteStopDisplayItem>
                {
                    new RouteStopDisplayItem { StopId = 1, IsCollected = true },
                    new RouteStopDisplayItem { StopId = 2, IsCollected = false },
                    new RouteStopDisplayItem { StopId = 3, IsCollected = false }
                }
            };

            // Act
            var result = viewModel.ProgressPercentage;

            // Assert
            Assert.Equal(33, result); // 1/3 = 33.33%, truncated to 33
        }

        [Fact]
        public void RouteDisplayName_ReturnsFormattedString()
        {
            // Arrange
            var viewModel = new RouteAssignmentDetailViewModel
            {
                RouteId = 42
            };

            // Act
            var result = viewModel.RouteDisplayName;

            // Assert
            Assert.Equal("Route #42", result);
        }

        [Fact]
        public void NextStopsViewModel_RouteDisplayName_ReturnsFormattedString()
        {
            // Arrange
            var viewModel = new NextStopsViewModel
            {
                RouteId = 123
            };

            // Act
            var result = viewModel.RouteDisplayName;

            // Assert
            Assert.Equal("Route #123", result);
        }

        [Fact]
        public void NextStopsViewModel_DefaultValues_AreInitializedCorrectly()
        {
            // Arrange & Act
            var viewModel = new NextStopsViewModel();

            // Assert
            Assert.NotNull(viewModel.NextStops);
            Assert.Empty(viewModel.NextStops);
            Assert.Equal(0, viewModel.TotalPendingStops);
        }

        [Fact]
        public void RouteStopDisplayItem_DefaultValues_AreInitializedCorrectly()
        {
            // Arrange & Act
            var item = new RouteStopDisplayItem();

            // Assert
            Assert.False(item.IsCollected);
            Assert.Null(item.CollectedAt);
            Assert.Null(item.CollectionStatus);
            Assert.Null(item.BinFillLevel);
        }
    }
}
