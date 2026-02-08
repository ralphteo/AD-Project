using Xunit;
using Moq;
using ADWebApplication.Services.Collector;
using ADWebApplication.Models;
using ADWebApplication.ViewModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ADWebApplication.Tests.Services
{
    public class CollectorServiceTests
    {
        private static Mock<ICollectorDashboardService> CreateMockDashboardService()
        {
            return new Mock<ICollectorDashboardService>();
        }

        private static Mock<ICollectorAssignmentService> CreateMockAssignmentService()
        {
            return new Mock<ICollectorAssignmentService>();
        }

        private static Mock<ICollectorIssueService> CreateMockIssueService()
        {
            return new Mock<ICollectorIssueService>();
        }

        private static CollectorService CreateService(
            Mock<ICollectorDashboardService> mockDashboard,
            Mock<ICollectorAssignmentService> mockAssignment,
            Mock<ICollectorIssueService> mockIssue)
        {
            return new CollectorService(
                mockDashboard.Object,
                mockAssignment.Object,
                mockIssue.Object);
        }

        #region Dashboard Service Delegation Tests

        [Fact]
        public async Task GetDailyRouteAsync_DelegatesToDashboardService()
        {
            // Arrange
            var mockDashboard = CreateMockDashboardService();
            var mockAssignment = CreateMockAssignmentService();
            var mockIssue = CreateMockIssueService();
            
            var expectedRoute = new CollectorRoute { RouteName = "Test Route" };
            mockDashboard.Setup(s => s.GetDailyRouteAsync("collector1"))
                .ReturnsAsync(expectedRoute)
                .Verifiable();

            var service = CreateService(mockDashboard, mockAssignment, mockIssue);

            // Act
            var result = await service.GetDailyRouteAsync("collector1");

            // Assert
            Assert.Equal(expectedRoute, result);
            mockDashboard.Verify(s => s.GetDailyRouteAsync("collector1"), Times.Once);
        }

        [Fact]
        public async Task GetCollectionConfirmationAsync_DelegatesToDashboardService()
        {
            // Arrange
            var mockDashboard = CreateMockDashboardService();
            var mockAssignment = CreateMockAssignmentService();
            var mockIssue = CreateMockIssueService();
            
            var expectedVM = new CollectionConfirmationVM { StopId = 1 };
            mockDashboard.Setup(s => s.GetCollectionConfirmationAsync(1, "collector1"))
                .ReturnsAsync(expectedVM)
                .Verifiable();

            var service = CreateService(mockDashboard, mockAssignment, mockIssue);

            // Act
            var result = await service.GetCollectionConfirmationAsync(1, "collector1");

            // Assert
            Assert.Equal(expectedVM, result);
            mockDashboard.Verify(s => s.GetCollectionConfirmationAsync(1, "collector1"), Times.Once);
        }

        [Fact]
        public async Task ConfirmCollectionAsync_DelegatesToDashboardService()
        {
            // Arrange
            var mockDashboard = CreateMockDashboardService();
            var mockAssignment = CreateMockAssignmentService();
            var mockIssue = CreateMockIssueService();
            
            var model = new CollectionConfirmationVM { StopId = 1, BinFillLevel = 75 };
            mockDashboard.Setup(s => s.ConfirmCollectionAsync(model, "collector1"))
                .ReturnsAsync(true)
                .Verifiable();

            var service = CreateService(mockDashboard, mockAssignment, mockIssue);

            // Act
            var result = await service.ConfirmCollectionAsync(model, "collector1");

            // Assert
            Assert.True(result);
            mockDashboard.Verify(s => s.ConfirmCollectionAsync(model, "collector1"), Times.Once);
        }

        #endregion

        #region Assignment Service Delegation Tests

        [Fact]
        public async Task GetRouteAssignmentsAsync_DelegatesToAssignmentService()
        {
            // Arrange
            var mockDashboard = CreateMockDashboardService();
            var mockAssignment = CreateMockAssignmentService();
            var mockIssue = CreateMockIssueService();
            
            var expectedVM = new RouteAssignmentSearchViewModel { TotalItems = 5 };
            mockAssignment.Setup(s => s.GetRouteAssignmentsAsync(
                "collector1", "search", 1, It.IsAny<DateTime>(), "Pending", 1, 10))
                .ReturnsAsync(expectedVM)
                .Verifiable();

            var service = CreateService(mockDashboard, mockAssignment, mockIssue);

            // Act
            var result = await service.GetRouteAssignmentsAsync("collector1", "search", 1, DateTime.Today, "Pending", 1, 10);

            // Assert
            Assert.Equal(expectedVM, result);
            mockAssignment.Verify(s => s.GetRouteAssignmentsAsync(
                "collector1", "search", 1, It.IsAny<DateTime>(), "Pending", 1, 10), Times.Once);
        }

        [Fact]
        public async Task GetRouteAssignmentDetailsAsync_DelegatesToAssignmentService()
        {
            // Arrange
            var mockDashboard = CreateMockDashboardService();
            var mockAssignment = CreateMockAssignmentService();
            var mockIssue = CreateMockIssueService();
            
            var expectedVM = new RouteAssignmentDetailViewModel { AssignmentId = 1 };
            mockAssignment.Setup(s => s.GetRouteAssignmentDetailsAsync(1, "collector1"))
                .ReturnsAsync(expectedVM)
                .Verifiable();

            var service = CreateService(mockDashboard, mockAssignment, mockIssue);

            // Act
            var result = await service.GetRouteAssignmentDetailsAsync(1, "collector1");

            // Assert
            Assert.Equal(expectedVM, result);
            mockAssignment.Verify(s => s.GetRouteAssignmentDetailsAsync(1, "collector1"), Times.Once);
        }

        [Fact]
        public async Task GetNextStopsAsync_DelegatesToAssignmentService()
        {
            // Arrange
            var mockDashboard = CreateMockDashboardService();
            var mockAssignment = CreateMockAssignmentService();
            var mockIssue = CreateMockIssueService();
            
            var expectedVM = new NextStopsViewModel { RouteId = 1, TotalPendingStops = 5 };
            mockAssignment.Setup(s => s.GetNextStopsAsync("collector1", 10))
                .ReturnsAsync(expectedVM)
                .Verifiable();

            var service = CreateService(mockDashboard, mockAssignment, mockIssue);

            // Act
            var result = await service.GetNextStopsAsync("collector1", 10);

            // Assert
            Assert.Equal(expectedVM, result);
            mockAssignment.Verify(s => s.GetNextStopsAsync("collector1", 10), Times.Once);
        }

        #endregion

        #region Issue Service Delegation Tests

        [Fact]
        public async Task GetReportIssueViewModelAsync_DelegatesToIssueService()
        {
            // Arrange
            var mockDashboard = CreateMockDashboardService();
            var mockAssignment = CreateMockAssignmentService();
            var mockIssue = CreateMockIssueService();
            
            var expectedVM = new ReportIssueVM { TotalIssues = 10 };
            mockIssue.Setup(s => s.GetReportIssueViewModelAsync("collector1", "search", "Open", "High"))
                .ReturnsAsync(expectedVM)
                .Verifiable();

            var service = CreateService(mockDashboard, mockAssignment, mockIssue);

            // Act
            var result = await service.GetReportIssueViewModelAsync("collector1", "search", "Open", "High");

            // Assert
            Assert.Equal(expectedVM, result);
            mockIssue.Verify(s => s.GetReportIssueViewModelAsync("collector1", "search", "Open", "High"), Times.Once);
        }

        [Fact]
        public async Task SubmitIssueAsync_DelegatesToIssueService()
        {
            // Arrange
            var mockDashboard = CreateMockDashboardService();
            var mockAssignment = CreateMockAssignmentService();
            var mockIssue = CreateMockIssueService();
            
            var model = new ReportIssueVM { BinId = 1, IssueType = "Overflow" };
            mockIssue.Setup(s => s.SubmitIssueAsync(model, "collector1"))
                .ReturnsAsync(true)
                .Verifiable();

            var service = CreateService(mockDashboard, mockAssignment, mockIssue);

            // Act
            var result = await service.SubmitIssueAsync(model, "collector1");

            // Assert
            Assert.True(result);
            mockIssue.Verify(s => s.SubmitIssueAsync(model, "collector1"), Times.Once);
        }

        [Fact]
        public async Task StartIssueWorkAsync_DelegatesToIssueService()
        {
            // Arrange
            var mockDashboard = CreateMockDashboardService();
            var mockAssignment = CreateMockAssignmentService();
            var mockIssue = CreateMockIssueService();
            
            mockIssue.Setup(s => s.StartIssueWorkAsync(1, "collector1"))
                .ReturnsAsync("In Progress")
                .Verifiable();

            var service = CreateService(mockDashboard, mockAssignment, mockIssue);

            // Act
            var result = await service.StartIssueWorkAsync(1, "collector1");

            // Assert
            Assert.Equal("In Progress", result);
            mockIssue.Verify(s => s.StartIssueWorkAsync(1, "collector1"), Times.Once);
        }

        #endregion

        #region Multiple Service Interaction Tests

        [Fact]
        public async Task Service_CanCallMultipleDashboardMethods()
        {
            // Arrange
            var mockDashboard = CreateMockDashboardService();
            var mockAssignment = CreateMockAssignmentService();
            var mockIssue = CreateMockIssueService();
            
            var route = new CollectorRoute { RouteName = "Route 1" };
            var confirmVM = new CollectionConfirmationVM { StopId = 1 };

            mockDashboard.Setup(s => s.GetDailyRouteAsync("collector1"))
                .ReturnsAsync(route);
            mockDashboard.Setup(s => s.GetCollectionConfirmationAsync(1, "collector1"))
                .ReturnsAsync(confirmVM);

            var service = CreateService(mockDashboard, mockAssignment, mockIssue);

            // Act
            var routeResult = await service.GetDailyRouteAsync("collector1");
            var confirmResult = await service.GetCollectionConfirmationAsync(1, "collector1");

            // Assert
            Assert.Equal(route, routeResult);
            Assert.Equal(confirmVM, confirmResult);
            mockDashboard.Verify(s => s.GetDailyRouteAsync("collector1"), Times.Once);
            mockDashboard.Verify(s => s.GetCollectionConfirmationAsync(1, "collector1"), Times.Once);
        }

        [Fact]
        public async Task Service_CanCallMethodsFromDifferentServices()
        {
            // Arrange
            var mockDashboard = CreateMockDashboardService();
            var mockAssignment = CreateMockAssignmentService();
            var mockIssue = CreateMockIssueService();
            
            var route = new CollectorRoute { RouteName = "Route 1" };
            var assignmentVM = new RouteAssignmentSearchViewModel { TotalItems = 5 };
            var issueVM = new ReportIssueVM { TotalIssues = 10 };

            mockDashboard.Setup(s => s.GetDailyRouteAsync("collector1"))
                .ReturnsAsync(route);
            mockAssignment.Setup(s => s.GetRouteAssignmentsAsync(
                "collector1", null, null, null, null, 1, 10))
                .ReturnsAsync(assignmentVM);
            mockIssue.Setup(s => s.GetReportIssueViewModelAsync("collector1", null, null, null))
                .ReturnsAsync(issueVM);

            var service = CreateService(mockDashboard, mockAssignment, mockIssue);

            // Act
            var routeResult = await service.GetDailyRouteAsync("collector1");
            var assignmentResult = await service.GetRouteAssignmentsAsync("collector1", null, null, null, null, 1, 10);
            var issueResult = await service.GetReportIssueViewModelAsync("collector1", null, null, null);

            // Assert
            Assert.Equal(route, routeResult);
            Assert.Equal(assignmentVM, assignmentResult);
            Assert.Equal(issueVM, issueResult);
        }

        [Fact]
        public async Task Service_HandlesNullReturns_FromDashboardService()
        {
            // Arrange
            var mockDashboard = CreateMockDashboardService();
            var mockAssignment = CreateMockAssignmentService();
            var mockIssue = CreateMockIssueService();
            
            mockDashboard.Setup(s => s.GetCollectionConfirmationAsync(99, "collector1"))
                .ReturnsAsync((CollectionConfirmationVM?)null);

            var service = CreateService(mockDashboard, mockAssignment, mockIssue);

            // Act
            var result = await service.GetCollectionConfirmationAsync(99, "collector1");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task Service_HandlesNullReturns_FromAssignmentService()
        {
            // Arrange
            var mockDashboard = CreateMockDashboardService();
            var mockAssignment = CreateMockAssignmentService();
            var mockIssue = CreateMockIssueService();
            
            mockAssignment.Setup(s => s.GetRouteAssignmentDetailsAsync(99, "collector1"))
                .ReturnsAsync((RouteAssignmentDetailViewModel?)null);
            mockAssignment.Setup(s => s.GetNextStopsAsync("collector1", 10))
                .ReturnsAsync((NextStopsViewModel?)null);

            var service = CreateService(mockDashboard, mockAssignment, mockIssue);

            // Act
            var detailsResult = await service.GetRouteAssignmentDetailsAsync(99, "collector1");
            var nextStopsResult = await service.GetNextStopsAsync("collector1", 10);

            // Assert
            Assert.Null(detailsResult);
            Assert.Null(nextStopsResult);
        }

        [Fact]
        public async Task Service_HandlesFalseReturns_FromServices()
        {
            // Arrange
            var mockDashboard = CreateMockDashboardService();
            var mockAssignment = CreateMockAssignmentService();
            var mockIssue = CreateMockIssueService();
            
            var model = new CollectionConfirmationVM { StopId = 999 };
            mockDashboard.Setup(s => s.ConfirmCollectionAsync(model, "collector1"))
                .ReturnsAsync(false);

            var issueModel = new ReportIssueVM { BinId = 1 };
            mockIssue.Setup(s => s.SubmitIssueAsync(issueModel, "collector1"))
                .ReturnsAsync(false);

            var service = CreateService(mockDashboard, mockAssignment, mockIssue);

            // Act
            var confirmResult = await service.ConfirmCollectionAsync(model, "collector1");
            var issueResult = await service.SubmitIssueAsync(issueModel, "collector1");

            // Assert
            Assert.False(confirmResult);
            Assert.False(issueResult);
        }

        #endregion

        #region Service Construction Tests

        [Fact]
        public void CollectorService_CanBeConstructed_WithAllDependencies()
        {
            // Arrange
            var mockDashboard = CreateMockDashboardService();
            var mockAssignment = CreateMockAssignmentService();
            var mockIssue = CreateMockIssueService();

            // Act
            var service = new CollectorService(
                mockDashboard.Object,
                mockAssignment.Object,
                mockIssue.Object);

            // Assert
            Assert.NotNull(service);
        }

        [Fact]
        public void CollectorService_ImplementsICollectorService()
        {
            // Arrange
            var mockDashboard = CreateMockDashboardService();
            var mockAssignment = CreateMockAssignmentService();
            var mockIssue = CreateMockIssueService();

            // Act
            var service = new CollectorService(
                mockDashboard.Object,
                mockAssignment.Object,
                mockIssue.Object);

            // Assert
            Assert.IsType<ICollectorService>(service, exactMatch: false);
        }

        [Fact]
        public void CollectorService_ImplementsDashboardInterface()
        {
            // Arrange
            var mockDashboard = CreateMockDashboardService();
            var mockAssignment = CreateMockAssignmentService();
            var mockIssue = CreateMockIssueService();

            // Act
            var service = new CollectorService(
                mockDashboard.Object,
                mockAssignment.Object,
                mockIssue.Object);

            // Assert
            Assert.IsType<ICollectorDashboardService>(service, exactMatch: false);
        }

        [Fact]
        public void CollectorService_ImplementsAssignmentInterface()
        {
            // Arrange
            var mockDashboard = CreateMockDashboardService();
            var mockAssignment = CreateMockAssignmentService();
            var mockIssue = CreateMockIssueService();

            // Act
            var service = new CollectorService(
                mockDashboard.Object,
                mockAssignment.Object,
                mockIssue.Object);

            // Assert
            Assert.IsType<ICollectorAssignmentService>(service, exactMatch: false);
        }

        [Fact]
        public void CollectorService_ImplementsIssueInterface()
        {
            // Arrange
            var mockDashboard = CreateMockDashboardService();
            var mockAssignment = CreateMockAssignmentService();
            var mockIssue = CreateMockIssueService();

            // Act
            var service = new CollectorService(
                mockDashboard.Object,
                mockAssignment.Object,
                mockIssue.Object);

            // Assert
            Assert.IsType<ICollectorIssueService>(service, exactMatch: false);
        }

        #endregion
    }
}
