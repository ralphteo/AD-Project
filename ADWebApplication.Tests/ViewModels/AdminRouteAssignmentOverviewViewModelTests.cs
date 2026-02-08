using ADWebApplication.ViewModels;
using Xunit;

namespace ADWebApplication.Tests.ViewModels
{
    public class AdminRouteAssignmentOverviewViewModelTests
    {
        [Fact]
        public void AdminRouteAssignmentOverviewViewModel_DefaultValues_AreSetCorrectly()
        {
            // Arrange & Act
            var viewModel = new AdminRouteAssignmentOverviewViewModel();

            // Assert
            Assert.Empty(viewModel.CollectorAssignments);
            Assert.Equal(1, viewModel.CurrentPage);
            Assert.Equal(10, viewModel.PageSize);
            Assert.Equal(0, viewModel.TotalCollectors);
            Assert.Null(viewModel.FilterDate);
            Assert.Null(viewModel.FilterCollectorUsername);
            Assert.Null(viewModel.FilterAdminUsername);
            Assert.Null(viewModel.FilterStatus);
            Assert.Empty(viewModel.AvailableCollectors);
            Assert.Empty(viewModel.AvailableAdmins);
            Assert.Equal(new[] { "Pending", "Active", "Completed" }, viewModel.AvailableStatuses);
        }

        [Fact]
        public void TotalPages_CalculatesCorrectly_WithExactDivision()
        {
            // Arrange
            var viewModel = new AdminRouteAssignmentOverviewViewModel
            {
                TotalCollectors = 20,
                PageSize = 10
            };

            // Act
            var totalPages = viewModel.TotalPages;

            // Assert
            Assert.Equal(2, totalPages);
        }

        [Fact]
        public void TotalPages_CalculatesCorrectly_WithRemainder()
        {
            // Arrange
            var viewModel = new AdminRouteAssignmentOverviewViewModel
            {
                TotalCollectors = 25,
                PageSize = 10
            };

            // Act
            var totalPages = viewModel.TotalPages;

            // Assert
            Assert.Equal(3, totalPages);
        }

        [Fact]
        public void TotalPages_ReturnsZero_WhenNoCollectors()
        {
            // Arrange
            var viewModel = new AdminRouteAssignmentOverviewViewModel
            {
                TotalCollectors = 0,
                PageSize = 10
            };

            // Act
            var totalPages = viewModel.TotalPages;

            // Assert
            Assert.Equal(0, totalPages);
        }

        [Fact]
        public void TotalPages_CalculatesCorrectly_WithOneCollector()
        {
            // Arrange
            var viewModel = new AdminRouteAssignmentOverviewViewModel
            {
                TotalCollectors = 1,
                PageSize = 10
            };

            // Act
            var totalPages = viewModel.TotalPages;

            // Assert
            Assert.Equal(1, totalPages);
        }

        [Fact]
        public void HasPreviousPage_ReturnsFalse_OnFirstPage()
        {
            // Arrange
            var viewModel = new AdminRouteAssignmentOverviewViewModel
            {
                CurrentPage = 1
            };

            // Act
            var hasPrevious = viewModel.HasPreviousPage;

            // Assert
            Assert.False(hasPrevious);
        }

        [Fact]
        public void HasPreviousPage_ReturnsTrue_OnSecondPage()
        {
            // Arrange
            var viewModel = new AdminRouteAssignmentOverviewViewModel
            {
                CurrentPage = 2
            };

            // Act
            var hasPrevious = viewModel.HasPreviousPage;

            // Assert
            Assert.True(hasPrevious);
        }

        [Fact]
        public void HasNextPage_ReturnsTrue_WhenNotOnLastPage()
        {
            // Arrange
            var viewModel = new AdminRouteAssignmentOverviewViewModel
            {
                CurrentPage = 1,
                TotalCollectors = 20,
                PageSize = 10
            };

            // Act
            var hasNext = viewModel.HasNextPage;

            // Assert
            Assert.True(hasNext);
        }

        [Fact]
        public void HasNextPage_ReturnsFalse_WhenOnLastPage()
        {
            // Arrange
            var viewModel = new AdminRouteAssignmentOverviewViewModel
            {
                CurrentPage = 2,
                TotalCollectors = 20,
                PageSize = 10
            };

            // Act
            var hasNext = viewModel.HasNextPage;

            // Assert
            Assert.False(hasNext);
        }

        [Fact]
        public void HasNextPage_ReturnsFalse_WhenNoPages()
        {
            // Arrange
            var viewModel = new AdminRouteAssignmentOverviewViewModel
            {
                CurrentPage = 1,
                TotalCollectors = 0,
                PageSize = 10
            };

            // Act
            var hasNext = viewModel.HasNextPage;

            // Assert
            Assert.False(hasNext);
        }

        [Fact]
        public void CollectorAssignments_CanBePopulated()
        {
            // Arrange
            var viewModel = new AdminRouteAssignmentOverviewViewModel();
            var assignments = new List<CollectorAssignmentSummary>
            {
                new CollectorAssignmentSummary { CollectorUsername = "collector1" },
                new CollectorAssignmentSummary { CollectorUsername = "collector2" }
            };

            // Act
            viewModel.CollectorAssignments = assignments;

            // Assert
            Assert.Equal(2, viewModel.CollectorAssignments.Count);
            Assert.Equal("collector1", viewModel.CollectorAssignments[0].CollectorUsername);
            Assert.Equal("collector2", viewModel.CollectorAssignments[1].CollectorUsername);
        }

        [Fact]
        public void Filters_CanBeSet()
        {
            // Arrange
            var viewModel = new AdminRouteAssignmentOverviewViewModel();
            var filterDate = new DateTime(2026, 2, 7);

            // Act
            viewModel.FilterDate = filterDate;
            viewModel.FilterCollectorUsername = "john_collector";
            viewModel.FilterAdminUsername = "admin_user";
            viewModel.FilterStatus = "Active";

            // Assert
            Assert.Equal(filterDate, viewModel.FilterDate);
            Assert.Equal("john_collector", viewModel.FilterCollectorUsername);
            Assert.Equal("admin_user", viewModel.FilterAdminUsername);
            Assert.Equal("Active", viewModel.FilterStatus);
        }
    }

    public class CollectorAssignmentSummaryTests
    {
        [Fact]
        public void CollectorAssignmentSummary_DefaultValues_AreSetCorrectly()
        {
            // Arrange & Act
            var summary = new CollectorAssignmentSummary();

            // Assert
            Assert.Equal(string.Empty, summary.CollectorUsername);
            Assert.Equal(string.Empty, summary.CollectorFullName);
            Assert.Empty(summary.Assignments);
        }

        [Fact]
        public void TotalAssignments_ReturnsCorrectCount()
        {
            // Arrange
            var summary = new CollectorAssignmentSummary
            {
                Assignments = new List<AssignmentSummaryItem>
                {
                    new AssignmentSummaryItem { Status = "Completed" },
                    new AssignmentSummaryItem { Status = "Active" },
                    new AssignmentSummaryItem { Status = "Pending" }
                }
            };

            // Act
            var total = summary.TotalAssignments;

            // Assert
            Assert.Equal(3, total);
        }

        [Fact]
        public void CompletedAssignments_CountsOnlyCompleted()
        {
            // Arrange
            var summary = new CollectorAssignmentSummary
            {
                Assignments = new List<AssignmentSummaryItem>
                {
                    new AssignmentSummaryItem { Status = "Completed" },
                    new AssignmentSummaryItem { Status = "Completed" },
                    new AssignmentSummaryItem { Status = "Active" }
                }
            };

            // Act
            var completed = summary.CompletedAssignments;

            // Assert
            Assert.Equal(2, completed);
        }

        [Fact]
        public void ActiveAssignments_CountsOnlyActive()
        {
            // Arrange
            var summary = new CollectorAssignmentSummary
            {
                Assignments = new List<AssignmentSummaryItem>
                {
                    new AssignmentSummaryItem { Status = "Active" },
                    new AssignmentSummaryItem { Status = "Active" },
                    new AssignmentSummaryItem { Status = "Pending" }
                }
            };

            // Act
            var active = summary.ActiveAssignments;

            // Assert
            Assert.Equal(2, active);
        }

        [Fact]
        public void PendingAssignments_CountsOnlyPending()
        {
            // Arrange
            var summary = new CollectorAssignmentSummary
            {
                Assignments = new List<AssignmentSummaryItem>
                {
                    new AssignmentSummaryItem { Status = "Pending" },
                    new AssignmentSummaryItem { Status = "Completed" },
                    new AssignmentSummaryItem { Status = "Active" }
                }
            };

            // Act
            var pending = summary.PendingAssignments;

            // Assert
            Assert.Equal(1, pending);
        }

        [Fact]
        public void StatusCounts_ReturnZero_WhenNoAssignments()
        {
            // Arrange
            var summary = new CollectorAssignmentSummary();

            // Act & Assert
            Assert.Equal(0, summary.TotalAssignments);
            Assert.Equal(0, summary.CompletedAssignments);
            Assert.Equal(0, summary.ActiveAssignments);
            Assert.Equal(0, summary.PendingAssignments);
        }
    }

    public class AssignmentSummaryItemTests
    {
        [Fact]
        public void AssignmentSummaryItem_DefaultValues_AreSetCorrectly()
        {
            // Arrange & Act
            var item = new AssignmentSummaryItem();

            // Assert
            Assert.Equal(0, item.AssignmentId);
            Assert.Equal(0, item.RouteId);
            Assert.Equal(string.Empty, item.RouteName);
            Assert.Equal(default, item.CollectionDate);
            Assert.Equal(string.Empty, item.AssignedBy);
            Assert.Equal(string.Empty, item.Status);
            Assert.Equal(0, item.TotalStops);
            Assert.Equal(0, item.CompletedStops);
        }

        [Fact]
        public void ProgressPercentage_CalculatesCorrectly()
        {
            // Arrange
            var item = new AssignmentSummaryItem
            {
                TotalStops = 10,
                CompletedStops = 5
            };

            // Act
            var progress = item.ProgressPercentage;

            // Assert
            Assert.Equal(50, progress);
        }

        [Fact]
        public void ProgressPercentage_ReturnsZero_WhenNoStops()
        {
            // Arrange
            var item = new AssignmentSummaryItem
            {
                TotalStops = 0,
                CompletedStops = 0
            };

            // Act
            var progress = item.ProgressPercentage;

            // Assert
            Assert.Equal(0, progress);
        }

        [Fact]
        public void ProgressPercentage_Returns100_WhenAllCompleted()
        {
            // Arrange
            var item = new AssignmentSummaryItem
            {
                TotalStops = 8,
                CompletedStops = 8
            };

            // Act
            var progress = item.ProgressPercentage;

            // Assert
            Assert.Equal(100, progress);
        }

        [Fact]
        public void ProgressPercentage_RoundsDown()
        {
            // Arrange
            var item = new AssignmentSummaryItem
            {
                TotalStops = 3,
                CompletedStops = 1
            };

            // Act
            var progress = item.ProgressPercentage;

            // Assert
            Assert.Equal(33, progress);
        }
    }
}
