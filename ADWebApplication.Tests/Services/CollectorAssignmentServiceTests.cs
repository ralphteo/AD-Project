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
    public class CollectorAssignmentServiceTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<In5niteDbContext> _options;

        public CollectorAssignmentServiceTests()
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

        #region GetRouteAssignmentDetailsAsync Tests

        [Fact]
        public async Task GetRouteAssignmentDetailsAsync_ReturnsDetails_ForValidAssignment()
        {
            // Arrange
            using var context = CreateContext();
            var service = new CollectorAssignmentService(context);

            var region = new Region { RegionName = "North" };
            context.Regions.Add(region);
            await context.SaveChangesAsync();

            var bin = new CollectionBin { RegionId = region.RegionId, LocationName = "Test Bin", BinCapacity = 100, BinStatus = "Active" };
            context.CollectionBins.Add(bin);
            await context.SaveChangesAsync();

            var assignment = new RouteAssignment { AssignedTo = "collector1", AssignedBy = "admin", AssignedDateTime = DateTime.Now };
            context.RouteAssignments.Add(assignment);
            await context.SaveChangesAsync();

            var plan = new RoutePlan { AssignmentId = assignment.AssignmentId, PlannedDate = DateTime.Today, RouteStatus = "In Progress" };
            context.RoutePlans.Add(plan);
            await context.SaveChangesAsync();

            var stop = new RouteStop { RouteId = plan.RouteId, BinId = bin.BinId, PlannedCollectionTime = DateTimeOffset.Now, StopSequence = 1 };
            context.RouteStops.Add(stop);
            await context.SaveChangesAsync();

            // Act
            var result = await service.GetRouteAssignmentDetailsAsync(assignment.AssignmentId, "collector1");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(assignment.AssignmentId, result.AssignmentId);
            Assert.Equal(plan.RouteId, result.RouteId);
            Assert.Single(result.RouteStops);
        }

        [Fact]
        public async Task GetRouteAssignmentDetailsAsync_ReturnsNull_ForDifferentUser()
        {
            // Arrange
            using var context = CreateContext();
            var service = new CollectorAssignmentService(context);

            var assignment = new RouteAssignment { AssignedTo = "collector1", AssignedBy = "admin", AssignedDateTime = DateTime.Now };
            context.RouteAssignments.Add(assignment);
            await context.SaveChangesAsync();

            // Act
            var result = await service.GetRouteAssignmentDetailsAsync(assignment.AssignmentId, "collector2");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetRouteAssignmentDetailsAsync_IncludesCollectionStatus()
        {
            // Arrange
            using var context = CreateContext();
            var service = new CollectorAssignmentService(context);

            var region = new Region { RegionName = "South" };
            context.Regions.Add(region);
            await context.SaveChangesAsync();

            var bin = new CollectionBin { RegionId = region.RegionId, LocationName = "Test Bin", BinCapacity = 100, BinStatus = "Active" };
            context.CollectionBins.Add(bin);
            await context.SaveChangesAsync();

            var assignment = new RouteAssignment { AssignedTo = "collector1", AssignedBy = "admin", AssignedDateTime = DateTime.Now };
            context.RouteAssignments.Add(assignment);
            await context.SaveChangesAsync();

            var plan = new RoutePlan { AssignmentId = assignment.AssignmentId, PlannedDate = DateTime.Today, RouteStatus = "In Progress" };
            context.RoutePlans.Add(plan);
            await context.SaveChangesAsync();

            var stop = new RouteStop { RouteId = plan.RouteId, BinId = bin.BinId, PlannedCollectionTime = DateTimeOffset.Now, StopSequence = 1 };
            context.RouteStops.Add(stop);
            await context.SaveChangesAsync();

            var detail = new CollectionDetails
            {
                StopId = stop.StopId,
                BinId = bin.BinId,
                CollectionStatus = "Collected",
                BinFillLevel = 85,
                CurrentCollectionDateTime = DateTimeOffset.Now
            };
            context.CollectionDetails.Add(detail);
            await context.SaveChangesAsync();

            // Act
            var result = await service.GetRouteAssignmentDetailsAsync(assignment.AssignmentId, "collector1");

            // Assert
            Assert.NotNull(result);
            var stopItem = result.RouteStops.First();
            Assert.True(stopItem.IsCollected);
            Assert.Equal("Collected", stopItem.CollectionStatus);
            Assert.Equal(85, stopItem.BinFillLevel);
        }

        [Fact]
        public async Task GetRouteAssignmentDetailsAsync_OrdersStopsBySequence()
        {
            // Arrange
            using var context = CreateContext();
            var service = new CollectorAssignmentService(context);

            var region = new Region { RegionName = "East" };
            context.Regions.Add(region);
            await context.SaveChangesAsync();

            var bin = new CollectionBin { RegionId = region.RegionId, LocationName = "Test Bin", BinCapacity = 100, BinStatus = "Active" };
            context.CollectionBins.Add(bin);
            await context.SaveChangesAsync();

            var assignment = new RouteAssignment { AssignedTo = "collector1", AssignedBy = "admin", AssignedDateTime = DateTime.Now };
            context.RouteAssignments.Add(assignment);
            await context.SaveChangesAsync();

            var plan = new RoutePlan { AssignmentId = assignment.AssignmentId, PlannedDate = DateTime.Today, RouteStatus = "Pending" };
            context.RoutePlans.Add(plan);
            await context.SaveChangesAsync();

            var stop3 = new RouteStop { RouteId = plan.RouteId, BinId = bin.BinId, PlannedCollectionTime = DateTimeOffset.Now, StopSequence = 3 };
            var stop1 = new RouteStop { RouteId = plan.RouteId, BinId = bin.BinId, PlannedCollectionTime = DateTimeOffset.Now, StopSequence = 1 };
            var stop2 = new RouteStop { RouteId = plan.RouteId, BinId = bin.BinId, PlannedCollectionTime = DateTimeOffset.Now, StopSequence = 2 };
            context.RouteStops.AddRange(stop3, stop1, stop2);
            await context.SaveChangesAsync();

            // Act
            var result = await service.GetRouteAssignmentDetailsAsync(assignment.AssignmentId, "collector1");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.RouteStops.Count);
            Assert.Equal(1, result.RouteStops[0].StopSequence);
            Assert.Equal(2, result.RouteStops[1].StopSequence);
            Assert.Equal(3, result.RouteStops[2].StopSequence);
        }

        #endregion

        #region GetNextStopsAsync Tests

        [Fact]
        public async Task GetNextStopsAsync_ReturnsNextStops_ForTodaysRoute()
        {
            // Arrange
            using var context = CreateContext();
            var service = new CollectorAssignmentService(context);

            var region = new Region { RegionName = "North" };
            context.Regions.Add(region);
            await context.SaveChangesAsync();

            var bin = new CollectionBin { RegionId = region.RegionId, LocationName = "Test Bin", BinCapacity = 100, BinStatus = "Active" };
            context.CollectionBins.Add(bin);
            await context.SaveChangesAsync();

            var assignment = new RouteAssignment { AssignedTo = "collector1", AssignedBy = "admin", AssignedDateTime = DateTime.Now };
            context.RouteAssignments.Add(assignment);
            await context.SaveChangesAsync();

            var plan = new RoutePlan { AssignmentId = assignment.AssignmentId, PlannedDate = DateTime.Today, RouteStatus = "In Progress" };
            context.RoutePlans.Add(plan);
            await context.SaveChangesAsync();

            var stop1 = new RouteStop { RouteId = plan.RouteId, BinId = bin.BinId, PlannedCollectionTime = DateTimeOffset.Now, StopSequence = 1 };
            var stop2 = new RouteStop { RouteId = plan.RouteId, BinId = bin.BinId, PlannedCollectionTime = DateTimeOffset.Now, StopSequence = 2 };
            context.RouteStops.AddRange(stop1, stop2);
            await context.SaveChangesAsync();

            // Act
            var result = await service.GetNextStopsAsync("collector1", 10);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(plan.RouteId, result.RouteId);
            Assert.Equal(2, result.NextStops.Count);
            Assert.Equal(2, result.TotalPendingStops);
        }

        [Fact]
        public async Task GetNextStopsAsync_ReturnsNull_WhenNoTodaysAssignment()
        {
            // Arrange
            using var context = CreateContext();
            var service = new CollectorAssignmentService(context);

            // Act
            var result = await service.GetNextStopsAsync("collector1", 10);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetNextStopsAsync_ExcludesCollectedStops()
        {
            // Arrange
            using var context = CreateContext();
            var service = new CollectorAssignmentService(context);

            var region = new Region { RegionName = "South" };
            context.Regions.Add(region);
            await context.SaveChangesAsync();

            var bin = new CollectionBin { RegionId = region.RegionId, LocationName = "Test Bin", BinCapacity = 100, BinStatus = "Active" };
            context.CollectionBins.Add(bin);
            await context.SaveChangesAsync();

            var assignment = new RouteAssignment { AssignedTo = "collector1", AssignedBy = "admin", AssignedDateTime = DateTime.Now };
            context.RouteAssignments.Add(assignment);
            await context.SaveChangesAsync();

            var plan = new RoutePlan { AssignmentId = assignment.AssignmentId, PlannedDate = DateTime.Today, RouteStatus = "In Progress" };
            context.RoutePlans.Add(plan);
            await context.SaveChangesAsync();

            var stop1 = new RouteStop { RouteId = plan.RouteId, BinId = bin.BinId, PlannedCollectionTime = DateTimeOffset.Now, StopSequence = 1 };
            var stop2 = new RouteStop { RouteId = plan.RouteId, BinId = bin.BinId, PlannedCollectionTime = DateTimeOffset.Now, StopSequence = 2 };
            var stop3 = new RouteStop { RouteId = plan.RouteId, BinId = bin.BinId, PlannedCollectionTime = DateTimeOffset.Now, StopSequence = 3 };
            context.RouteStops.AddRange(stop1, stop2, stop3);
            await context.SaveChangesAsync();

            var detail = new CollectionDetails
            {
                StopId = stop1.StopId,
                BinId = bin.BinId,
                CollectionStatus = "Collected",
                CurrentCollectionDateTime = DateTimeOffset.Now
            };
            context.CollectionDetails.Add(detail);
            await context.SaveChangesAsync();

            // Act
            var result = await service.GetNextStopsAsync("collector1", 10);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.NextStops.Count);
            Assert.Equal(2, result.TotalPendingStops);
            Assert.DoesNotContain(result.NextStops, s => s.StopId == stop1.StopId);
        }

        [Fact]
        public async Task GetNextStopsAsync_RespectsTopParameter()
        {
            // Arrange
            using var context = CreateContext();
            var service = new CollectorAssignmentService(context);

            var region = new Region { RegionName = "East" };
            context.Regions.Add(region);
            await context.SaveChangesAsync();

            var bin = new CollectionBin { RegionId = region.RegionId, LocationName = "Test Bin", BinCapacity = 100, BinStatus = "Active" };
            context.CollectionBins.Add(bin);
            await context.SaveChangesAsync();

            var assignment = new RouteAssignment { AssignedTo = "collector1", AssignedBy = "admin", AssignedDateTime = DateTime.Now };
            context.RouteAssignments.Add(assignment);
            await context.SaveChangesAsync();

            var plan = new RoutePlan { AssignmentId = assignment.AssignmentId, PlannedDate = DateTime.Today, RouteStatus = "In Progress" };
            context.RoutePlans.Add(plan);
            await context.SaveChangesAsync();

            for (int i = 1; i <= 10; i++)
            {
                var stop = new RouteStop { RouteId = plan.RouteId, BinId = bin.BinId, PlannedCollectionTime = DateTimeOffset.Now, StopSequence = i };
                context.RouteStops.Add(stop);
            }
            await context.SaveChangesAsync();

            // Act
            var result = await service.GetNextStopsAsync("collector1", 3);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.NextStops.Count);
            Assert.Equal(10, result.TotalPendingStops);
        }

        [Fact]
        public async Task GetNextStopsAsync_ReturnsNull_ForCompletedRoute()
        {
            // Arrange
            using var context = CreateContext();
            var service = new CollectorAssignmentService(context);

            var region = new Region { RegionName = "West" };
            context.Regions.Add(region);
            await context.SaveChangesAsync();

            var bin = new CollectionBin { RegionId = region.RegionId, LocationName = "Test Bin", BinCapacity = 100, BinStatus = "Active" };
            context.CollectionBins.Add(bin);
            await context.SaveChangesAsync();

            var assignment = new RouteAssignment { AssignedTo = "collector1", AssignedBy = "admin", AssignedDateTime = DateTime.Now };
            context.RouteAssignments.Add(assignment);
            await context.SaveChangesAsync();

            var plan = new RoutePlan { AssignmentId = assignment.AssignmentId, PlannedDate = DateTime.Today, RouteStatus = "Completed" };
            context.RoutePlans.Add(plan);
            await context.SaveChangesAsync();

            var stop = new RouteStop { RouteId = plan.RouteId, BinId = bin.BinId, PlannedCollectionTime = DateTimeOffset.Now, StopSequence = 1 };
            context.RouteStops.Add(stop);
            await context.SaveChangesAsync();

            // Act
            var result = await service.GetNextStopsAsync("collector1", 10);

            // Assert
            Assert.Null(result);
        }

        #endregion
    }
}
