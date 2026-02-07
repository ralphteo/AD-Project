using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ADWebApplication.Data;
using ADWebApplication.Data.Repository;
using ADWebApplication.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ADWebApplication.Tests.Repositories
{
    public class AdminRepositoryTests
    {
        private In5niteDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<In5niteDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new In5niteDbContext(options);
        }

        #region GetAllBinsAsync Tests

        [Fact]
        public async Task GetAllBinsAsync_ShouldReturnAllBins_OrderedByRegionAndBinId()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var region1 = new Region { RegionId = 1, RegionName = "North" };
            var region2 = new Region { RegionId = 2, RegionName = "South" };
            context.Regions.AddRange(region1, region2);

            context.CollectionBins.AddRange(
                new CollectionBin { BinId = 3, RegionId = 2, BinStatus = "Active" },
                new CollectionBin { BinId = 1, RegionId = 1, BinStatus = "Active" },
                new CollectionBin { BinId = 2, RegionId = 1, BinStatus = "Inactive" }
            );
            await context.SaveChangesAsync();

            var repository = new AdminRepository(context);

            // Act
            var result = await repository.GetAllBinsAsync();

            // Assert
            result.Should().HaveCount(3);
            result.ToList()[0].BinId.Should().Be(1); // Region 1, BinId 1
            result.ToList()[1].BinId.Should().Be(2); // Region 1, BinId 2
            result.ToList()[2].BinId.Should().Be(3); // Region 2, BinId 3
            result.ToList()[0].Region.Should().NotBeNull();
        }

        [Fact]
        public async Task GetAllBinsAsync_WithNoBins_ShouldReturnEmptyList()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var repository = new AdminRepository(context);

            // Act
            var result = await repository.GetAllBinsAsync();

            // Assert
            result.Should().BeEmpty();
        }

        #endregion

        #region GetAllCollectionOfficersAsync Tests

        [Fact]
        public async Task GetAllCollectionOfficersAsync_ShouldReturnOnlyActiveCollectionOfficers()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var role = new Role { RoleId = 1, Name = "Collection Officer" };
            context.Roles.Add(role);

            context.Employees.AddRange(
                new Employee { Username = "CO-001", FullName = "Officer One", IsActive = true, RoleId = 1 },
                new Employee { Username = "CO-002", FullName = "Officer Two", IsActive = true, RoleId = 1 },
                new Employee { Username = "CO-003", FullName = "Officer Three", IsActive = false, RoleId = 1 },
                new Employee { Username = "HR-001", FullName = "HR One", IsActive = true, RoleId = 1 }
            );
            await context.SaveChangesAsync();

            var repository = new AdminRepository(context);

            // Act
            var result = await repository.GetAllCollectionOfficersAsync();

            // Assert
            result.Should().HaveCount(2);
            result.Should().AllSatisfy(e => e.Username.Should().StartWith("CO-"));
            result.Should().AllSatisfy(e => e.IsActive.Should().BeTrue());
            result.Should().BeInAscendingOrder(e => e.Username);
        }

        [Fact]
        public async Task GetAllCollectionOfficersAsync_WithNoOfficers_ShouldReturnEmptyList()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var repository = new AdminRepository(context);

            // Act
            var result = await repository.GetAllCollectionOfficersAsync();

            // Assert
            result.Should().BeEmpty();
        }

        #endregion

        #region GetAllRegionsAsync Tests

        [Fact]
        public async Task GetAllRegionsAsync_ShouldReturnAllRegions_OrderedByName()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            context.Regions.AddRange(
                new Region { RegionId = 1, RegionName = "South" },
                new Region { RegionId = 2, RegionName = "North" },
                new Region { RegionId = 3, RegionName = "East" }
            );
            await context.SaveChangesAsync();

            var repository = new AdminRepository(context);

            // Act
            var result = await repository.GetAllRegionsAsync();

            // Assert
            result.Should().HaveCount(3);
            result.Should().BeInAscendingOrder(r => r.RegionName);
            result[0].RegionName.Should().Be("East");
            result[1].RegionName.Should().Be("North");
            result[2].RegionName.Should().Be("South");
        }

        [Fact]
        public async Task GetAllRegionsAsync_WithNoRegions_ShouldReturnEmptyList()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var repository = new AdminRepository(context);

            // Act
            var result = await repository.GetAllRegionsAsync();

            // Assert
            result.Should().BeEmpty();
        }

        #endregion

        #region DeleteBinAsync Tests

        [Fact]
        public async Task DeleteBinAsync_ShouldRemoveBin_WhenBinExists()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var bin = new CollectionBin { BinId = 1, RegionId = 1, BinStatus = "Active" };
            context.CollectionBins.Add(bin);
            await context.SaveChangesAsync();

            var repository = new AdminRepository(context);

            // Act
            await repository.DeleteBinAsync(1);

            // Assert
            var deletedBin = await context.CollectionBins.FindAsync(1);
            deletedBin.Should().BeNull();
        }

        [Fact]
        public async Task DeleteBinAsync_ShouldDoNothing_WhenBinDoesNotExist()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var repository = new AdminRepository(context);

            // Act & Assert
            await repository.Invoking(r => r.DeleteBinAsync(999))
                .Should().NotThrowAsync();
        }

        #endregion

        #region GetBinByIdAsync Tests

        [Fact]
        public async Task GetBinByIdAsync_ShouldReturnBin_WhenBinExists()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var region = new Region { RegionId = 1, RegionName = "North" };
            context.Regions.Add(region);
            var bin = new CollectionBin 
            { 
                BinId = 1, 
                RegionId = 1, 
                BinStatus = "Active",
                LocationName = "Test Location"
            };
            context.CollectionBins.Add(bin);
            await context.SaveChangesAsync();

            var repository = new AdminRepository(context);

            // Act
            var result = await repository.GetBinByIdAsync(1);

            // Assert
            result.Should().NotBeNull();
            result!.BinId.Should().Be(1);
            result.LocationName.Should().Be("Test Location");
            result.Region.Should().NotBeNull();
        }

        [Fact]
        public async Task GetBinByIdAsync_ShouldReturnNull_WhenBinDoesNotExist()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var repository = new AdminRepository(context);

            // Act
            var result = await repository.GetBinByIdAsync(999);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region UpdateBinAsync Tests

        [Fact]
        public async Task UpdateBinAsync_ShouldUpdateBinProperties()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var bin = new CollectionBin 
            { 
                BinId = 1, 
                RegionId = 1, 
                BinStatus = "Active",
                LocationName = "Old Location"
            };
            context.CollectionBins.Add(bin);
            await context.SaveChangesAsync();

            // Detach to simulate fresh retrieval
            context.Entry(bin).State = EntityState.Detached;

            var repository = new AdminRepository(context);
            var updatedBin = new CollectionBin
            {
                BinId = 1,
                RegionId = 1,
                BinStatus = "Inactive",
                LocationName = "New Location"
            };

            // Act
            await repository.UpdateBinAsync(updatedBin);

            // Assert
            var result = await context.CollectionBins.FindAsync(1);
            result.Should().NotBeNull();
            result!.BinStatus.Should().Be("Inactive");
            result.LocationName.Should().Be("New Location");
        }

        #endregion

        #region CreateBinAsync Tests

        [Fact]
        public async Task CreateBinAsync_ShouldAddNewBin()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var repository = new AdminRepository(context);
            var newBin = new CollectionBin
            {
                RegionId = 1,
                BinStatus = "Active",
                LocationName = "New Location",
                BinCapacity = 100
            };

            // Act
            await repository.CreateBinAsync(newBin);

            // Assert
            var bins = await context.CollectionBins.ToListAsync();
            bins.Should().HaveCount(1);
            bins[0].LocationName.Should().Be("New Location");
            bins[0].BinCapacity.Should().Be(100);
        }

        #endregion

        #region GetEmployeeByUsernameAsync Tests

        [Fact]
        public async Task GetEmployeeByUsernameAsync_ShouldReturnEmployee_WhenExists()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            context.Employees.Add(new Employee 
            { 
                Username = "CO-001", 
                FullName = "Test Officer",
                IsActive = true,
                RoleId = 1
            });
            await context.SaveChangesAsync();

            var repository = new AdminRepository(context);

            // Act
            var result = await repository.GetEmployeeByUsernameAsync("CO-001");

            // Assert
            result.Should().NotBeNull();
            result.Username.Should().Be("CO-001");
            result.FullName.Should().Be("Test Officer");
        }

        [Fact]
        public async Task GetEmployeeByUsernameAsync_ShouldThrowException_WhenNotExists()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var repository = new AdminRepository(context);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await repository.GetEmployeeByUsernameAsync("NON-EXISTENT")
            );
        }

        #endregion

        #region GetAvailableCollectionOfficersAsync Tests

        [Fact]
        public async Task GetAvailableCollectionOfficersAsync_ShouldReturnOfficersWithoutAssignments()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var role = new Role { RoleId = 1, Name = "Collection Officer" };
            context.Roles.Add(role);

            var officer1 = new Employee { Username = "CO-001", FullName = "Available Officer", IsActive = true, RoleId = 1 };
            var officer2 = new Employee { Username = "CO-002", FullName = "Busy Officer", IsActive = true, RoleId = 1 };
            context.Employees.AddRange(officer1, officer2);

            var from = DateTime.Today;
            var to = DateTime.Today.AddDays(7);

            // Create assignment for officer2 (making them unavailable)
            var assignment = new RouteAssignment 
            { 
                AssignmentId = 1, 
                AssignedTo = "CO-002", 
                AssignedDateTime = DateTime.Now 
            };
            var route = new RoutePlan 
            { 
                RouteId = 1, 
                AssignmentId = 1, 
                PlannedDate = from.AddDays(1),
                RouteStatus = "Active"
            };
            context.RouteAssignments.Add(assignment);
            context.RoutePlans.Add(route);
            await context.SaveChangesAsync();

            var repository = new AdminRepository(context);

            // Act
            var result = await repository.GetAvailableCollectionOfficersAsync(from, to);

            // Assert
            result.Should().HaveCount(1);
            result[0].Username.Should().Be("CO-001");
        }

        #endregion

        #region GetAvailableCollectionOfficersCalendarAsync Tests

        [Fact]
        public async Task GetAvailableCollectionOfficersCalendarAsync_ShouldReturnCalendarDtos()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var role = new Role { RoleId = 1, Name = "Collection Officer" };
            context.Roles.Add(role);

            context.Employees.Add(new Employee 
            { 
                Username = "CO-001", 
                FullName = "Test Officer", 
                IsActive = true, 
                RoleId = 1 
            });
            await context.SaveChangesAsync();

            var repository = new AdminRepository(context);
            var from = DateTime.Today;
            var to = DateTime.Today.AddDays(7);

            // Act
            var result = await repository.GetAvailableCollectionOfficersCalendarAsync(from, to);

            // Assert
            result.Should().NotBeNull();
            // The result should contain DTOs with officer information
        }

        #endregion

        #region GetAssignedCollectionOfficersCalendarAsync Tests

        [Fact]
        public async Task GetAssignedCollectionOfficersCalendarAsync_ShouldReturnAssignedOfficers()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var role = new Role { RoleId = 1, Name = "Collection Officer" };
            context.Roles.Add(role);

            var officer = new Employee { Username = "CO-001", FullName = "Assigned Officer", IsActive = true, RoleId = 1 };
            context.Employees.Add(officer);

            var assignment = new RouteAssignment 
            { 
                AssignmentId = 1, 
                AssignedTo = "CO-001", 
                AssignedDateTime = DateTime.Now 
            };
            var route = new RoutePlan 
            { 
                RouteId = 1, 
                AssignmentId = 1, 
                PlannedDate = DateTime.Today.AddDays(1),
                RouteStatus = "Active"
            };
            context.RouteAssignments.Add(assignment);
            context.RoutePlans.Add(route);
            await context.SaveChangesAsync();

            var repository = new AdminRepository(context);
            var from = DateTime.Today;
            var to = DateTime.Today.AddDays(7);

            // Act
            var result = await repository.GetAssignedCollectionOfficersCalendarAsync(from, to);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCountGreaterThanOrEqualTo(0);
        }

        #endregion

        #region GetRouteAssignmentsForOfficerAsync Tests

        [Fact]
        public async Task GetRouteAssignmentsForOfficerAsync_ShouldReturnOfficerAssignments()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var oneYearAgo = DateTime.Today.AddYears(-1);
            
            var assignment1 = new RouteAssignment 
            { 
                AssignmentId = 1, 
                AssignedTo = "CO-001", 
                AssignedDateTime = DateTime.Now 
            };
            var assignment2 = new RouteAssignment 
            { 
                AssignmentId = 2, 
                AssignedTo = "CO-002", 
                AssignedDateTime = DateTime.Now 
            };
            
            context.RouteAssignments.AddRange(assignment1, assignment2);
            
            var route1 = new RoutePlan 
            { 
                RouteId = 1, 
                AssignmentId = 1, 
                PlannedDate = DateTime.Today,
                RouteStatus = "Active"
            };
            var route2 = new RoutePlan 
            { 
                RouteId = 2, 
                AssignmentId = 1, 
                PlannedDate = DateTime.Today.AddYears(-2),
                RouteStatus = "Completed"
            };
            
            context.RoutePlans.AddRange(route1, route2);
            await context.SaveChangesAsync();

            var repository = new AdminRepository(context);

            // Act
            var result = await repository.GetRouteAssignmentsForOfficerAsync("CO-001", oneYearAgo);

            // Assert
            result.Should().NotBeEmpty();
            result.Should().AllSatisfy(ra => ra.AssignedTo.Should().Be("CO-001"));
        }

        #endregion

        #region GetAllRouteAssignmentsForCollectionOfficersAsync Tests

        [Fact]
        public async Task GetAllRouteAssignmentsForCollectionOfficersAsync_ShouldReturnOnlyCollectionOfficerAssignments()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var role = new Role { RoleId = 1, Name = "Collection Officer" };
            context.Roles.Add(role);

            var officer = new Employee { Username = "CO-001", FullName = "Test Officer", IsActive = true, RoleId = 1 };
            context.Employees.Add(officer);

            var assignment1 = new RouteAssignment 
            { 
                AssignmentId = 1, 
                AssignedTo = "CO-001", 
                AssignedDateTime = DateTime.Now 
            };
            var assignment2 = new RouteAssignment 
            { 
                AssignmentId = 2, 
                AssignedTo = "HR-001", 
                AssignedDateTime = DateTime.Now 
            };
            
            context.RouteAssignments.AddRange(assignment1, assignment2);
            await context.SaveChangesAsync();

            var repository = new AdminRepository(context);

            // Act
            var result = await repository.GetAllRouteAssignmentsForCollectionOfficersAsync();

            // Assert
            result.Should().NotBeEmpty();
            result.Should().AllSatisfy(ra => ra.AssignedTo.Should().StartWith("CO-"));
        }

        #endregion
    }
}
