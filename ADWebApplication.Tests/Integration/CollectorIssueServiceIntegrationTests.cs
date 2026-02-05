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
    }
}
