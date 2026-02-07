using Xunit;
using Microsoft.EntityFrameworkCore;
using ADWebApplication.Data;
using ADWebApplication.Models;
using ADWebApplication.Models.DTOs;
using ADWebApplication.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ADWebApplication.Tests
{
    public class RouteAssignmentServiceTests
    {
        private static In5niteDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<In5niteDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new In5niteDbContext(options);
        }

        private static List<UiRouteStopDto> CreateSampleStops(int routeKey, int binCount, int startingBinId = 1)
        {
            var stops = new List<UiRouteStopDto>();
            for (int i = 0; i < binCount; i++)
            {
                stops.Add(new UiRouteStopDto
                {
                    RouteKey = routeKey,
                    BinId = startingBinId + i,
                    StopNumber = i + 1
                });
            }
            return stops;
        }

        #region SavePlannedRoutesAsync - New Routes Tests

        [Fact]
        public async Task SavePlannedRoutesAsync_CreatesNewRoute_WithValidData()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var service = new RouteAssignmentService(dbContext);
            var date = new DateTime(2026, 2, 10);
            var stops = CreateSampleStops(routeKey: 1, binCount: 3);
            var assignments = new Dictionary<int, string>();

            // Act
            await service.SavePlannedRoutesAsync(stops, assignments, "admin1", date);

            // Assert
            var routes = await dbContext.RoutePlans
                .Include(r => r.RouteStops)
                .ToListAsync();

            Assert.Single(routes);
            var route = routes[0];
            Assert.Equal(date, route.PlannedDate);
            Assert.Equal("admin1", route.GeneratedBy);
            Assert.Equal("Scheduled", route.RouteStatus);
            Assert.Equal(3, route.RouteStops.Count);
        }

        [Fact]
        public async Task SavePlannedRoutesAsync_CreatesRouteStops_WithCorrectSequence()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var service = new RouteAssignmentService(dbContext);
            var date = new DateTime(2026, 2, 10);
            var stops = new List<UiRouteStopDto>
            {
                new UiRouteStopDto { RouteKey = 1, BinId = 10, StopNumber = 1 },
                new UiRouteStopDto { RouteKey = 1, BinId = 20, StopNumber = 2 },
                new UiRouteStopDto { RouteKey = 1, BinId = 30, StopNumber = 3 }
            };
            var assignments = new Dictionary<int, string>();

            // Act
            await service.SavePlannedRoutesAsync(stops, assignments, "admin1", date);

            // Assert
            var route = await dbContext.RoutePlans
                .Include(r => r.RouteStops)
                .FirstOrDefaultAsync();

            Assert.NotNull(route);
            var orderedStops = route!.RouteStops.OrderBy(s => s.StopSequence).ToList();
            Assert.Equal(10, orderedStops[0].BinId);
            Assert.Equal(1, orderedStops[0].StopSequence);
            Assert.Equal(20, orderedStops[1].BinId);
            Assert.Equal(2, orderedStops[1].StopSequence);
            Assert.Equal(30, orderedStops[2].BinId);
            Assert.Equal(3, orderedStops[2].StopSequence);
        }

        [Fact]
        public async Task SavePlannedRoutesAsync_CreatesRouteAssignment_WhenOfficerAssigned()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var service = new RouteAssignmentService(dbContext);
            var date = new DateTime(2026, 2, 10);
            var stops = CreateSampleStops(routeKey: 1, binCount: 2);
            var assignments = new Dictionary<int, string>
            {
                { 1, "officer@in5nite.sg" }
            };

            // Act
            await service.SavePlannedRoutesAsync(stops, assignments, "admin1", date);

            // Assert
            var route = await dbContext.RoutePlans
                .Include(r => r.RouteAssignment)
                .FirstOrDefaultAsync();

            Assert.NotNull(route);
            Assert.NotNull(route!.RouteAssignment);
            Assert.Equal("officer@in5nite.sg", route.RouteAssignment!.AssignedTo);
            Assert.Equal("admin1", route.RouteAssignment.AssignedBy);
        }

        [Fact]
        public async Task SavePlannedRoutesAsync_CreatesMultipleRoutes_WithCorrectCounts()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var service = new RouteAssignmentService(dbContext);
            var date = new DateTime(2026, 2, 10);
            var stops = new List<UiRouteStopDto>();
            stops.AddRange(CreateSampleStops(routeKey: 1, binCount: 2, startingBinId: 1));
            stops.AddRange(CreateSampleStops(routeKey: 2, binCount: 3, startingBinId: 10));
            stops.AddRange(CreateSampleStops(routeKey: 3, binCount: 1, startingBinId: 20));
            var assignments = new Dictionary<int, string>();

            // Act
            await service.SavePlannedRoutesAsync(stops, assignments, "admin1", date);

            // Assert
            var routes = await dbContext.RoutePlans
                .Include(r => r.RouteStops)
                .ToListAsync();

            Assert.Equal(3, routes.Count);
        }

        [Fact]
        public async Task SavePlannedRoutesAsync_AssignsOfficersToCorrectRoutes()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var service = new RouteAssignmentService(dbContext);
            var date = new DateTime(2026, 2, 10);
            var stops = new List<UiRouteStopDto>();
            stops.AddRange(CreateSampleStops(routeKey: 1, binCount: 2));
            stops.AddRange(CreateSampleStops(routeKey: 2, binCount: 2, startingBinId: 10));
            var assignments = new Dictionary<int, string>
            {
                { 1, "officer1@in5nite.sg" },
                { 2, "officer2@in5nite.sg" }
            };

            // Act
            await service.SavePlannedRoutesAsync(stops, assignments, "admin1", date);

            // Assert
            var routes = await dbContext.RoutePlans
                .Include(r => r.RouteAssignment)
                .OrderBy(r => r.RouteId)
                .ToListAsync();

            Assert.Equal(2, routes.Count);
            Assert.NotNull(routes[0].RouteAssignment);
            Assert.NotNull(routes[1].RouteAssignment);
            Assert.Equal("officer1@in5nite.sg", routes[0].RouteAssignment!.AssignedTo);
            Assert.Equal("officer2@in5nite.sg", routes[1].RouteAssignment!.AssignedTo);
        }

        [Fact]
        public async Task SavePlannedRoutesAsync_SetsPlannedCollectionTime_Correctly()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var service = new RouteAssignmentService(dbContext);
            var date = new DateTime(2026, 3, 15);
            var stops = CreateSampleStops(routeKey: 1, binCount: 3);
            var assignments = new Dictionary<int, string>();

            // Act
            await service.SavePlannedRoutesAsync(stops, assignments, "admin1", date);

            // Assert
            var route = await dbContext.RoutePlans
                .Include(r => r.RouteStops)
                .FirstOrDefaultAsync();

            Assert.NotNull(route);
            Assert.All(route!.RouteStops, stop => 
                Assert.Equal(date, stop.PlannedCollectionTime));
        }

        #region SavePlannedRoutesAsync - Update Existing Routes Tests

        [Fact]
        public async Task SavePlannedRoutesAsync_UpdatesExistingAssignment_ChangesOfficer()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var service = new RouteAssignmentService(dbContext);
            var date = new DateTime(2026, 2, 10);

            var existingRoute = new RoutePlan
            {
                PlannedDate = date,
                GeneratedBy = "admin1",
                RouteStatus = "Scheduled",
                RouteStops = new List<RouteStop>
                {
                    new RouteStop { BinId = 1, StopSequence = 1, PlannedCollectionTime = date }
                },
                RouteAssignment = new RouteAssignment
                {
                    AssignedTo = "oldofficer@in5nite.sg",
                    AssignedBy = "admin1"
                }
            };
            dbContext.RoutePlans.Add(existingRoute);
            await dbContext.SaveChangesAsync();

            var routeId = existingRoute.RouteId;
            var stops = new List<UiRouteStopDto>();
            var assignments = new Dictionary<int, string>
            {
                { routeId, "newofficer@in5nite.sg" }
            };

            // Act
            await service.SavePlannedRoutesAsync(stops, assignments, "admin2", date);

            // Assert
            var updatedRoute = await dbContext.RoutePlans
                .Include(r => r.RouteAssignment)
                .FirstOrDefaultAsync(r => r.RouteId == routeId);

            Assert.NotNull(updatedRoute);
            Assert.NotNull(updatedRoute!.RouteAssignment);
            Assert.Equal("newofficer@in5nite.sg", updatedRoute.RouteAssignment!.AssignedTo);
        }

        [Fact]
        public async Task SavePlannedRoutesAsync_PreservesExistingRouteStops_WhenUpdatingAssignment()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var service = new RouteAssignmentService(dbContext);
            var date = new DateTime(2026, 2, 10);

            var existingRoute = new RoutePlan
            {
                PlannedDate = date,
                GeneratedBy = "admin1",
                RouteStatus = "Scheduled",
                RouteStops = new List<RouteStop>
                {
                    new RouteStop { BinId = 1, StopSequence = 1, PlannedCollectionTime = date },
                    new RouteStop { BinId = 2, StopSequence = 2, PlannedCollectionTime = date },
                    new RouteStop { BinId = 3, StopSequence = 3, PlannedCollectionTime = date }
                }
            };
            dbContext.RoutePlans.Add(existingRoute);
            await dbContext.SaveChangesAsync();

            var routeId = existingRoute.RouteId;

            var stops = new List<UiRouteStopDto>();
            var assignments = new Dictionary<int, string>
            {
                { routeId, "officer@in5nite.sg" }
            };

            // Act
            await service.SavePlannedRoutesAsync(stops, assignments, "admin2", date);

            // Assert
            var updatedRoute = await dbContext.RoutePlans
                .Include(r => r.RouteStops)
                .FirstOrDefaultAsync(r => r.RouteId == routeId);

            Assert.NotNull(updatedRoute);
            Assert.Equal(3, updatedRoute!.RouteStops.Count);
        }

        #endregion

        #endregion

        #region SavePlannedRoutesAsync - Edge Cases Tests

        [Fact]
        public async Task SavePlannedRoutesAsync_HandlesEmptyStops_WithNoExistingPlans()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var service = new RouteAssignmentService(dbContext);
            var date = new DateTime(2026, 2, 10);
            var stops = new List<UiRouteStopDto>();
            var assignments = new Dictionary<int, string>();

            // Act
            await service.SavePlannedRoutesAsync(stops, assignments, "admin1", date);

            // Assert
            var routes = await dbContext.RoutePlans.ToListAsync();
            Assert.Empty(routes);
        }

        [Fact]
        public async Task SavePlannedRoutesAsync_CreatesSeparateRoutes_ForDifferentDates()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var service = new RouteAssignmentService(dbContext);
            var date1 = new DateTime(2026, 2, 10);
            var date2 = new DateTime(2026, 2, 11);
            var stops1 = CreateSampleStops(routeKey: 1, binCount: 2, startingBinId: 1);
            var stops2 = CreateSampleStops(routeKey: 1, binCount: 2, startingBinId: 10);
            var assignments = new Dictionary<int, string>();

            // Act
            await service.SavePlannedRoutesAsync(stops1, assignments, "admin1", date1);
            await service.SavePlannedRoutesAsync(stops2, assignments, "admin1", date2);

            // Assert
            var allRoutes = await dbContext.RoutePlans.ToListAsync();
            Assert.Equal(2, allRoutes.Count);
            Assert.Single(allRoutes, r => r.PlannedDate == date1);
            Assert.Single(allRoutes, r => r.PlannedDate == date2);
        }

        #endregion
    }
}
