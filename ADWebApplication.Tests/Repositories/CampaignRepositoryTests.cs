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
    public class CampaignRepositoryTests
    {
        private In5niteDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<In5niteDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new In5niteDbContext(options);
        }

        #region GetAllCampaignsAsync Tests

        [Fact]
        public async Task GetAllCampaignsAsync_ShouldReturnAllCampaigns_OrderedByStartDateDescending()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            context.Campaigns.AddRange(
                new Campaign 
                { 
                    CampaignId = 1, 
                    CampaignName = "Campaign 1", 
                    StartDate = DateTime.Today.AddDays(-30),
                    EndDate = DateTime.Today.AddDays(-20),
                    Status = "Completed"
                },
                new Campaign 
                { 
                    CampaignId = 2, 
                    CampaignName = "Campaign 2", 
                    StartDate = DateTime.Today.AddDays(10),
                    EndDate = DateTime.Today.AddDays(20),
                    Status = "Scheduled"
                },
                new Campaign 
                { 
                    CampaignId = 3, 
                    CampaignName = "Campaign 3", 
                    StartDate = DateTime.Today,
                    EndDate = DateTime.Today.AddDays(10),
                    Status = "Active"
                }
            );
            await context.SaveChangesAsync();

            var repository = new CampaignRepository(context);

            // Act
            var result = await repository.GetAllCampaignsAsync();

            // Assert
            result.Should().HaveCount(3);
            result.ToList()[0].CampaignId.Should().Be(2); // Future campaign first
            result.ToList()[1].CampaignId.Should().Be(3); // Current campaign
            result.ToList()[2].CampaignId.Should().Be(1); // Past campaign last
        }

        [Fact]
        public async Task GetAllCampaignsAsync_WithNoCampaigns_ShouldReturnEmptyList()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var repository = new CampaignRepository(context);

            // Act
            var result = await repository.GetAllCampaignsAsync();

            // Assert
            result.Should().BeEmpty();
        }

        #endregion

        #region GetCampaignByIdAsync Tests

        [Fact]
        public async Task GetCampaignByIdAsync_ShouldReturnCampaign_WhenExists()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var campaign = new Campaign
            {
                CampaignId = 1,
                CampaignName = "Test Campaign",
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(10),
                Status = "Active",
                Description = "Test Description"
            };
            context.Campaigns.Add(campaign);
            await context.SaveChangesAsync();

            var repository = new CampaignRepository(context);

            // Act
            var result = await repository.GetCampaignByIdAsync(1);

            // Assert
            result.Should().NotBeNull();
            result!.CampaignId.Should().Be(1);
            result.CampaignName.Should().Be("Test Campaign");
            result.Description.Should().Be("Test Description");
        }

        [Fact]
        public async Task GetCampaignByIdAsync_ShouldReturnNull_WhenNotExists()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var repository = new CampaignRepository(context);

            // Act
            var result = await repository.GetCampaignByIdAsync(999);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region AddCampaignAsync Tests

        [Fact]
        public async Task AddCampaignAsync_ShouldAddNewCampaign()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var repository = new CampaignRepository(context);
            var newCampaign = new Campaign
            {
                CampaignName = "New Campaign",
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(30),
                Status = "Scheduled",
                Description = "New Campaign Description"
            };

            // Act
            await repository.AddCampaignAsync(newCampaign);

            // Assert
            var campaigns = await context.Campaigns.ToListAsync();
            campaigns.Should().HaveCount(1);
            campaigns[0].CampaignName.Should().Be("New Campaign");
            campaigns[0].Description.Should().Be("New Campaign Description");
        }

        #endregion

        #region UpdateCampaignAsync Tests

        [Fact]
        public async Task UpdateCampaignAsync_ShouldUpdateCampaignProperties()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var campaign = new Campaign
            {
                CampaignId = 1,
                CampaignName = "Original Name",
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(10),
                Status = "Active",
                Description = "Original Description"
            };
            context.Campaigns.Add(campaign);
            await context.SaveChangesAsync();

            // Detach to simulate fresh retrieval
            context.Entry(campaign).State = EntityState.Detached;

            var repository = new CampaignRepository(context);
            var updatedCampaign = new Campaign
            {
                CampaignId = 1,
                CampaignName = "Updated Name",
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(20),
                Status = "Completed",
                Description = "Updated Description"
            };

            // Act
            await repository.UpdateCampaignAsync(updatedCampaign);

            // Assert
            var result = await context.Campaigns.FindAsync(1);
            result.Should().NotBeNull();
            result!.CampaignName.Should().Be("Updated Name");
            result.Description.Should().Be("Updated Description");
            result.Status.Should().Be("Completed");
            result.EndDate.Should().Be(DateTime.Today.AddDays(20));
        }

        #endregion

        #region DeleteCampaignAsync Tests

        [Fact]
        public async Task DeleteCampaignAsync_ShouldRemoveCampaign_WhenExists()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var campaign = new Campaign
            {
                CampaignId = 1,
                CampaignName = "Campaign to Delete",
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(10),
                Status = "Active"
            };
            context.Campaigns.Add(campaign);
            await context.SaveChangesAsync();

            var repository = new CampaignRepository(context);

            // Act
            await repository.DeleteCampaignAsync(1);

            // Assert
            var deletedCampaign = await context.Campaigns.FindAsync(1);
            deletedCampaign.Should().BeNull();
        }

        [Fact]
        public async Task DeleteCampaignAsync_ShouldDoNothing_WhenNotExists()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var repository = new CampaignRepository(context);

            // Act & Assert
            await repository.Invoking(r => r.DeleteCampaignAsync(999))
                .Should().NotThrowAsync();
        }

        #endregion

        #region GetActiveCampaignsAsync Tests

        [Fact]
        public async Task GetActiveCampaignsAsync_ShouldReturnOnlyActiveCampaigns()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            context.Campaigns.AddRange(
                new Campaign 
                { 
                    CampaignId = 1, 
                    CampaignName = "Active Campaign 1", 
                    StartDate = DateTime.Today.AddDays(-5),
                    EndDate = DateTime.Today.AddDays(5),
                    Status = "Active"
                },
                new Campaign 
                { 
                    CampaignId = 2, 
                    CampaignName = "Active Campaign 2", 
                    StartDate = DateTime.Today.AddDays(-10),
                    EndDate = DateTime.Today.AddDays(10),
                    Status = "Active"
                },
                new Campaign 
                { 
                    CampaignId = 3, 
                    CampaignName = "Completed Campaign", 
                    StartDate = DateTime.Today.AddDays(-20),
                    EndDate = DateTime.Today.AddDays(-10),
                    Status = "Completed"
                },
                new Campaign 
                { 
                    CampaignId = 4, 
                    CampaignName = "Scheduled Campaign", 
                    StartDate = DateTime.Today.AddDays(10),
                    EndDate = DateTime.Today.AddDays(20),
                    Status = "Scheduled"
                }
            );
            await context.SaveChangesAsync();

            var repository = new CampaignRepository(context);

            // Act
            var result = await repository.GetActiveCampaignsAsync();

            // Assert
            result.Should().HaveCount(2);
            result.Should().AllSatisfy(c => c.Status.Should().Be("Active"));
            result.Should().BeInDescendingOrder(c => c.StartDate);
        }

        [Fact]
        public async Task GetActiveCampaignsAsync_WithNoCampaigns_ShouldReturnEmptyList()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var repository = new CampaignRepository(context);

            // Act
            var result = await repository.GetActiveCampaignsAsync();

            // Assert
            result.Should().BeEmpty();
        }

        #endregion

        #region GetScheduledCampaignsAsync Tests

        [Fact]
        public async Task GetScheduledCampaignsAsync_ShouldReturnOnlyScheduledCampaigns()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            context.Campaigns.AddRange(
                new Campaign 
                { 
                    CampaignId = 1, 
                    CampaignName = "Scheduled Campaign 1", 
                    StartDate = DateTime.Today.AddDays(5),
                    EndDate = DateTime.Today.AddDays(15),
                    Status = "Planned"
                },
                new Campaign 
                { 
                    CampaignId = 2, 
                    CampaignName = "Scheduled Campaign 2", 
                    StartDate = DateTime.Today.AddDays(10),
                    EndDate = DateTime.Today.AddDays(20),
                    Status = "Planned"
                },
                new Campaign 
                { 
                    CampaignId = 3, 
                    CampaignName = "Active Campaign", 
                    StartDate = DateTime.Today.AddDays(-5),
                    EndDate = DateTime.Today.AddDays(5),
                    Status = "Active"
                }
            );
            await context.SaveChangesAsync();

            var repository = new CampaignRepository(context);

            // Act
            var result = await repository.GetScheduledCampaignsAsync();

            // Assert
            result.Should().HaveCount(2);
            result.Should().AllSatisfy(c => c.Status.Should().Be("Planned"));
            result.Should().BeInAscendingOrder(c => c.StartDate);
        }

        #endregion

        #region GetByStatusAsync Tests

        [Fact]
        public async Task GetByStatusAsync_ShouldReturnCampaignsWithSpecifiedStatus()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            context.Campaigns.AddRange(
                new Campaign 
                { 
                    CampaignId = 1, 
                    CampaignName = "Completed 1", 
                    StartDate = DateTime.Today.AddDays(-30),
                    EndDate = DateTime.Today.AddDays(-20),
                    Status = "Completed"
                },
                new Campaign 
                { 
                    CampaignId = 2, 
                    CampaignName = "Completed 2", 
                    StartDate = DateTime.Today.AddDays(-40),
                    EndDate = DateTime.Today.AddDays(-30),
                    Status = "Completed"
                },
                new Campaign 
                { 
                    CampaignId = 3, 
                    CampaignName = "Active", 
                    StartDate = DateTime.Today,
                    EndDate = DateTime.Today.AddDays(10),
                    Status = "Active"
                }
            );
            await context.SaveChangesAsync();

            var repository = new CampaignRepository(context);

            // Act
            var result = await repository.GetByStatusAsync("Completed");

            // Assert
            result.Should().HaveCount(2);
            result.Should().AllSatisfy(c => c.Status.Should().Be("Completed"));
            result.Should().BeInDescendingOrder(c => c.StartDate);
        }

        [Fact]
        public async Task GetByStatusAsync_WithNoMatchingStatus_ShouldReturnEmptyList()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            context.Campaigns.Add(new Campaign 
            { 
                CampaignId = 1, 
                CampaignName = "Active Campaign", 
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(10),
                Status = "Active"
            });
            await context.SaveChangesAsync();

            var repository = new CampaignRepository(context);

            // Act
            var result = await repository.GetByStatusAsync("Cancelled");

            // Assert
            result.Should().BeEmpty();
        }

        #endregion

        #region GetCurrentCampaignAsync Tests

        [Fact]
        public async Task GetCurrentCampaignAsync_ShouldReturnActiveCampaign_WhenOneExists()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var today = DateTime.Today;
            context.Campaigns.AddRange(
                new Campaign 
                { 
                    CampaignId = 1, 
                    CampaignName = "Current Campaign", 
                    StartDate = today.AddDays(-5),
                    EndDate = today.AddDays(5),
                    Status = "Active"
                },
                new Campaign 
                { 
                    CampaignId = 2, 
                    CampaignName = "Past Campaign", 
                    StartDate = today.AddDays(-30),
                    EndDate = today.AddDays(-20),
                    Status = "Completed"
                },
                new Campaign 
                { 
                    CampaignId = 3, 
                    CampaignName = "Future Campaign", 
                    StartDate = today.AddDays(10),
                    EndDate = today.AddDays(20),
                    Status = "Scheduled"
                }
            );
            await context.SaveChangesAsync();

            var repository = new CampaignRepository(context);

            // Act
            var result = await repository.GetCurrentCampaignAsync();

            // Assert
            result.Should().NotBeNull();
            result!.CampaignId.Should().Be(1);
            result.CampaignName.Should().Be("Current Campaign");
        }

        [Fact]
        public async Task GetCurrentCampaignAsync_ShouldReturnNull_WhenNoActiveCampaign()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var today = DateTime.Today;
            context.Campaigns.Add(new Campaign 
            { 
                CampaignId = 1, 
                CampaignName = "Future Campaign", 
                StartDate = today.AddDays(10),
                EndDate = today.AddDays(20),
                Status = "Scheduled"
            });
            await context.SaveChangesAsync();

            var repository = new CampaignRepository(context);

            // Act
            var result = await repository.GetCurrentCampaignAsync();

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetCurrentCampaignAsync_ShouldReturnOldestActive_WhenMultipleActiveExist()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var today = DateTime.Today;
            context.Campaigns.AddRange(
                new Campaign 
                { 
                    CampaignId = 1, 
                    CampaignName = "Older Active Campaign", 
                    StartDate = today.AddDays(-10),
                    EndDate = today.AddDays(5),
                    Status = "Active"
                },
                new Campaign 
                { 
                    CampaignId = 2, 
                    CampaignName = "Newer Active Campaign", 
                    StartDate = today.AddDays(-3),
                    EndDate = today.AddDays(7),
                    Status = "Active"
                }
            );
            await context.SaveChangesAsync();

            var repository = new CampaignRepository(context);

            // Act
            var result = await repository.GetCurrentCampaignAsync();

            // Assert
            result.Should().NotBeNull();
            result!.CampaignId.Should().Be(1); // Oldest active campaign
            result.CampaignName.Should().Be("Older Active Campaign");
        }

        #endregion
    }
}
