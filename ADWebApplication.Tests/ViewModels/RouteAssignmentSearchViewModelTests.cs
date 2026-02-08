using ADWebApplication.ViewModels;
using Xunit;

namespace ADWebApplication.Tests.ViewModels
{
    public class RouteAssignmentSearchViewModelTests : IDisposable
    {
        public RouteAssignmentSearchViewModelTests()
        {
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        [Fact]
        public void TotalPages_WithZeroItems_ReturnsZero()
        {
            // Arrange
            var viewModel = new RouteAssignmentSearchViewModel
            {
                TotalItems = 0,
                PageSize = 10
            };

            // Act
            var result = viewModel.TotalPages;

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public void TotalPages_WithItemsLessThanPageSize_ReturnsOne()
        {
            // Arrange
            var viewModel = new RouteAssignmentSearchViewModel
            {
                TotalItems = 5,
                PageSize = 10
            };

            // Act
            var result = viewModel.TotalPages;

            // Assert
            Assert.Equal(1, result);
        }

        [Fact]
        public void TotalPages_WithItemsEqualToPageSize_ReturnsOne()
        {
            // Arrange
            var viewModel = new RouteAssignmentSearchViewModel
            {
                TotalItems = 10,
                PageSize = 10
            };

            // Act
            var result = viewModel.TotalPages;

            // Assert
            Assert.Equal(1, result);
        }

        [Fact]
        public void TotalPages_WithItemsGreaterThanPageSize_ReturnsCorrectPages()
        {
            // Arrange
            var viewModel = new RouteAssignmentSearchViewModel
            {
                TotalItems = 25,
                PageSize = 10
            };

            // Act
            var result = viewModel.TotalPages;

            // Assert
            Assert.Equal(3, result); // 25 items / 10 per page = 3 pages
        }

        [Fact]
        public void TotalPages_WithExactMultiple_ReturnsCorrectPages()
        {
            // Arrange
            var viewModel = new RouteAssignmentSearchViewModel
            {
                TotalItems = 40,
                PageSize = 10
            };

            // Act
            var result = viewModel.TotalPages;

            // Assert
            Assert.Equal(4, result);
        }

        [Fact]
        public void TotalPages_WithSingleItem_ReturnsOne()
        {
            // Arrange
            var viewModel = new RouteAssignmentSearchViewModel
            {
                TotalItems = 1,
                PageSize = 10
            };

            // Act
            var result = viewModel.TotalPages;

            // Assert
            Assert.Equal(1, result);
        }

        [Fact]
        public void HasPreviousPage_OnFirstPage_ReturnsFalse()
        {
            // Arrange
            var viewModel = new RouteAssignmentSearchViewModel
            {
                CurrentPage = 1,
                TotalItems = 100,
                PageSize = 10
            };

            // Act
            var result = viewModel.HasPreviousPage;

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void HasPreviousPage_OnSecondPage_ReturnsTrue()
        {
            // Arrange
            var viewModel = new RouteAssignmentSearchViewModel
            {
                CurrentPage = 2,
                TotalItems = 100,
                PageSize = 10
            };

            // Act
            var result = viewModel.HasPreviousPage;

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void HasPreviousPage_OnLastPage_ReturnsTrue()
        {
            // Arrange
            var viewModel = new RouteAssignmentSearchViewModel
            {
                CurrentPage = 10,
                TotalItems = 100,
                PageSize = 10
            };

            // Act
            var result = viewModel.HasPreviousPage;

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void HasNextPage_OnLastPage_ReturnsFalse()
        {
            // Arrange
            var viewModel = new RouteAssignmentSearchViewModel
            {
                CurrentPage = 10,
                TotalItems = 100,
                PageSize = 10
            };

            // Act
            var result = viewModel.HasNextPage;

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void HasNextPage_OnFirstPage_ReturnsTrue()
        {
            // Arrange
            var viewModel = new RouteAssignmentSearchViewModel
            {
                CurrentPage = 1,
                TotalItems = 100,
                PageSize = 10
            };

            // Act
            var result = viewModel.HasNextPage;

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void HasNextPage_OnMiddlePage_ReturnsTrue()
        {
            // Arrange
            var viewModel = new RouteAssignmentSearchViewModel
            {
                CurrentPage = 5,
                TotalItems = 100,
                PageSize = 10
            };

            // Act
            var result = viewModel.HasNextPage;

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void HasNextPage_WithOnePage_ReturnsFalse()
        {
            // Arrange
            var viewModel = new RouteAssignmentSearchViewModel
            {
                CurrentPage = 1,
                TotalItems = 5,
                PageSize = 10
            };

            // Act
            var result = viewModel.HasNextPage;

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void DefaultValues_AreInitializedCorrectly()
        {
            // Arrange & Act
            var viewModel = new RouteAssignmentSearchViewModel();

            // Assert
            Assert.NotNull(viewModel.Assignments);
            Assert.Empty(viewModel.Assignments);
            Assert.Equal(1, viewModel.CurrentPage);
            Assert.Equal(10, viewModel.PageSize);
            Assert.Equal(0, viewModel.TotalItems);
            Assert.NotNull(viewModel.AvailableRegions);
            Assert.Empty(viewModel.AvailableRegions);
            Assert.NotNull(viewModel.AvailableStatuses);
            Assert.Equal(3, viewModel.AvailableStatuses.Length);
        }

        [Fact]
        public void RouteAssignmentDisplayItem_ProgressPercentage_WithZeroStops_ReturnsZero()
        {
            // Arrange
            var item = new RouteAssignmentDisplayItem
            {
                TotalStops = 0,
                CompletedStops = 0
            };

            // Act
            var result = item.ProgressPercentage;

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public void RouteAssignmentDisplayItem_ProgressPercentage_WithAllCompleted_Returns100()
        {
            // Arrange
            var item = new RouteAssignmentDisplayItem
            {
                TotalStops = 10,
                CompletedStops = 10
            };

            // Act
            var result = item.ProgressPercentage;

            // Assert
            Assert.Equal(100, result);
        }

        [Fact]
        public void RouteAssignmentDisplayItem_ProgressPercentage_WithPartialCompleted_ReturnsCorrectPercentage()
        {
            // Arrange
            var item = new RouteAssignmentDisplayItem
            {
                TotalStops = 10,
                CompletedStops = 3
            };

            // Act
            var result = item.ProgressPercentage;

            // Assert
            Assert.Equal(30, result);
        }

        [Fact]
        public void RouteAssignmentDisplayItem_RouteDisplayName_ReturnsFormattedString()
        {
            // Arrange
            var item = new RouteAssignmentDisplayItem
            {
                RouteId = 456
            };

            // Act
            var result = item.RouteDisplayName;

            // Assert
            Assert.Equal("Route #456", result);
        }

        [Fact]
        public void RouteAssignmentDisplayItem_DefaultStatus_IsPending()
        {
            // Arrange & Act
            var item = new RouteAssignmentDisplayItem();

            // Assert
            Assert.Equal("Pending", item.Status);
        }

        [Fact]
        public void Pagination_EdgeCase_CurrentPageGreaterThanTotalPages()
        {
            // Arrange
            var viewModel = new RouteAssignmentSearchViewModel
            {
                CurrentPage = 5,
                TotalItems = 10,
                PageSize = 10
            };

            // Act
            var hasNext = viewModel.HasNextPage;
            var hasPrevious = viewModel.HasPreviousPage;

            // Assert
            Assert.False(hasNext); // Page 5 > 1 total page
            Assert.True(hasPrevious); // Has previous because CurrentPage > 1
        }
    }
}
