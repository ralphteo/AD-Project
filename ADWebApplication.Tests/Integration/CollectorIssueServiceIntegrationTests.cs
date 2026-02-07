using ADWebApplication.Data;
using ADWebApplication.Models;
using ADWebApplication.Services.Collector;
using ADWebApplication.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ADWebApplication.Tests.Integration
{
    public class CollectorIssueServiceIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public CollectorIssueServiceIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private async Task RunInIsolatedContext(Func<In5niteDbContext, CollectorIssueService, Task> testAction)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<In5niteDbContext>();
            var service = new CollectorIssueService(db);

            // Use a transaction that we roll back to keep the DB clean for the next test
            using var transaction = await db.Database.BeginTransactionAsync();
            try
            {
                await testAction(db, service);
            }
            finally
            {
                await transaction.RollbackAsync();
            }
        }

        [Fact]
        public async Task SubmitIssueAsync_AddsIssueToStop_UsingPluralCollectionDetails()
        {
            await RunInIsolatedContext(async (db, service) =>
            {
                // Arrange
                var username = "testcollector";
                
                var region = new Region { RegionName = "TestRegion" };
                db.Regions.Add(region);
                await db.SaveChangesAsync();

                var bin = new CollectionBin { RegionId = region.RegionId, LocationName = "TestBin", BinCapacity = 100, BinStatus = "Active" };
                db.CollectionBins.Add(bin);
                await db.SaveChangesAsync();

                var assignment = new RouteAssignment { AssignedTo = username, AssignedBy = "admin", AssignedDateTime = DateTime.Now };
                db.RouteAssignments.Add(assignment);
                await db.SaveChangesAsync();

                var plan = new RoutePlan { AssignmentId = assignment.AssignmentId, PlannedDate = DateTime.Today, RouteStatus = "Pending" };
                db.RoutePlans.Add(plan);
                await db.SaveChangesAsync();

                var stop = new RouteStop 
                { 
                    RouteId = plan.RouteId,
                    BinId = bin.BinId,
                    PlannedCollectionTime = DateTimeOffset.Now,
                    StopSequence = 1
                };
                db.RouteStops.Add(stop);
                await db.SaveChangesAsync();

                var model = new ReportIssueVM 
                { 
                    BinId = bin.BinId, 
                    IssueType = "Overflow", 
                    Severity = "High", 
                    Description = "Test issue" 
                };

                // Act
                var result = await service.SubmitIssueAsync(model, username);

                // Assert
                Assert.True(result);
                var updatedStop = await db.RouteStops.FindAsync(stop.StopId);
                Assert.Contains("Overflow", updatedStop.IssueLog);
            });
        }

        [Fact]
        public async Task StartIssueWorkAsync_TransitionsStatus_FromOpenToInProgress()
        {
            await RunInIsolatedContext(async (db, service) =>
            {
                // Arrange
                var username = "worker";
                
                var region = new Region { RegionName = "WorkRegion" };
                db.Regions.Add(region);
                await db.SaveChangesAsync();

                var bin = new CollectionBin { RegionId = region.RegionId, LocationName = "WorkBin", BinCapacity = 100, BinStatus = "Active" };
                db.CollectionBins.Add(bin);
                await db.SaveChangesAsync();

                var assignment = new RouteAssignment { AssignedTo = username, AssignedBy = "admin", AssignedDateTime = DateTime.Now };
                db.RouteAssignments.Add(assignment);
                await db.SaveChangesAsync();

                var plan = new RoutePlan { AssignmentId = assignment.AssignmentId, PlannedDate = DateTime.Today, RouteStatus = "Pending" };
                db.RoutePlans.Add(plan);
                await db.SaveChangesAsync();

                var stop = new RouteStop
                {
                    RouteId = plan.RouteId,
                    BinId = bin.BinId,
                    IssueLog = "type: Damage; severity: High; status: Open; description: Broken lid",
                    PlannedCollectionTime = DateTimeOffset.Now
                };
                db.RouteStops.Add(stop);
                await db.SaveChangesAsync();

                // Act
                var newStatus = await service.StartIssueWorkAsync(stop.StopId, username);

                // Assert
                Assert.Equal("In Progress", newStatus);
                var updatedStop = await db.RouteStops.AsNoTracking().FirstOrDefaultAsync(s => s.StopId == stop.StopId);
                Assert.Contains("status: In Progress", updatedStop.IssueLog);
            });
        }

        [Fact]
        public async Task GetReportIssueViewModelAsync_FiltersByStatus()
        {
            await RunInIsolatedContext(async (db, service) =>
            {
                // Arrange
                var username = "admin_worker";
                
                var region = new Region { RegionName = "FilterRegion" };
                db.Regions.Add(region);
                await db.SaveChangesAsync();

                var bin = new CollectionBin { RegionId = region.RegionId, LocationName = "FilterBin", BinCapacity = 100, BinStatus = "Active" };
                db.CollectionBins.Add(bin);
                await db.SaveChangesAsync();

                var assignment = new RouteAssignment { AssignedTo = username, AssignedBy = "admin", AssignedDateTime = DateTime.Now };
                db.RouteAssignments.Add(assignment);
                await db.SaveChangesAsync();

                var plan = new RoutePlan { AssignmentId = assignment.AssignmentId, PlannedDate = DateTime.Today, RouteStatus = "Pending" };
                db.RoutePlans.Add(plan);
                await db.SaveChangesAsync();

                var stop = new RouteStop
                {
                    RouteId = plan.RouteId,
                    BinId = bin.BinId,
                    IssueLog = "type: Full; status: Resolved; description: Cleaned",
                    PlannedCollectionTime = DateTimeOffset.Now
                };
                db.RouteStops.Add(stop);
                await db.SaveChangesAsync();

                // Act
                var vm = await service.GetReportIssueViewModelAsync(username, null, "Resolved", null);

                // Assert
                Assert.NotEmpty(vm.Issues);
                Assert.All(vm.Issues, i => Assert.Equal("Resolved", i.Status));
            });
        }

        [Fact]
        public async Task SubmitIssueAsync_ReturnsFalse_WhenStopNotFound()
        {
            await RunInIsolatedContext(async (db, service) =>
            {
                // Arrange
                var model = new ReportIssueVM
                {
                    BinId = 999999,
                    IssueType = "Overflow",
                    Severity = "High",
                    Description = "Test issue"
                };

                // Act
                var result = await service.SubmitIssueAsync(model, "nonexistent_user");

                // Assert
                Assert.False(result);
            });
        }

        [Fact]
        public async Task SubmitIssueAsync_AppendsToExistingIssueLog()
        {
            await RunInIsolatedContext(async (db, service) =>
            {
                // Arrange
                var username = "collector2";
                
                var region = new Region { RegionName = "TestRegion2" };
                db.Regions.Add(region);
                await db.SaveChangesAsync();

                var bin = new CollectionBin { RegionId = region.RegionId, LocationName = "TestBin2", BinCapacity = 100, BinStatus = "Active" };
                db.CollectionBins.Add(bin);
                await db.SaveChangesAsync();

                var assignment = new RouteAssignment { AssignedTo = username, AssignedBy = "admin", AssignedDateTime = DateTime.Now };
                db.RouteAssignments.Add(assignment);
                await db.SaveChangesAsync();

                var plan = new RoutePlan { AssignmentId = assignment.AssignmentId, PlannedDate = DateTime.Today, RouteStatus = "Pending" };
                db.RoutePlans.Add(plan);
                await db.SaveChangesAsync();

                var stop = new RouteStop
                {
                    RouteId = plan.RouteId,
                    BinId = bin.BinId,
                    PlannedCollectionTime = DateTimeOffset.Now,
                    StopSequence = 1,
                    IssueLog = "type: Damage; severity: Low; status: Open; description: Small crack"
                };
                db.RouteStops.Add(stop);
                await db.SaveChangesAsync();

                var model = new ReportIssueVM
                {
                    BinId = bin.BinId,
                    IssueType = "Overflow",
                    Severity = "High",
                    Description = "Bin is full"
                };

                // Act
                var result = await service.SubmitIssueAsync(model, username);

                // Assert
                Assert.True(result);
                var updatedStop = await db.RouteStops.FindAsync(stop.StopId);
                Assert.Contains("Small crack", updatedStop.IssueLog);
                Assert.Contains("Bin is full", updatedStop.IssueLog);
                Assert.Contains("Overflow", updatedStop.IssueLog);
            });
        }

        [Fact]
        public async Task StartIssueWorkAsync_ReturnsNotFound_WhenStopDoesNotExist()
        {
            await RunInIsolatedContext(async (db, service) =>
            {
                // Act
                var result = await service.StartIssueWorkAsync(999999, "nonexistent_user");

                // Assert
                Assert.Equal("Issue not found for this route.", result);
            });
        }

        [Fact]
        public async Task StartIssueWorkAsync_ReturnsAlreadyResolved_WhenStatusIsResolved()
        {
            await RunInIsolatedContext(async (db, service) =>
            {
                // Arrange
                var username = "resolver";
                
                var region = new Region { RegionName = "ResolveRegion" };
                db.Regions.Add(region);
                await db.SaveChangesAsync();

                var bin = new CollectionBin { RegionId = region.RegionId, LocationName = "ResolvedBin", BinCapacity = 100, BinStatus = "Active" };
                db.CollectionBins.Add(bin);
                await db.SaveChangesAsync();

                var assignment = new RouteAssignment { AssignedTo = username, AssignedBy = "admin", AssignedDateTime = DateTime.Now };
                db.RouteAssignments.Add(assignment);
                await db.SaveChangesAsync();

                var plan = new RoutePlan { AssignmentId = assignment.AssignmentId, PlannedDate = DateTime.Today, RouteStatus = "Pending" };
                db.RoutePlans.Add(plan);
                await db.SaveChangesAsync();

                var stop = new RouteStop
                {
                    RouteId = plan.RouteId,
                    BinId = bin.BinId,
                    IssueLog = "type: Damage; severity: High; status: Resolved; description: Fixed",
                    PlannedCollectionTime = DateTimeOffset.Now
                };
                db.RouteStops.Add(stop);
                await db.SaveChangesAsync();

                // Act
                var result = await service.StartIssueWorkAsync(stop.StopId, username);

                // Assert
                Assert.Equal("Issue is already resolved.", result);
            });
        }

        [Fact]
        public async Task StartIssueWorkAsync_TransitionsToResolved_WhenInProgress()
        {
            await RunInIsolatedContext(async (db, service) =>
            {
                // Arrange
                var username = "finisher";
                
                var region = new Region { RegionName = "FinishRegion" };
                db.Regions.Add(region);
                await db.SaveChangesAsync();

                var bin = new CollectionBin { RegionId = region.RegionId, LocationName = "FinishBin", BinCapacity = 100, BinStatus = "Active" };
                db.CollectionBins.Add(bin);
                await db.SaveChangesAsync();

                var assignment = new RouteAssignment { AssignedTo = username, AssignedBy = "admin", AssignedDateTime = DateTime.Now };
                db.RouteAssignments.Add(assignment);
                await db.SaveChangesAsync();

                var plan = new RoutePlan { AssignmentId = assignment.AssignmentId, PlannedDate = DateTime.Today, RouteStatus = "Pending" };
                db.RoutePlans.Add(plan);
                await db.SaveChangesAsync();

                var stop = new RouteStop
                {
                    RouteId = plan.RouteId,
                    BinId = bin.BinId,
                    IssueLog = "type: Overflow; severity: Medium; status: In Progress; description: Working on it",
                    PlannedCollectionTime = DateTimeOffset.Now
                };
                db.RouteStops.Add(stop);
                await db.SaveChangesAsync();

                // Act
                var result = await service.StartIssueWorkAsync(stop.StopId, username);

                // Assert
                Assert.Equal("Resolved", result);
                var updatedStop = await db.RouteStops.AsNoTracking().FirstOrDefaultAsync(s => s.StopId == stop.StopId);
                Assert.Contains("status: Resolved", updatedStop.IssueLog);
            });
        }

        [Fact]
        public async Task StartIssueWorkAsync_UpdatesCollectionDetails_WhenIssueIsInDetails()
        {
            await RunInIsolatedContext(async (db, service) =>
            {
                // Arrange
                var username = "detailworker";
                
                var region = new Region { RegionName = "DetailRegion" };
                db.Regions.Add(region);
                await db.SaveChangesAsync();

                var bin = new CollectionBin { RegionId = region.RegionId, LocationName = "DetailBin", BinCapacity = 100, BinStatus = "Active" };
                db.CollectionBins.Add(bin);
                await db.SaveChangesAsync();

                var assignment = new RouteAssignment { AssignedTo = username, AssignedBy = "admin", AssignedDateTime = DateTime.Now };
                db.RouteAssignments.Add(assignment);
                await db.SaveChangesAsync();

                var plan = new RoutePlan { AssignmentId = assignment.AssignmentId, PlannedDate = DateTime.Today, RouteStatus = "Pending" };
                db.RoutePlans.Add(plan);
                await db.SaveChangesAsync();

                var stop = new RouteStop
                {
                    RouteId = plan.RouteId,
                    BinId = bin.BinId,
                    PlannedCollectionTime = DateTimeOffset.Now
                };
                db.RouteStops.Add(stop);
                await db.SaveChangesAsync();

                var detail = new CollectionDetails
                {
                    StopId = stop.StopId,
                    BinId = bin.BinId,
                    CurrentCollectionDateTime = DateTime.Now,
                    BinFillLevel = 50,
                    IssueLog = "type: Sensor; severity: Low; status: Open; description: Sensor offline"
                };
                db.CollectionDetails.Add(detail);
                await db.SaveChangesAsync();

                // Act
                var result = await service.StartIssueWorkAsync(stop.StopId, username);

                // Assert
                Assert.Equal("In Progress", result);
                var updatedDetail = await db.CollectionDetails.AsNoTracking().FirstOrDefaultAsync(d => d.CollectionId == detail.CollectionId);
                Assert.Contains("status: In Progress", updatedDetail.IssueLog);
            });
        }

        [Fact]
        public async Task GetReportIssueViewModelAsync_FiltersBySearch()
        {
            await RunInIsolatedContext(async (db, service) =>
            {
                // Arrange
                var username = "searcher";
                
                var region = new Region { RegionName = "SearchRegion" };
                db.Regions.Add(region);
                await db.SaveChangesAsync();

                var bin = new CollectionBin { RegionId = region.RegionId, LocationName = "SearchBin", BinCapacity = 100, BinStatus = "Active" };
                db.CollectionBins.Add(bin);
                await db.SaveChangesAsync();

                var assignment = new RouteAssignment { AssignedTo = username, AssignedBy = "admin", AssignedDateTime = DateTime.Now };
                db.RouteAssignments.Add(assignment);
                await db.SaveChangesAsync();

                var plan = new RoutePlan { AssignmentId = assignment.AssignmentId, PlannedDate = DateTime.Today, RouteStatus = "Pending" };
                db.RoutePlans.Add(plan);
                await db.SaveChangesAsync();

                var stop1 = new RouteStop
                {
                    RouteId = plan.RouteId,
                    BinId = bin.BinId,
                    IssueLog = "type: Overflow; severity: High; status: Open; description: Bin overflow problem",
                    PlannedCollectionTime = DateTimeOffset.Now
                };
                db.RouteStops.Add(stop1);
                await db.SaveChangesAsync();

                // Act
                var vm = await service.GetReportIssueViewModelAsync(username, "overflow", null, null);

                // Assert
                Assert.NotEmpty(vm.Issues);
                Assert.All(vm.Issues, i => Assert.True(
                    i.IssueType.ToLowerInvariant().Contains("overflow") ||
                    i.Description.ToLowerInvariant().Contains("overflow")));
            });
        }

        [Fact]
        public async Task GetReportIssueViewModelAsync_FiltersByPriority()
        {
            await RunInIsolatedContext(async (db, service) =>
            {
                // Arrange
                var username = "priority_user";
                
                var region = new Region { RegionName = "PriorityRegion" };
                db.Regions.Add(region);
                await db.SaveChangesAsync();

                var bin = new CollectionBin { RegionId = region.RegionId, LocationName = "PriorityBin", BinCapacity = 100, BinStatus = "Active" };
                db.CollectionBins.Add(bin);
                await db.SaveChangesAsync();

                var assignment = new RouteAssignment { AssignedTo = username, AssignedBy = "admin", AssignedDateTime = DateTime.Now };
                db.RouteAssignments.Add(assignment);
                await db.SaveChangesAsync();

                var plan = new RoutePlan { AssignmentId = assignment.AssignmentId, PlannedDate = DateTime.Today, RouteStatus = "Pending" };
                db.RoutePlans.Add(plan);
                await db.SaveChangesAsync();

                var stop1 = new RouteStop
                {
                    RouteId = plan.RouteId,
                    BinId = bin.BinId,
                    IssueLog = "type: Damage; severity: High; status: Open; description: Critical damage",
                    PlannedCollectionTime = DateTimeOffset.Now
                };
                db.RouteStops.Add(stop1);
                await db.SaveChangesAsync();

                // Act
                var vm = await service.GetReportIssueViewModelAsync(username, null, null, "High");

                // Assert
                Assert.NotEmpty(vm.Issues);
                Assert.All(vm.Issues, i => Assert.Equal("High", i.Severity));
            });
        }

        [Fact]
        public async Task GetReportIssueViewModelAsync_FiltersByMultipleParameters()
        {
            await RunInIsolatedContext(async (db, service) =>
            {
                // Arrange
                var username = "multi_filter";
                
                var region = new Region { RegionName = "MultiRegion" };
                db.Regions.Add(region);
                await db.SaveChangesAsync();

                var bin = new CollectionBin { RegionId = region.RegionId, LocationName = "MultiBin", BinCapacity = 100, BinStatus = "Active" };
                db.CollectionBins.Add(bin);
                await db.SaveChangesAsync();

                var assignment = new RouteAssignment { AssignedTo = username, AssignedBy = "admin", AssignedDateTime = DateTime.Now };
                db.RouteAssignments.Add(assignment);
                await db.SaveChangesAsync();

                var plan = new RoutePlan { AssignmentId = assignment.AssignmentId, PlannedDate = DateTime.Today, RouteStatus = "Pending" };
                db.RoutePlans.Add(plan);
                await db.SaveChangesAsync();

                var stop1 = new RouteStop
                {
                    RouteId = plan.RouteId,
                    BinId = bin.BinId,
                    IssueLog = "type: Damage; severity: High; status: Open; description: Serious damage issue",
                    PlannedCollectionTime = DateTimeOffset.Now
                };
                var stop2 = new RouteStop
                {
                    RouteId = plan.RouteId,
                    BinId = bin.BinId,
                    IssueLog = "type: Overflow; severity: Low; status: Open; description: Minor overflow",
                    PlannedCollectionTime = DateTimeOffset.Now.AddMinutes(-10)
                };
                db.RouteStops.AddRange(stop1, stop2);
                await db.SaveChangesAsync();

                // Act
                var vm = await service.GetReportIssueViewModelAsync(username, "damage", "Open", "High");

                // Assert
                Assert.Single(vm.Issues);
                Assert.Equal("Damage", vm.Issues.First().IssueType);
                Assert.Equal("High", vm.Issues.First().Severity);
                Assert.Equal("Open", vm.Issues.First().Status);
            });
        }

        [Fact]
        public async Task GetReportIssueViewModelAsync_ReturnsEmpty_WhenNoMatchingIssues()
        {
            await RunInIsolatedContext(async (db, service) =>
            {
                // Arrange
                var username = "empty_user";
                
                var region = new Region { RegionName = "EmptyRegion" };
                db.Regions.Add(region);
                await db.SaveChangesAsync();

                // Act
                var vm = await service.GetReportIssueViewModelAsync(username, null, null, null);

                // Assert
                Assert.Empty(vm.Issues);
                Assert.Equal(0, vm.TotalIssues);
                Assert.Equal(0, vm.OpenIssues);
                Assert.Equal(0, vm.InProgressIssues);
                Assert.Equal(0, vm.ResolvedIssues);
            });
        }

        [Fact]
        public async Task GetReportIssueViewModelAsync_ReturnsStatistics()
        {
            await RunInIsolatedContext(async (db, service) =>
            {
                // Arrange
                var username = "stats_user";
                
                var region = new Region { RegionName = "StatsRegion" };
                db.Regions.Add(region);
                await db.SaveChangesAsync();

                var bin = new CollectionBin { RegionId = region.RegionId, LocationName = "StatsBin", BinCapacity = 100, BinStatus = "Active" };
                db.CollectionBins.Add(bin);
                await db.SaveChangesAsync();

                var assignment = new RouteAssignment { AssignedTo = username, AssignedBy = "admin", AssignedDateTime = DateTime.Now };
                db.RouteAssignments.Add(assignment);
                await db.SaveChangesAsync();

                var plan = new RoutePlan { AssignmentId = assignment.AssignmentId, PlannedDate = DateTime.Today, RouteStatus = "Pending" };
                db.RoutePlans.Add(plan);
                await db.SaveChangesAsync();

                var stop1 = new RouteStop
                {
                    RouteId = plan.RouteId,
                    BinId = bin.BinId,
                    IssueLog = "type: Damage; status: Open; description: Issue 1",
                    PlannedCollectionTime = DateTimeOffset.Now
                };
                var stop2 = new RouteStop
                {
                    RouteId = plan.RouteId,
                    BinId = bin.BinId,
                    IssueLog = "type: Overflow; status: In Progress; description: Issue 2",
                    PlannedCollectionTime = DateTimeOffset.Now.AddMinutes(-10)
                };
                var stop3 = new RouteStop
                {
                    RouteId = plan.RouteId,
                    BinId = bin.BinId,
                    IssueLog = "type: Full; status: Resolved; description: Issue 3",
                    PlannedCollectionTime = DateTimeOffset.Now.AddMinutes(-20)
                };
                db.RouteStops.AddRange(stop1, stop2, stop3);
                await db.SaveChangesAsync();

                // Act
                var vm = await service.GetReportIssueViewModelAsync(username, null, null, null);

                // Assert
                Assert.Equal(3, vm.TotalIssues);
                Assert.Equal(1, vm.OpenIssues);
                Assert.Equal(1, vm.InProgressIssues);
                Assert.Equal(1, vm.ResolvedIssues);
            });
        }

        [Fact]
        public async Task GetReportIssueViewModelAsync_InfersSeverity_WhenNotExplicit()
        {
            await RunInIsolatedContext(async (db, service) =>
            {
                // Arrange
                var username = "infer_user";
                
                var region = new Region { RegionName = "InferRegion" };
                db.Regions.Add(region);
                await db.SaveChangesAsync();

                var bin = new CollectionBin { RegionId = region.RegionId, LocationName = "InferBin", BinCapacity = 100, BinStatus = "Active" };
                db.CollectionBins.Add(bin);
                await db.SaveChangesAsync();

                var assignment = new RouteAssignment { AssignedTo = username, AssignedBy = "admin", AssignedDateTime = DateTime.Now };
                db.RouteAssignments.Add(assignment);
                await db.SaveChangesAsync();

                var plan = new RoutePlan { AssignmentId = assignment.AssignmentId, PlannedDate = DateTime.Today, RouteStatus = "Pending" };
                db.RoutePlans.Add(plan);
                await db.SaveChangesAsync();

                var stop1 = new RouteStop
                {
                    RouteId = plan.RouteId,
                    BinId = bin.BinId,
                    IssueLog = "This is a high priority issue",
                    PlannedCollectionTime = DateTimeOffset.Now
                };
                var stop2 = new RouteStop
                {
                    RouteId = plan.RouteId,
                    BinId = bin.BinId,
                    IssueLog = "This is a low priority issue",
                    PlannedCollectionTime = DateTimeOffset.Now.AddMinutes(-10)
                };
                var stop3 = new RouteStop
                {
                    RouteId = plan.RouteId,
                    BinId = bin.BinId,
                    IssueLog = "Normal issue without priority keyword",
                    PlannedCollectionTime = DateTimeOffset.Now.AddMinutes(-20)
                };
                db.RouteStops.AddRange(stop1, stop2, stop3);
                await db.SaveChangesAsync();

                // Act
                var vm = await service.GetReportIssueViewModelAsync(username, null, null, null);

                // Assert
                Assert.Contains(vm.Issues, i => i.Severity == "High");
                Assert.Contains(vm.Issues, i => i.Severity == "Low");
                Assert.Contains(vm.Issues, i => i.Severity == "Medium");
            });
        }

        [Fact]
        public async Task GetReportIssueViewModelAsync_LoadsIssuesFromCollectionDetails()
        {
            await RunInIsolatedContext(async (db, service) =>
            {
                // Arrange
                var username = "details_user";
                
                var region = new Region { RegionName = "DetailsRegion" };
                db.Regions.Add(region);
                await db.SaveChangesAsync();

                var bin = new CollectionBin { RegionId = region.RegionId, LocationName = "DetailsBin", BinCapacity = 100, BinStatus = "Active" };
                db.CollectionBins.Add(bin);
                await db.SaveChangesAsync();

                var assignment = new RouteAssignment { AssignedTo = username, AssignedBy = "admin", AssignedDateTime = DateTime.Now };
                db.RouteAssignments.Add(assignment);
                await db.SaveChangesAsync();

                var plan = new RoutePlan { AssignmentId = assignment.AssignmentId, PlannedDate = DateTime.Today, RouteStatus = "Pending" };
                db.RoutePlans.Add(plan);
                await db.SaveChangesAsync();

                var stop = new RouteStop
                {
                    RouteId = plan.RouteId,
                    BinId = bin.BinId,
                    PlannedCollectionTime = DateTimeOffset.Now
                };
                db.RouteStops.Add(stop);
                await db.SaveChangesAsync();

                var detail = new CollectionDetails
                {
                    StopId = stop.StopId,
                    BinId = bin.BinId,
                    CurrentCollectionDateTime = DateTime.Now,
                    BinFillLevel = 80,
                    IssueLog = "type: Full; severity: Medium; status: Open; description: Bin near capacity"
                };
                db.CollectionDetails.Add(detail);
                await db.SaveChangesAsync();

                // Act
                var vm = await service.GetReportIssueViewModelAsync(username, null, null, null);

                // Assert
                Assert.NotEmpty(vm.Issues);
                Assert.Contains(vm.Issues, i => i.Description.Contains("near capacity"));
            });
        }

        [Fact]
        public async Task StartIssueWorkAsync_HandlesStatusWithoutExplicitField()
        {
            await RunInIsolatedContext(async (db, service) =>
            {
                // Arrange
                var username = "implicit_status";
                
                var region = new Region { RegionName = "ImplicitRegion" };
                db.Regions.Add(region);
                await db.SaveChangesAsync();

                var bin = new CollectionBin { RegionId = region.RegionId, LocationName = "ImplicitBin", BinCapacity = 100, BinStatus = "Active" };
                db.CollectionBins.Add(bin);
                await db.SaveChangesAsync();

                var assignment = new RouteAssignment { AssignedTo = username, AssignedBy = "admin", AssignedDateTime = DateTime.Now };
                db.RouteAssignments.Add(assignment);
                await db.SaveChangesAsync();

                var plan = new RoutePlan { AssignmentId = assignment.AssignmentId, PlannedDate = DateTime.Today, RouteStatus = "Pending" };
                db.RoutePlans.Add(plan);
                await db.SaveChangesAsync();

                var stop = new RouteStop
                {
                    RouteId = plan.RouteId,
                    BinId = bin.BinId,
                    IssueLog = "Bin is broken and needs repair",
                    PlannedCollectionTime = DateTimeOffset.Now
                };
                db.RouteStops.Add(stop);
                await db.SaveChangesAsync();

                // Act
                var result = await service.StartIssueWorkAsync(stop.StopId, username);

                // Assert
                Assert.Equal("In Progress", result);
                var updatedStop = await db.RouteStops.AsNoTracking().FirstOrDefaultAsync(s => s.StopId == stop.StopId);
                Assert.Contains("status: In Progress", updatedStop.IssueLog);
            });
        }
    }
}
