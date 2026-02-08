using ADWebApplication.Models.DTOs;
using ADWebApplication.Models.ViewModels;
using Xunit;

namespace ADWebApplication.Tests.ViewModels
{
    public class RoutePlanningViewModelTests : IDisposable
    {
        public RoutePlanningViewModelTests()
        {
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        [Fact]
        public void HighPriorityCount_WithEmptyStops_ReturnsZero()
        {
            // Arrange
            var viewModel = new RoutePlanningViewModel
            {
                AllStops = new List<UiRouteStopDto>(),
                CollectionDate = "2026-01-01"
            };

            // Act
            var result = viewModel.HighPriorityCount;

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public void HighPriorityCount_WithNoHighPriorityStops_ReturnsZero()
        {
            // Arrange
            var viewModel = new RoutePlanningViewModel
            {
                AllStops = new List<UiRouteStopDto>
                {
                    new UiRouteStopDto { BinId = 1, IsHighPriority = false },
                    new UiRouteStopDto { BinId = 2, IsHighPriority = false },
                    new UiRouteStopDto { BinId = 3, IsHighPriority = false }
                },
                CollectionDate = "2026-01-01"
            };

            // Act
            var result = viewModel.HighPriorityCount;

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public void HighPriorityCount_WithAllHighPriorityStops_ReturnsCorrectCount()
        {
            // Arrange
            var viewModel = new RoutePlanningViewModel
            {
                AllStops = new List<UiRouteStopDto>
                {
                    new UiRouteStopDto { BinId = 1, IsHighPriority = true },
                    new UiRouteStopDto { BinId = 2, IsHighPriority = true },
                    new UiRouteStopDto { BinId = 3, IsHighPriority = true }
                },
                CollectionDate = "2026-01-01"
            };

            // Act
            var result = viewModel.HighPriorityCount;

            // Assert
            Assert.Equal(3, result);
        }

        [Fact]
        public void HighPriorityCount_WithMixedPriorities_ReturnsOnlyHighPriorityCount()
        {
            // Arrange
            var viewModel = new RoutePlanningViewModel
            {
                AllStops = new List<UiRouteStopDto>
                {
                    new UiRouteStopDto { BinId = 1, IsHighPriority = true },
                    new UiRouteStopDto { BinId = 2, IsHighPriority = false },
                    new UiRouteStopDto { BinId = 3, IsHighPriority = true },
                    new UiRouteStopDto { BinId = 4, IsHighPriority = false },
                    new UiRouteStopDto { BinId = 5, IsHighPriority = true }
                },
                CollectionDate = "2026-01-01"
            };

            // Act
            var result = viewModel.HighPriorityCount;

            // Assert
            Assert.Equal(3, result);
        }

        [Fact]
        public void TotalBins_WithEmptyStops_ReturnsZero()
        {
            // Arrange
            var viewModel = new RoutePlanningViewModel
            {
                AllStops = new List<UiRouteStopDto>(),
                CollectionDate = "2026-01-01"
            };

            // Act
            var result = viewModel.TotalBins;

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public void TotalBins_WithNullStops_ReturnsZero()
        {
            // Arrange
            var viewModel = new RoutePlanningViewModel
            {
                AllStops = null!,
                CollectionDate = "2026-01-01"
            };

            // Act
            var result = viewModel.TotalBins;

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public void TotalBins_WithStops_ReturnsCorrectCount()
        {
            // Arrange
            var viewModel = new RoutePlanningViewModel
            {
                AllStops = new List<UiRouteStopDto>
                {
                    new UiRouteStopDto { BinId = 1 },
                    new UiRouteStopDto { BinId = 2 },
                    new UiRouteStopDto { BinId = 3 },
                    new UiRouteStopDto { BinId = 4 },
                    new UiRouteStopDto { BinId = 5 }
                },
                CollectionDate = "2026-01-01"
            };

            // Act
            var result = viewModel.TotalBins;

            // Assert
            Assert.Equal(5, result);
        }

        [Fact]
        public void TotalBins_WithSingleStop_ReturnsOne()
        {
            // Arrange
            var viewModel = new RoutePlanningViewModel
            {
                AllStops = new List<UiRouteStopDto>
                {
                    new UiRouteStopDto { BinId = 1 }
                },
                CollectionDate = "2026-01-01"
            };

            // Act
            var result = viewModel.TotalBins;

            // Assert
            Assert.Equal(1, result);
        }

        [Fact]
        public void DefaultValues_AreInitializedCorrectly()
        {
            // Arrange & Act
            var viewModel = new RoutePlanningViewModel
            {
                CollectionDate = "2026-01-01"
            };

            // Assert
            Assert.NotNull(viewModel.AllStops);
            Assert.Empty(viewModel.AllStops);
            Assert.NotNull(viewModel.Routes);
            Assert.Empty(viewModel.Routes);
            Assert.NotNull(viewModel.AvailableOfficers);
            Assert.Empty(viewModel.AvailableOfficers);
            Assert.Equal(string.Empty, viewModel.TodayDate);
        }

        [Fact]
        public void HighPriorityCount_WithLargeDataset_ReturnsCorrectCount()
        {
            // Arrange
            var stops = new List<UiRouteStopDto>();
            for (int i = 0; i < 1000; i++)
            {
                stops.Add(new UiRouteStopDto
                {
                    BinId = i,
                    IsHighPriority = i % 3 == 0 // Every 3rd stop is high priority
                });
            }

            var viewModel = new RoutePlanningViewModel
            {
                AllStops = stops,
                CollectionDate = "2026-01-01"
            };

            // Act
            var result = viewModel.HighPriorityCount;

            // Assert
            Assert.Equal(334, result); // 0, 3, 6, 9, ..., 999 = 334 stops
        }

        [Fact]
        public void Properties_CanBeSetAndRetrieved()
        {
            // Arrange
            var viewModel = new RoutePlanningViewModel
            {
                CollectionDate = "2026-02-15",
                TodayDate = "2026-02-07"
            };

            var stops = new List<UiRouteStopDto>
            {
                new UiRouteStopDto { BinId = 1, IsHighPriority = true }
            };

            var routes = new List<RouteGroupViewModel>
            {
                new RouteGroupViewModel()
            };

            var officers = new List<CollectionOfficerDto>
            {
                new CollectionOfficerDto { Username = "officer1", FullName = "Officer One" }
            };

            // Act
            viewModel.AllStops = stops;
            viewModel.Routes = routes;
            viewModel.AvailableOfficers = officers;

            // Assert
            Assert.Equal("2026-02-15", viewModel.CollectionDate);
            Assert.Equal("2026-02-07", viewModel.TodayDate);
            Assert.Single(viewModel.AllStops);
            Assert.Single(viewModel.Routes);
            Assert.Single(viewModel.AvailableOfficers);
        }

        [Fact]
        public void HighPriorityCount_MultipleAccess_ReturnsSameValue()
        {
            // Arrange
            var viewModel = new RoutePlanningViewModel
            {
                AllStops = new List<UiRouteStopDto>
                {
                    new UiRouteStopDto { BinId = 1, IsHighPriority = true },
                    new UiRouteStopDto { BinId = 2, IsHighPriority = true }
                },
                CollectionDate = "2026-01-01"
            };

            // Act
            var result1 = viewModel.HighPriorityCount;
            var result2 = viewModel.HighPriorityCount;
            var result3 = viewModel.HighPriorityCount;

            // Assert
            Assert.Equal(2, result1);
            Assert.Equal(2, result2);
            Assert.Equal(2, result3);
            Assert.Equal(result1, result2);
            Assert.Equal(result2, result3);
        }
    }
}
