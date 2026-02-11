using Xunit;
using ADWebApplication.Data;
using ADWebApplication.Models;
using ADWebApplication.Services.Collector;
using ADWebApplication.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ADWebApplication.Tests.Services
{
    public class CollectorIssueServiceTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<In5niteDbContext> _options;

        public CollectorIssueServiceTests()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            _options = new DbContextOptionsBuilder<In5niteDbContext>()
                .UseSqlite(_connection)
                .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.AmbientTransactionWarning))
                .Options;

            using var context = new In5niteDbContext(_options);
            context.Database.EnsureCreated();
        }

        public void Dispose()
        {
            _connection.Close();
            _connection.Dispose();
            GC.SuppressFinalize(this);
        }

        private In5niteDbContext CreateContext() => new In5niteDbContext(_options);

        #region GetReportIssueViewModelAsync Tests

        [Fact]
        public async Task GetReportIssueViewModelAsync_ReturnsTodaysBins_ForCollector()
        {
            // Arrange
            using var context = CreateContext();
            var service = new CollectorIssueService(context);

            var region = new Region { RegionName = "North" };
            context.Regions.Add(region);
            await context.SaveChangesAsync();

            var bin = new CollectionBin 
            { 
                RegionId = region.RegionId, 
                LocationName = "Test Bin", 
                BinCapacity = 100, 
                BinStatus = "Active" 
            };
            context.CollectionBins.Add(bin);
            await context.SaveChangesAsync();

            var assignment = new RouteAssignment 
            { 
                AssignedTo = "collector1", 
                AssignedBy = "admin", 
                AssignedDateTime = DateTime.Now 
            };
            context.RouteAssignments.Add(assignment);
            await context.SaveChangesAsync();

            var plan = new RoutePlan 
            { 
                AssignmentId = assignment.AssignmentId, 
                PlannedDate = DateTime.Today, 
                RouteStatus = "In Progress" 
            };
            context.RoutePlans.Add(plan);
            await context.SaveChangesAsync();

            var stop = new RouteStop 
            { 
                RouteId = plan.RouteId, 
                BinId = bin.BinId, 
                PlannedCollectionTime = DateTimeOffset.Now, 
                StopSequence = 1 
            };
            context.RouteStops.Add(stop);
            await context.SaveChangesAsync();

            // Act
            var result = await service.GetReportIssueViewModelAsync("collector1", null, null, null);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.AvailableBins);
            Assert.Equal(bin.BinId, result.AvailableBins.First().BinId);
            Assert.Equal("Test Bin", result.AvailableBins.First().LocationName);
        }

        [Fact]
        public async Task GetReportIssueViewModelAsync_ReturnsIssues_FromRouteStops()
        {
            // Arrange
            using var context = CreateContext();
            var service = new CollectorIssueService(context);

            var region = new Region { RegionName = "South" };
            context.Regions.Add(region);
            await context.SaveChangesAsync();

            var bin = new CollectionBin 
            { 
                RegionId = region.RegionId, 
                LocationName = "Issue Bin", 
                LocationAddress = "123 Test St",
                BinCapacity = 100, 
                BinStatus = "Active" 
            };
            context.CollectionBins.Add(bin);
            await context.SaveChangesAsync();

            var assignment = new RouteAssignment 
            { 
                AssignedTo = "collector1", 
                AssignedBy = "admin", 
                AssignedDateTime = DateTime.Now 
            };
            context.RouteAssignments.Add(assignment);
            await context.SaveChangesAsync();

            var plan = new RoutePlan 
            { 
                AssignmentId = assignment.AssignmentId, 
                PlannedDate = DateTime.Today, 
                RouteStatus = "In Progress" 
            };
            context.RoutePlans.Add(plan);
            await context.SaveChangesAsync();

            var stop = new RouteStop 
            { 
                RouteId = plan.RouteId, 
                BinId = bin.BinId, 
                PlannedCollectionTime = DateTimeOffset.Now, 
                StopSequence = 1,
                IssueLog = "type: Damaged; severity: High; status: Open; description: Bin is broken"
            };
            context.RouteStops.Add(stop);
            await context.SaveChangesAsync();

            // Act
            var result = await service.GetReportIssueViewModelAsync("collector1", null, null, null);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Issues);
            Assert.Equal("Damaged", result.Issues.First().IssueType);
            Assert.Equal("High", result.Issues.First().Severity);
            Assert.Equal("Open", result.Issues.First().Status);
            Assert.Equal("Bin is broken", result.Issues.First().Description);
        }

        [Fact]
        public async Task GetReportIssueViewModelAsync_FiltersIssuesBySearch()
        {
            // Arrange
            using var context = CreateContext();
            var service = new CollectorIssueService(context);

            var bin = new CollectionBin 
            { 
                LocationName = "Bin A", 
                LocationAddress = "123 Main St",
                BinCapacity = 100, 
                BinStatus = "Active" 
            };
            context.CollectionBins.Add(bin);
            await context.SaveChangesAsync();

            var assignment = new RouteAssignment 
            { 
                AssignedTo = "collector1", 
                AssignedBy = "admin", 
                AssignedDateTime = DateTime.Now 
            };
            context.RouteAssignments.Add(assignment);
            await context.SaveChangesAsync();

            var plan = new RoutePlan 
            { 
                AssignmentId = assignment.AssignmentId, 
                PlannedDate = DateTime.Today, 
                RouteStatus = "In Progress" 
            };
            context.RoutePlans.Add(plan);
            await context.SaveChangesAsync();

            var stop = new RouteStop 
            { 
                RouteId = plan.RouteId, 
                BinId = bin.BinId, 
                PlannedCollectionTime = DateTimeOffset.Now, 
                StopSequence = 1,
                IssueLog = "type: Damaged; severity: High; status: Open; description: Broken lid"
            };
            context.RouteStops.Add(stop);
            await context.SaveChangesAsync();

            // Act - Search by description
            var result = await service.GetReportIssueViewModelAsync("collector1", "Broken", null, null);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Issues);
            Assert.Equal("Broken lid", result.Issues.First().Description);
        }

        [Fact]
        public async Task GetReportIssueViewModelAsync_FiltersIssuesByStatus()
        {
            // Arrange
            using var context = CreateContext();
            var service = new CollectorIssueService(context);

            var bin = new CollectionBin 
            { 
                LocationName = "Bin A", 
                BinCapacity = 100, 
                BinStatus = "Active" 
            };
            context.CollectionBins.Add(bin);
            await context.SaveChangesAsync();

            var assignment = new RouteAssignment 
            { 
                AssignedTo = "collector1", 
                AssignedBy = "admin", 
                AssignedDateTime = DateTime.Now 
            };
            context.RouteAssignments.Add(assignment);
            await context.SaveChangesAsync();

            var plan = new RoutePlan 
            { 
                AssignmentId = assignment.AssignmentId, 
                PlannedDate = DateTime.Today, 
                RouteStatus = "In Progress" 
            };
            context.RoutePlans.Add(plan);
            await context.SaveChangesAsync();

            var stop1 = new RouteStop 
            { 
                RouteId = plan.RouteId, 
                BinId = bin.BinId, 
                PlannedCollectionTime = DateTimeOffset.Now, 
                StopSequence = 1,
                IssueLog = "type: Damaged; severity: High; status: Open; description: Issue 1"
            };
            var stop2 = new RouteStop 
            { 
                RouteId = plan.RouteId, 
                BinId = bin.BinId, 
                PlannedCollectionTime = DateTimeOffset.Now, 
                StopSequence = 2,
                IssueLog = "type: Full; severity: Low; status: Resolved; description: Issue 2"
            };
            context.RouteStops.AddRange(stop1, stop2);
            await context.SaveChangesAsync();

            // Act - Filter by Open status
            var result = await service.GetReportIssueViewModelAsync("collector1", null, "Open", null);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Issues);
            Assert.Equal("Open", result.Issues.First().Status);
        }

        [Fact]
        public async Task GetReportIssueViewModelAsync_FiltersIssuesByPriority()
        {
            // Arrange
            using var context = CreateContext();
            var service = new CollectorIssueService(context);

            var bin = new CollectionBin 
            { 
                LocationName = "Bin A", 
                BinCapacity = 100, 
                BinStatus = "Active" 
            };
            context.CollectionBins.Add(bin);
            await context.SaveChangesAsync();

            var assignment = new RouteAssignment 
            { 
                AssignedTo = "collector1", 
                AssignedBy = "admin", 
                AssignedDateTime = DateTime.Now 
            };
            context.RouteAssignments.Add(assignment);
            await context.SaveChangesAsync();

            var plan = new RoutePlan 
            { 
                AssignmentId = assignment.AssignmentId, 
                PlannedDate = DateTime.Today, 
                RouteStatus = "In Progress" 
            };
            context.RoutePlans.Add(plan);
            await context.SaveChangesAsync();

            var stop1 = new RouteStop 
            { 
                RouteId = plan.RouteId, 
                BinId = bin.BinId, 
                PlannedCollectionTime = DateTimeOffset.Now, 
                StopSequence = 1,
                IssueLog = "type: Damaged; severity: High; status: Open; description: Critical issue"
            };
            var stop2 = new RouteStop 
            { 
                RouteId = plan.RouteId, 
                BinId = bin.BinId, 
                PlannedCollectionTime = DateTimeOffset.Now, 
                StopSequence = 2,
                IssueLog = "type: Minor; severity: Low; status: Open; description: Minor issue"
            };
            context.RouteStops.AddRange(stop1, stop2);
            await context.SaveChangesAsync();

            // Act - Filter by High priority
            var result = await service.GetReportIssueViewModelAsync("collector1", null, null, "High");

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Issues);
            Assert.Equal("High", result.Issues.First().Severity);
        }

        [Fact]
        public async Task GetReportIssueViewModelAsync_CalculatesIssueCounts()
        {
            // Arrange
            using var context = CreateContext();
            var service = new CollectorIssueService(context);

            var bin = new CollectionBin 
            { 
                LocationName = "Bin A", 
                BinCapacity = 100, 
                BinStatus = "Active" 
            };
            context.CollectionBins.Add(bin);
            await context.SaveChangesAsync();

            var assignment = new RouteAssignment 
            { 
                AssignedTo = "collector1", 
                AssignedBy = "admin", 
                AssignedDateTime = DateTime.Now 
            };
            context.RouteAssignments.Add(assignment);
            await context.SaveChangesAsync();

            var plan = new RoutePlan 
            { 
                AssignmentId = assignment.AssignmentId, 
                PlannedDate = DateTime.Today, 
                RouteStatus = "In Progress" 
            };
            context.RoutePlans.Add(plan);
            await context.SaveChangesAsync();

            var stop1 = new RouteStop 
            { 
                RouteId = plan.RouteId, 
                BinId = bin.BinId, 
                PlannedCollectionTime = DateTimeOffset.Now, 
                StopSequence = 1,
                IssueLog = "type: Damaged; severity: High; status: Open; description: Issue 1"
            };
            var stop2 = new RouteStop 
            { 
                RouteId = plan.RouteId, 
                BinId = bin.BinId, 
                PlannedCollectionTime = DateTimeOffset.Now, 
                StopSequence = 2,
                IssueLog = "type: Full; severity: Medium; status: In Progress; description: Issue 2"
            };
            var stop3 = new RouteStop 
            { 
                RouteId = plan.RouteId, 
                BinId = bin.BinId, 
                PlannedCollectionTime = DateTimeOffset.Now, 
                StopSequence = 3,
                IssueLog = "type: Other; severity: Low; status: Resolved; description: Issue 3"
            };
            context.RouteStops.AddRange(stop1, stop2, stop3);
            await context.SaveChangesAsync();

            // Act
            var result = await service.GetReportIssueViewModelAsync("collector1", null, null, null);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.TotalIssues);
            Assert.Equal(1, result.OpenIssues);
            Assert.Equal(1, result.InProgressIssues);
            Assert.Equal(1, result.ResolvedIssues);
        }

        #endregion

        #region SubmitIssueAsync Tests

        [Fact]
        public async Task SubmitIssueAsync_AddsNewIssue_ToRouteStop()
        {
            // Arrange
            using var context = CreateContext();
            var service = new CollectorIssueService(context);

            var bin = new CollectionBin 
            { 
                LocationName = "Bin A", 
                BinCapacity = 100, 
                BinStatus = "Active" 
            };
            context.CollectionBins.Add(bin);
            await context.SaveChangesAsync();

            var assignment = new RouteAssignment 
            { 
                AssignedTo = "collector1", 
                AssignedBy = "admin", 
                AssignedDateTime = DateTime.Now 
            };
            context.RouteAssignments.Add(assignment);
            await context.SaveChangesAsync();

            var plan = new RoutePlan 
            { 
                AssignmentId = assignment.AssignmentId, 
                PlannedDate = DateTime.Today, 
                RouteStatus = "In Progress" 
            };
            context.RoutePlans.Add(plan);
            await context.SaveChangesAsync();

            var stop = new RouteStop 
            { 
                RouteId = plan.RouteId, 
                BinId = bin.BinId, 
                PlannedCollectionTime = DateTimeOffset.Now, 
                StopSequence = 1
            };
            context.RouteStops.Add(stop);
            await context.SaveChangesAsync();

            var model = new ReportIssueVM
            {
                BinId = bin.BinId,
                IssueType = "Damaged",
                Severity = "High",
                Description = "Bin lid is broken"
            };

            // Act
            var result = await service.SubmitIssueAsync(model, "collector1");

            // Assert
            Assert.True(result);
            var updatedStop = await context.RouteStops.FindAsync(stop.StopId);
            Assert.NotNull(updatedStop);
            Assert.NotNull(updatedStop.IssueLog);
            Assert.Contains("Damaged", updatedStop.IssueLog);
            Assert.Contains("High", updatedStop.IssueLog);
            Assert.Contains("Bin lid is broken", updatedStop.IssueLog);
        }

        [Fact]
        public async Task SubmitIssueAsync_ReturnsFalse_WhenStopNotFound()
        {
            // Arrange
            using var context = CreateContext();
            var service = new CollectorIssueService(context);

            var model = new ReportIssueVM
            {
                BinId = 999,
                IssueType = "Damaged",
                Severity = "High",
                Description = "Test issue"
            };

            // Act
            var result = await service.SubmitIssueAsync(model, "collector1");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task SubmitIssueAsync_AppendsToExistingIssueLog()
        {
            // Arrange
            using var context = CreateContext();
            var service = new CollectorIssueService(context);

            var bin = new CollectionBin 
            { 
                LocationName = "Bin A", 
                BinCapacity = 100, 
                BinStatus = "Active" 
            };
            context.CollectionBins.Add(bin);
            await context.SaveChangesAsync();

            var assignment = new RouteAssignment 
            { 
                AssignedTo = "collector1", 
                AssignedBy = "admin", 
                AssignedDateTime = DateTime.Now 
            };
            context.RouteAssignments.Add(assignment);
            await context.SaveChangesAsync();

            var plan = new RoutePlan 
            { 
                AssignmentId = assignment.AssignmentId, 
                PlannedDate = DateTime.Today, 
                RouteStatus = "In Progress" 
            };
            context.RoutePlans.Add(plan);
            await context.SaveChangesAsync();

            var stop = new RouteStop 
            { 
                RouteId = plan.RouteId, 
                BinId = bin.BinId, 
                PlannedCollectionTime = DateTimeOffset.Now, 
                StopSequence = 1,
                IssueLog = "type: Full; severity: Low; status: Open; description: Bin is full"
            };
            context.RouteStops.Add(stop);
            await context.SaveChangesAsync();

            var model = new ReportIssueVM
            {
                BinId = bin.BinId,
                IssueType = "Damaged",
                Severity = "High",
                Description = "Lid broken"
            };

            // Act
            var result = await service.SubmitIssueAsync(model, "collector1");

            // Assert
            Assert.True(result);
            var updatedStop = await context.RouteStops.FindAsync(stop.StopId);
            Assert.NotNull(updatedStop);
            Assert.Contains("Bin is full", updatedStop.IssueLog);
            Assert.Contains("Lid broken", updatedStop.IssueLog);
        }

        #endregion

        #region StartIssueWorkAsync Tests

        [Fact]
        public async Task StartIssueWorkAsync_ChangesStatusToInProgress()
        {
            // Arrange
            using var context = CreateContext();
            var service = new CollectorIssueService(context);

            var bin = new CollectionBin 
            { 
                LocationName = "Bin A", 
                BinCapacity = 100, 
                BinStatus = "Active" 
            };
            context.CollectionBins.Add(bin);
            await context.SaveChangesAsync();

            var assignment = new RouteAssignment 
            { 
                AssignedTo = "collector1", 
                AssignedBy = "admin", 
                AssignedDateTime = DateTime.Now 
            };
            context.RouteAssignments.Add(assignment);
            await context.SaveChangesAsync();

            var plan = new RoutePlan 
            { 
                AssignmentId = assignment.AssignmentId, 
                PlannedDate = DateTime.Today, 
                RouteStatus = "In Progress" 
            };
            context.RoutePlans.Add(plan);
            await context.SaveChangesAsync();

            var stop = new RouteStop 
            { 
                RouteId = plan.RouteId, 
                BinId = bin.BinId, 
                PlannedCollectionTime = DateTimeOffset.Now, 
                StopSequence = 1,
                IssueLog = "type: Damaged; severity: High; status: Open; description: Bin broken"
            };
            context.RouteStops.Add(stop);
            await context.SaveChangesAsync();

            // Act
            var result = await service.StartIssueWorkAsync(stop.StopId, "collector1");

            // Assert
            Assert.Equal("In Progress", result);
            var updatedStop = await context.RouteStops.FindAsync(stop.StopId);
            Assert.NotNull(updatedStop);
            Assert.Contains("In Progress", updatedStop.IssueLog!);
        }

        [Fact]
        public async Task StartIssueWorkAsync_ChangesInProgressToResolved()
        {
            // Arrange
            using var context = CreateContext();
            var service = new CollectorIssueService(context);

            var bin = new CollectionBin 
            { 
                LocationName = "Bin A", 
                BinCapacity = 100, 
                BinStatus = "Active" 
            };
            context.CollectionBins.Add(bin);
            await context.SaveChangesAsync();

            var assignment = new RouteAssignment 
            { 
                AssignedTo = "collector1", 
                AssignedBy = "admin", 
                AssignedDateTime = DateTime.Now 
            };
            context.RouteAssignments.Add(assignment);
            await context.SaveChangesAsync();

            var plan = new RoutePlan 
            { 
                AssignmentId = assignment.AssignmentId, 
                PlannedDate = DateTime.Today, 
                RouteStatus = "In Progress" 
            };
            context.RoutePlans.Add(plan);
            await context.SaveChangesAsync();

            var stop = new RouteStop 
            { 
                RouteId = plan.RouteId, 
                BinId = bin.BinId, 
                PlannedCollectionTime = DateTimeOffset.Now, 
                StopSequence = 1,
                IssueLog = "type: Damaged; severity: High; status: In Progress; description: Bin broken"
            };
            context.RouteStops.Add(stop);
            await context.SaveChangesAsync();

            // Act
            var result = await service.StartIssueWorkAsync(stop.StopId, "collector1");

            // Assert
            Assert.Equal("Resolved", result);
            var updatedStop = await context.RouteStops.FindAsync(stop.StopId);
            Assert.NotNull(updatedStop);
            Assert.Contains("Resolved", updatedStop.IssueLog!);
        }

        [Fact]
        public async Task StartIssueWorkAsync_ReturnsMessage_WhenAlreadyResolved()
        {
            // Arrange
            using var context = CreateContext();
            var service = new CollectorIssueService(context);

            var bin = new CollectionBin 
            { 
                LocationName = "Bin A", 
                BinCapacity = 100, 
                BinStatus = "Active" 
            };
            context.CollectionBins.Add(bin);
            await context.SaveChangesAsync();

            var assignment = new RouteAssignment 
            { 
                AssignedTo = "collector1", 
                AssignedBy = "admin", 
                AssignedDateTime = DateTime.Now 
            };
            context.RouteAssignments.Add(assignment);
            await context.SaveChangesAsync();

            var plan = new RoutePlan 
            { 
                AssignmentId = assignment.AssignmentId, 
                PlannedDate = DateTime.Today, 
                RouteStatus = "In Progress" 
            };
            context.RoutePlans.Add(plan);
            await context.SaveChangesAsync();

            var stop = new RouteStop 
            { 
                RouteId = plan.RouteId, 
                BinId = bin.BinId, 
                PlannedCollectionTime = DateTimeOffset.Now, 
                StopSequence = 1,
                IssueLog = "type: Damaged; severity: High; status: Resolved; description: Bin fixed"
            };
            context.RouteStops.Add(stop);
            await context.SaveChangesAsync();

            // Act
            var result = await service.StartIssueWorkAsync(stop.StopId, "collector1");

            // Assert
            Assert.Equal("Issue is already resolved.", result);
        }

        [Fact]
        public async Task StartIssueWorkAsync_ReturnsMessage_WhenStopNotFound()
        {
            // Arrange
            using var context = CreateContext();
            var service = new CollectorIssueService(context);

            // Act
            var result = await service.StartIssueWorkAsync(999, "collector1");

            // Assert
            Assert.Equal("Issue not found for this route.", result);
        }

        [Fact]
        public async Task StartIssueWorkAsync_UpdatesCollectionDetailsIssueLog()
        {
            // Arrange
            using var context = CreateContext();
            var service = new CollectorIssueService(context);

            var bin = new CollectionBin 
            { 
                LocationName = "Bin A", 
                BinCapacity = 100, 
                BinStatus = "Active" 
            };
            context.CollectionBins.Add(bin);
            await context.SaveChangesAsync();

            var assignment = new RouteAssignment 
            { 
                AssignedTo = "collector1", 
                AssignedBy = "admin", 
                AssignedDateTime = DateTime.Now 
            };
            context.RouteAssignments.Add(assignment);
            await context.SaveChangesAsync();

            var plan = new RoutePlan 
            { 
                AssignmentId = assignment.AssignmentId, 
                PlannedDate = DateTime.Today, 
                RouteStatus = "In Progress" 
            };
            context.RoutePlans.Add(plan);
            await context.SaveChangesAsync();

            var stop = new RouteStop 
            { 
                RouteId = plan.RouteId, 
                BinId = bin.BinId, 
                PlannedCollectionTime = DateTimeOffset.Now, 
                StopSequence = 1
            };
            context.RouteStops.Add(stop);
            await context.SaveChangesAsync();

            var detail = new CollectionDetails
            {
                StopId = stop.StopId,
                BinId = bin.BinId,
                CollectionStatus = "Collected",
                CurrentCollectionDateTime = DateTimeOffset.Now,
                IssueLog = "type: Damaged; severity: High; status: Open; description: Broken"
            };
            context.CollectionDetails.Add(detail);
            await context.SaveChangesAsync();

            // Act
            var result = await service.StartIssueWorkAsync(stop.StopId, "collector1");

            // Assert
            Assert.Equal("In Progress", result);
            var updatedDetail = await context.CollectionDetails.FindAsync(detail.CollectionId);
            Assert.NotNull(updatedDetail);
            Assert.Contains("In Progress", updatedDetail.IssueLog!);
        }

        #endregion

        #region Edge Cases and Integration Tests

        [Fact]
        public async Task GetReportIssueViewModelAsync_ReturnsEmptyBins_WhenNoTodaysRoute()
        {
            // Arrange
            using var context = CreateContext();
            var service = new CollectorIssueService(context);

            var bin = new CollectionBin 
            { 
                LocationName = "Bin A", 
                BinCapacity = 100, 
                BinStatus = "Active" 
            };
            context.CollectionBins.Add(bin);
            await context.SaveChangesAsync();

            var assignment = new RouteAssignment 
            { 
                AssignedTo = "collector1", 
                AssignedBy = "admin", 
                AssignedDateTime = DateTime.Now 
            };
            context.RouteAssignments.Add(assignment);
            await context.SaveChangesAsync();

            var plan = new RoutePlan 
            { 
                AssignmentId = assignment.AssignmentId, 
                PlannedDate = DateTime.Today.AddDays(-1), // Yesterday
                RouteStatus = "Completed" 
            };
            context.RoutePlans.Add(plan);
            await context.SaveChangesAsync();

            var stop = new RouteStop 
            { 
                RouteId = plan.RouteId, 
                BinId = bin.BinId, 
                PlannedCollectionTime = DateTimeOffset.Now, 
                StopSequence = 1
            };
            context.RouteStops.Add(stop);
            await context.SaveChangesAsync();

            // Act
            var result = await service.GetReportIssueViewModelAsync("collector1", null, null, null);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.AvailableBins);
        }

        [Fact]
        public async Task SubmitIssueAsync_ReturnsFalse_ForDifferentCollector()
        {
            // Arrange
            using var context = CreateContext();
            var service = new CollectorIssueService(context);

            var bin = new CollectionBin 
            { 
                LocationName = "Bin A", 
                BinCapacity = 100, 
                BinStatus = "Active" 
            };
            context.CollectionBins.Add(bin);
            await context.SaveChangesAsync();

            var assignment = new RouteAssignment 
            { 
                AssignedTo = "collector1", 
                AssignedBy = "admin", 
                AssignedDateTime = DateTime.Now 
            };
            context.RouteAssignments.Add(assignment);
            await context.SaveChangesAsync();

            var plan = new RoutePlan 
            { 
                AssignmentId = assignment.AssignmentId, 
                PlannedDate = DateTime.Today, 
                RouteStatus = "In Progress" 
            };
            context.RoutePlans.Add(plan);
            await context.SaveChangesAsync();

            var stop = new RouteStop 
            { 
                RouteId = plan.RouteId, 
                BinId = bin.BinId, 
                PlannedCollectionTime = DateTimeOffset.Now, 
                StopSequence = 1
            };
            context.RouteStops.Add(stop);
            await context.SaveChangesAsync();

            var model = new ReportIssueVM
            {
                BinId = bin.BinId,
                IssueType = "Damaged",
                Severity = "High",
                Description = "Test"
            };

            // Act - Try to submit as different collector
            var result = await service.SubmitIssueAsync(model, "collector2");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task GetReportIssueViewModelAsync_HandlesMultipleIssuesOnSameBin()
        {
            // Arrange
            using var context = CreateContext();
            var service = new CollectorIssueService(context);

            var bin = new CollectionBin 
            { 
                LocationName = "Bin A", 
                BinCapacity = 100, 
                BinStatus = "Active" 
            };
            context.CollectionBins.Add(bin);
            await context.SaveChangesAsync();

            var assignment = new RouteAssignment 
            { 
                AssignedTo = "collector1", 
                AssignedBy = "admin", 
                AssignedDateTime = DateTime.Now 
            };
            context.RouteAssignments.Add(assignment);
            await context.SaveChangesAsync();

            var plan1 = new RoutePlan 
            { 
                AssignmentId = assignment.AssignmentId, 
                PlannedDate = DateTime.Today, 
                RouteStatus = "In Progress" 
            };
            var plan2 = new RoutePlan 
            { 
                AssignmentId = assignment.AssignmentId, 
                PlannedDate = DateTime.Today.AddDays(-1), 
                RouteStatus = "Completed" 
            };
            context.RoutePlans.AddRange(plan1, plan2);
            await context.SaveChangesAsync();

            var stop1 = new RouteStop 
            { 
                RouteId = plan1.RouteId, 
                BinId = bin.BinId, 
                PlannedCollectionTime = DateTimeOffset.Now, 
                StopSequence = 1,
                IssueLog = "type: Damaged; severity: High; status: Open; description: Issue 1"
            };
            var stop2 = new RouteStop 
            { 
                RouteId = plan2.RouteId, 
                BinId = bin.BinId, 
                PlannedCollectionTime = DateTimeOffset.Now.AddDays(-1), 
                StopSequence = 1,
                IssueLog = "type: Full; severity: Low; status: Resolved; description: Issue 2"
            };
            context.RouteStops.AddRange(stop1, stop2);
            await context.SaveChangesAsync();

            // Act
            var result = await service.GetReportIssueViewModelAsync("collector1", null, null, null);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Issues.Count);
        }

        #endregion
    }
}
