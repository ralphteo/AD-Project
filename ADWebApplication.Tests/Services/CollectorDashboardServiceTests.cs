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
    public class CollectorDashboardServiceTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<In5niteDbContext> _options;

        public CollectorDashboardServiceTests()
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

        #region GetDailyRouteAsync Tests

        [Fact]
        public async Task GetDailyRouteAsync_ReturnsRoute_ForTodaysAssignment()
        {
            // Arrange
            using var context = CreateContext();
            var service = new CollectorDashboardService(context);

            var region = new Region { RegionName = "North" };
            context.Regions.Add(region);
            await context.SaveChangesAsync();

            var bin = new CollectionBin 
            { 
                RegionId = region.RegionId, 
                LocationName = "Test Location",
                LocationAddress = "123 Test St",
                Latitude = 1.3521,
                Longitude = 103.8198,
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
            var result = await service.GetDailyRouteAsync("collector1");

            // Assert
            Assert.NotNull(result);
            Assert.Equal($"Route #{plan.RouteId}", result.RouteName);
            Assert.Equal("North", result.Zone);
            Assert.Equal("In Progress", result.Status);
            Assert.Single(result.CollectionPoints);
        }

        [Fact]
        public async Task GetDailyRouteAsync_ReturnsEmptyRoute_WhenNoAssignment()
        {
            // Arrange
            using var context = CreateContext();
            var service = new CollectorDashboardService(context);

            // Act
            var result = await service.GetDailyRouteAsync("collector1");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("No Route Assigned", result.RouteName);
            Assert.Equal("-", result.Zone);
            Assert.Equal("Pending", result.Status);
            Assert.Empty(result.CollectionPoints);
        }

        [Fact]
        public async Task GetDailyRouteAsync_OrdersStopsBySequence()
        {
            // Arrange
            using var context = CreateContext();
            var service = new CollectorDashboardService(context);

            var region = new Region { RegionName = "South" };
            context.Regions.Add(region);
            await context.SaveChangesAsync();

            var bin = new CollectionBin 
            { 
                RegionId = region.RegionId, 
                LocationName = "Test Location",
                BinCapacity = 100,
                BinStatus = "Active"
            };
            context.CollectionBins.Add(bin);
            await context.SaveChangesAsync();

            var assignment = new RouteAssignment { AssignedTo = "collector1", AssignedBy = "admin", AssignedDateTime = DateTime.Now };
            context.RouteAssignments.Add(assignment);
            await context.SaveChangesAsync();

            var plan = new RoutePlan { AssignmentId = assignment.AssignmentId, PlannedDate = DateTime.Today, RouteStatus = "Pending" };
            context.RoutePlans.Add(plan);
            await context.SaveChangesAsync();

            var stop3 = new RouteStop { RouteId = plan.RouteId, BinId = bin.BinId, PlannedCollectionTime = DateTimeOffset.Now.AddHours(3), StopSequence = 3 };
            var stop1 = new RouteStop { RouteId = plan.RouteId, BinId = bin.BinId, PlannedCollectionTime = DateTimeOffset.Now.AddHours(1), StopSequence = 1 };
            var stop2 = new RouteStop { RouteId = plan.RouteId, BinId = bin.BinId, PlannedCollectionTime = DateTimeOffset.Now.AddHours(2), StopSequence = 2 };
            context.RouteStops.AddRange(stop3, stop1, stop2);
            await context.SaveChangesAsync();

            // Act
            var result = await service.GetDailyRouteAsync("collector1");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.CollectionPoints.Count);
            Assert.Equal(stop1.StopId, result.CollectionPoints[0].StopId);
            Assert.Equal(stop2.StopId, result.CollectionPoints[1].StopId);
            Assert.Equal(stop3.StopId, result.CollectionPoints[2].StopId);
        }

        [Fact]
        public async Task GetDailyRouteAsync_IncludesCollectionStatus()
        {
            // Arrange
            using var context = CreateContext();
            var service = new CollectorDashboardService(context);

            var region = new Region { RegionName = "East" };
            context.Regions.Add(region);
            await context.SaveChangesAsync();

            var bin = new CollectionBin { RegionId = region.RegionId, LocationName = "Test Location", BinCapacity = 100, BinStatus = "Active" };
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
                BinFillLevel = 75,
                CurrentCollectionDateTime = DateTimeOffset.Now
            };
            context.CollectionDetails.Add(detail);
            await context.SaveChangesAsync();

            // Act
            var result = await service.GetDailyRouteAsync("collector1");

            // Assert
            Assert.NotNull(result);
            var point = result.CollectionPoints.First();
            Assert.Equal("Collected", point.Status);
            Assert.Equal(75, point.CurrentFillLevel);
            Assert.NotNull(point.CollectedAt);
        }

        [Fact]
        public async Task GetDailyRouteAsync_CalculatesEstimatedTime()
        {
            // Arrange
            using var context = CreateContext();
            var service = new CollectorDashboardService(context);

            var region = new Region { RegionName = "West" };
            context.Regions.Add(region);
            await context.SaveChangesAsync();

            var bin = new CollectionBin { RegionId = region.RegionId, LocationName = "Test Location", BinCapacity = 100, BinStatus = "Active" };
            context.CollectionBins.Add(bin);
            await context.SaveChangesAsync();

            var assignment = new RouteAssignment { AssignedTo = "collector1", AssignedBy = "admin", AssignedDateTime = DateTime.Now };
            context.RouteAssignments.Add(assignment);
            await context.SaveChangesAsync();

            var plan = new RoutePlan { AssignmentId = assignment.AssignmentId, PlannedDate = DateTime.Today, RouteStatus = "Pending" };
            context.RoutePlans.Add(plan);
            await context.SaveChangesAsync();

            var baseTime = DateTimeOffset.Now;
            var stop1 = new RouteStop { RouteId = plan.RouteId, BinId = bin.BinId, PlannedCollectionTime = baseTime, StopSequence = 1 };
            var stop2 = new RouteStop { RouteId = plan.RouteId, BinId = bin.BinId, PlannedCollectionTime = baseTime.AddMinutes(30), StopSequence = 2 };
            context.RouteStops.AddRange(stop1, stop2);
            await context.SaveChangesAsync();

            // Act
            var result = await service.GetDailyRouteAsync("collector1");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.CollectionPoints[0].EstimatedTimeMins); // First stop has 0
            Assert.Equal(30, result.CollectionPoints[1].EstimatedTimeMins); // 30 minutes from first
        }

        [Fact]
        public async Task GetDailyRouteAsync_GeneratesBinLabels()
        {
            // Arrange
            using var context = CreateContext();
            var service = new CollectorDashboardService(context);

            var region = new Region { RegionName = "Central" };
            context.Regions.Add(region);
            await context.SaveChangesAsync();

            var bin = new CollectionBin { RegionId = region.RegionId, LocationName = "Test Location", BinCapacity = 100, BinStatus = "Active" };
            context.CollectionBins.Add(bin);
            await context.SaveChangesAsync();

            var assignment = new RouteAssignment { AssignedTo = "collector1", AssignedBy = "admin", AssignedDateTime = DateTime.Now };
            context.RouteAssignments.Add(assignment);
            await context.SaveChangesAsync();

            var plan = new RoutePlan { AssignmentId = assignment.AssignmentId, PlannedDate = DateTime.Today, RouteStatus = "Pending" };
            context.RoutePlans.Add(plan);
            await context.SaveChangesAsync();

            var stop = new RouteStop { RouteId = plan.RouteId, BinId = bin.BinId, PlannedCollectionTime = DateTimeOffset.Now, StopSequence = 1 };
            context.RouteStops.Add(stop);
            await context.SaveChangesAsync();

            // Act
            var result = await service.GetDailyRouteAsync("collector1");

            // Assert
            Assert.NotNull(result);
            var point = result.CollectionPoints.First();
            Assert.StartsWith("B", point.PointId);
        }

        #endregion

        #region GetCollectionConfirmationAsync Tests

        [Fact]
        public async Task GetCollectionConfirmationAsync_ReturnsViewModel_ForValidStop()
        {
            // Arrange
            using var context = CreateContext();
            var service = new CollectorDashboardService(context);

            var region = new Region { RegionName = "North" };
            context.Regions.Add(region);
            await context.SaveChangesAsync();

            var bin = new CollectionBin 
            { 
                RegionId = region.RegionId, 
                LocationName = "Test Location",
                LocationAddress = "123 Test St",
                BinCapacity = 100,
                BinStatus = "Active"
            };
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
            var result = await service.GetCollectionConfirmationAsync(stop.StopId, "collector1");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(stop.StopId, result.StopId);
            Assert.Equal("Test Location", result.LocationName);
            Assert.Equal("123 Test St", result.Address);
            Assert.Equal("North", result.Zone);
        }

        [Fact]
        public async Task GetCollectionConfirmationAsync_ReturnsNull_ForDifferentUser()
        {
            // Arrange
            using var context = CreateContext();
            var service = new CollectorDashboardService(context);

            var region = new Region { RegionName = "South" };
            context.Regions.Add(region);
            await context.SaveChangesAsync();

            var bin = new CollectionBin { RegionId = region.RegionId, LocationName = "Test Location", BinCapacity = 100, BinStatus = "Active" };
            context.CollectionBins.Add(bin);
            await context.SaveChangesAsync();

            var assignment = new RouteAssignment { AssignedTo = "collector1", AssignedBy = "admin", AssignedDateTime = DateTime.Now };
            context.RouteAssignments.Add(assignment);
            await context.SaveChangesAsync();

            var plan = new RoutePlan { AssignmentId = assignment.AssignmentId, PlannedDate = DateTime.Today, RouteStatus = "Pending" };
            context.RoutePlans.Add(plan);
            await context.SaveChangesAsync();

            var stop = new RouteStop { RouteId = plan.RouteId, BinId = bin.BinId, PlannedCollectionTime = DateTimeOffset.Now, StopSequence = 1 };
            context.RouteStops.Add(stop);
            await context.SaveChangesAsync();

            // Act
            var result = await service.GetCollectionConfirmationAsync(stop.StopId, "collector2");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetCollectionConfirmationAsync_IncludesLatestFillLevel()
        {
            // Arrange
            using var context = CreateContext();
            var service = new CollectorDashboardService(context);

            var region = new Region { RegionName = "East" };
            context.Regions.Add(region);
            await context.SaveChangesAsync();

            var bin = new CollectionBin { RegionId = region.RegionId, LocationName = "Test Location", BinCapacity = 100, BinStatus = "Active" };
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

            var detail1 = new CollectionDetails
            {
                StopId = stop.StopId,
                BinId = bin.BinId,
                BinFillLevel = 50,
                CurrentCollectionDateTime = DateTimeOffset.Now.AddHours(-2)
            };
            var detail2 = new CollectionDetails
            {
                StopId = stop.StopId,
                BinId = bin.BinId,
                BinFillLevel = 80,
                CurrentCollectionDateTime = DateTimeOffset.Now.AddHours(-1)
            };
            context.CollectionDetails.AddRange(detail1, detail2);
            await context.SaveChangesAsync();

            // Act
            var result = await service.GetCollectionConfirmationAsync(stop.StopId, "collector1");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(80, result.BinFillLevel);
        }

        #endregion

        #region ConfirmCollectionAsync Tests

        [Fact]
        public async Task ConfirmCollectionAsync_AddsCollectionDetails_AndReturnsTrue()
        {
            // Arrange
            using var context = CreateContext();
            var service = new CollectorDashboardService(context);

            var region = new Region { RegionName = "North" };
            context.Regions.Add(region);
            await context.SaveChangesAsync();

            var bin = new CollectionBin { RegionId = region.RegionId, LocationName = "Test Location", BinCapacity = 100, BinStatus = "Active" };
            context.CollectionBins.Add(bin);
            await context.SaveChangesAsync();

            var assignment = new RouteAssignment { AssignedTo = "collector1", AssignedBy = "admin", AssignedDateTime = DateTime.Now };
            context.RouteAssignments.Add(assignment);
            await context.SaveChangesAsync();

            var plan = new RoutePlan { AssignmentId = assignment.AssignmentId, PlannedDate = DateTime.Today, RouteStatus = "Pending" };
            context.RoutePlans.Add(plan);
            await context.SaveChangesAsync();

            var stop = new RouteStop { RouteId = plan.RouteId, BinId = bin.BinId, PlannedCollectionTime = DateTimeOffset.Now, StopSequence = 1 };
            context.RouteStops.Add(stop);
            await context.SaveChangesAsync();

            var model = new CollectionConfirmationVM
            {
                StopId = stop.StopId,
                BinFillLevel = 85,
                Remarks = "Test collection"
            };

            // Act
            var result = await service.ConfirmCollectionAsync(model, "collector1");

            // Assert
            Assert.True(result);
            var details = await context.CollectionDetails.FirstOrDefaultAsync(cd => cd.StopId == stop.StopId);
            Assert.NotNull(details);
            Assert.Equal(85, details.BinFillLevel);
            Assert.Equal("Collected", details.CollectionStatus);
            Assert.Equal("Test collection", details.IssueLog);
        }

        [Fact]
        public async Task ConfirmCollectionAsync_UpdatesRouteStatus_ToInProgress()
        {
            // Arrange
            using var context = CreateContext();
            var service = new CollectorDashboardService(context);

            var region = new Region { RegionName = "South" };
            context.Regions.Add(region);
            await context.SaveChangesAsync();

            var bin = new CollectionBin { RegionId = region.RegionId, LocationName = "Test Location", BinCapacity = 100, BinStatus = "Active" };
            context.CollectionBins.Add(bin);
            await context.SaveChangesAsync();

            var assignment = new RouteAssignment { AssignedTo = "collector1", AssignedBy = "admin", AssignedDateTime = DateTime.Now };
            context.RouteAssignments.Add(assignment);
            await context.SaveChangesAsync();

            var plan = new RoutePlan { AssignmentId = assignment.AssignmentId, PlannedDate = DateTime.Today, RouteStatus = "Pending" };
            context.RoutePlans.Add(plan);
            await context.SaveChangesAsync();

            var stop = new RouteStop { RouteId = plan.RouteId, BinId = bin.BinId, PlannedCollectionTime = DateTimeOffset.Now, StopSequence = 1 };
            context.RouteStops.Add(stop);
            await context.SaveChangesAsync();

            var model = new CollectionConfirmationVM
            {
                StopId = stop.StopId,
                BinFillLevel = 75
            };

            // Act
            await service.ConfirmCollectionAsync(model, "collector1");

            // Assert
            var updatedPlan = await context.RoutePlans.FindAsync(plan.RouteId);
            Assert.NotNull(updatedPlan);
            // Since there's only 1 stop and it's collected, the route will be marked as Completed
            Assert.Equal("Completed", updatedPlan.RouteStatus);
        }

        [Fact]
        public async Task ConfirmCollectionAsync_UpdatesRouteStatus_ToCompleted_WhenAllStopsCollected()
        {
            // Arrange
            using var context = CreateContext();
            var service = new CollectorDashboardService(context);

            var region = new Region { RegionName = "East" };
            context.Regions.Add(region);
            await context.SaveChangesAsync();

            var bin = new CollectionBin { RegionId = region.RegionId, LocationName = "Test Location", BinCapacity = 100, BinStatus = "Active" };
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

            // Collect first stop
            var detail1 = new CollectionDetails
            {
                StopId = stop1.StopId,
                BinId = bin.BinId,
                CollectionStatus = "Collected",
                CurrentCollectionDateTime = DateTimeOffset.Now
            };
            context.CollectionDetails.Add(detail1);
            await context.SaveChangesAsync();

            var model = new CollectionConfirmationVM
            {
                StopId = stop2.StopId,
                BinFillLevel = 80
            };

            // Act - collect the last stop
            await service.ConfirmCollectionAsync(model, "collector1");

            // Assert
            var updatedPlan = await context.RoutePlans.FindAsync(plan.RouteId);
            Assert.NotNull(updatedPlan);
            Assert.Equal("Completed", updatedPlan.RouteStatus);
        }

        [Fact]
        public async Task ConfirmCollectionAsync_SetsNextStopInfo_InViewModel()
        {
            // Arrange
            using var context = CreateContext();
            var service = new CollectorDashboardService(context);

            var region = new Region { RegionName = "West" };
            context.Regions.Add(region);
            await context.SaveChangesAsync();

            var bin = new CollectionBin 
            { 
                RegionId = region.RegionId, 
                LocationName = "Test Location",
                LocationAddress = "123 Next St",
                BinCapacity = 100, 
                BinStatus = "Active" 
            };
            context.CollectionBins.Add(bin);
            await context.SaveChangesAsync();

            var assignment = new RouteAssignment { AssignedTo = "collector1", AssignedBy = "admin", AssignedDateTime = DateTime.Now };
            context.RouteAssignments.Add(assignment);
            await context.SaveChangesAsync();

            var plan = new RoutePlan { AssignmentId = assignment.AssignmentId, PlannedDate = DateTime.Today, RouteStatus = "In Progress" };
            context.RoutePlans.Add(plan);
            await context.SaveChangesAsync();

            var stop1 = new RouteStop { RouteId = plan.RouteId, BinId = bin.BinId, PlannedCollectionTime = DateTimeOffset.Now, StopSequence = 1 };
            var stop2 = new RouteStop { RouteId = plan.RouteId, BinId = bin.BinId, PlannedCollectionTime = DateTimeOffset.Now.AddMinutes(30), StopSequence = 2 };
            context.RouteStops.AddRange(stop1, stop2);
            await context.SaveChangesAsync();

            var model = new CollectionConfirmationVM
            {
                StopId = stop1.StopId,
                BinFillLevel = 75
            };

            // Act
            await service.ConfirmCollectionAsync(model, "collector1");

            // Assert
            Assert.Equal(stop2.StopId.ToString(), model.NextPointId);
            Assert.Equal("Test Location", model.NextLocationName);
            Assert.Equal("123 Next St", model.NextAddress);
        }

        [Fact]
        public async Task ConfirmCollectionAsync_ReturnsFalse_ForDifferentUser()
        {
            // Arrange
            using var context = CreateContext();
            var service = new CollectorDashboardService(context);

            var region = new Region { RegionName = "Central" };
            context.Regions.Add(region);
            await context.SaveChangesAsync();

            var bin = new CollectionBin { RegionId = region.RegionId, LocationName = "Test Location", BinCapacity = 100, BinStatus = "Active" };
            context.CollectionBins.Add(bin);
            await context.SaveChangesAsync();

            var assignment = new RouteAssignment { AssignedTo = "collector1", AssignedBy = "admin", AssignedDateTime = DateTime.Now };
            context.RouteAssignments.Add(assignment);
            await context.SaveChangesAsync();

            var plan = new RoutePlan { AssignmentId = assignment.AssignmentId, PlannedDate = DateTime.Today, RouteStatus = "Pending" };
            context.RoutePlans.Add(plan);
            await context.SaveChangesAsync();

            var stop = new RouteStop { RouteId = plan.RouteId, BinId = bin.BinId, PlannedCollectionTime = DateTimeOffset.Now, StopSequence = 1 };
            context.RouteStops.Add(stop);
            await context.SaveChangesAsync();

            var model = new CollectionConfirmationVM
            {
                StopId = stop.StopId,
                BinFillLevel = 75
            };

            // Act
            var result = await service.ConfirmCollectionAsync(model, "collector2");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ConfirmCollectionAsync_ReturnsFalse_ForNonTodaysRoute()
        {
            // Arrange
            using var context = CreateContext();
            var service = new CollectorDashboardService(context);

            var region = new Region { RegionName = "North" };
            context.Regions.Add(region);
            await context.SaveChangesAsync();

            var bin = new CollectionBin { RegionId = region.RegionId, LocationName = "Test Location", BinCapacity = 100, BinStatus = "Active" };
            context.CollectionBins.Add(bin);
            await context.SaveChangesAsync();

            var assignment = new RouteAssignment { AssignedTo = "collector1", AssignedBy = "admin", AssignedDateTime = DateTime.Now };
            context.RouteAssignments.Add(assignment);
            await context.SaveChangesAsync();

            var plan = new RoutePlan { AssignmentId = assignment.AssignmentId, PlannedDate = DateTime.Today.AddDays(1), RouteStatus = "Pending" };
            context.RoutePlans.Add(plan);
            await context.SaveChangesAsync();

            var stop = new RouteStop { RouteId = plan.RouteId, BinId = bin.BinId, PlannedCollectionTime = DateTimeOffset.Now, StopSequence = 1 };
            context.RouteStops.Add(stop);
            await context.SaveChangesAsync();

            var model = new CollectionConfirmationVM
            {
                StopId = stop.StopId,
                BinFillLevel = 75
            };

            // Act
            var result = await service.ConfirmCollectionAsync(model, "collector1");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ConfirmCollectionAsync_SkipsAlreadyCollectedStops_WhenFindingNextStop()
        {
            // Arrange
            using var context = CreateContext();
            var service = new CollectorDashboardService(context);

            var region = new Region { RegionName = "South" };
            context.Regions.Add(region);
            await context.SaveChangesAsync();

            var bin = new CollectionBin { RegionId = region.RegionId, LocationName = "Next Location", BinCapacity = 100, BinStatus = "Active" };
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

            // Mark stop2 as already collected
            var detail2 = new CollectionDetails
            {
                StopId = stop2.StopId,
                BinId = bin.BinId,
                CollectionStatus = "Collected",
                CurrentCollectionDateTime = DateTimeOffset.Now.AddMinutes(-10)
            };
            context.CollectionDetails.Add(detail2);
            await context.SaveChangesAsync();

            var model = new CollectionConfirmationVM
            {
                StopId = stop1.StopId,
                BinFillLevel = 70
            };

            // Act
            await service.ConfirmCollectionAsync(model, "collector1");

            // Assert
            Assert.Equal(stop3.StopId.ToString(), model.NextPointId);
        }

        #endregion
    }
}
