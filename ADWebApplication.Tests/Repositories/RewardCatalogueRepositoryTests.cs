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
    public class RewardCatalogueRepositoryTests
    {
        private In5niteDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<In5niteDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new In5niteDbContext(options);
        }

        #region GetAllRewardsAsync Tests

        [Fact]
        public async Task GetAllRewardsAsync_ShouldReturnAllRewards_OrderedByCategoryThenPoints()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            context.RewardCatalogues.AddRange(
                new RewardCatalogue
                {
                    RewardId = 1,
                    RewardName = "Item B",
                    RewardCategory = "Electronics",
                    Points = 200,
                    StockQuantity = 10,
                    Availability = true
                },
                new RewardCatalogue
                {
                    RewardId = 2,
                    RewardName = "Item A",
                    RewardCategory = "Electronics",
                    Points = 100,
                    StockQuantity = 5,
                    Availability = true
                },
                new RewardCatalogue
                {
                    RewardId = 3,
                    RewardName = "Item C",
                    RewardCategory = "Books",
                    Points = 50,
                    StockQuantity = 15,
                    Availability = true
                }
            );
            await context.SaveChangesAsync();

            var repository = new RewardCatalogueRepository(context);

            // Act
            var result = await repository.GetAllRewardsAsync();

            // Assert
            result.Should().HaveCount(3);
            result.ToList()[0].RewardCategory.Should().Be("Books");
            result.ToList()[1].RewardCategory.Should().Be("Electronics");
            result.ToList()[1].Points.Should().Be(100); // Lower points first within category
            result.ToList()[2].Points.Should().Be(200);
        }

        [Fact]
        public async Task GetAllRewardsAsync_WithNoRewards_ShouldReturnEmptyList()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var repository = new RewardCatalogueRepository(context);

            // Act
            var result = await repository.GetAllRewardsAsync();

            // Assert
            result.Should().BeEmpty();
        }

        #endregion

        #region GetRewardByIdAsync Tests

        [Fact]
        public async Task GetRewardByIdAsync_ShouldReturnReward_WhenExists()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var reward = new RewardCatalogue
            {
                RewardId = 1,
                RewardName = "Test Reward",
                RewardCategory = "Electronics",
                Points = 150,
                StockQuantity = 10,
                Description = "Test Description",
                Availability = true
            };
            context.RewardCatalogues.Add(reward);
            await context.SaveChangesAsync();

            var repository = new RewardCatalogueRepository(context);

            // Act
            var result = await repository.GetRewardByIdAsync(1);

            // Assert
            result.Should().NotBeNull();
            result!.RewardId.Should().Be(1);
            result.RewardName.Should().Be("Test Reward");
            result.Description.Should().Be("Test Description");
        }

        [Fact]
        public async Task GetRewardByIdAsync_ShouldReturnNull_WhenNotExists()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var repository = new RewardCatalogueRepository(context);

            // Act
            var result = await repository.GetRewardByIdAsync(999);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region AddRewardAsync Tests

        [Fact]
        public async Task AddRewardAsync_ShouldAddNewReward_WithCreatedAndUpdatedDates()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var repository = new RewardCatalogueRepository(context);
            var newReward = new RewardCatalogue
            {
                RewardName = "New Reward",
                RewardCategory = "Vouchers",
                Points = 100,
                StockQuantity = 20,
                Description = "New reward description",
                Availability = true
            };

            // Act
            await repository.AddRewardAsync(newReward);

            // Assert
            var rewards = await context.RewardCatalogues.ToListAsync();
            rewards.Should().HaveCount(1);
            rewards[0].RewardName.Should().Be("New Reward");
            rewards[0].CreatedDate.Should().NotBe(default(DateTime));
            rewards[0].UpdatedDate.Should().NotBe(default(DateTime));
            rewards[0].CreatedDate.Date.Should().Be(DateTime.Today);
        }

        #endregion

        #region UpdateRewardAsync Tests

        [Fact]
        public async Task UpdateRewardAsync_ShouldUpdateRewardProperties_AndSetUpdatedDate()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var originalDate = DateTime.Now.AddDays(-10);
            var reward = new RewardCatalogue
            {
                RewardId = 1,
                RewardName = "Original Name",
                RewardCategory = "Original Category",
                Points = 100,
                StockQuantity = 10,
                Description = "Original Description",
                Availability = true,
                CreatedDate = originalDate,
                UpdatedDate = originalDate
            };
            context.RewardCatalogues.Add(reward);
            await context.SaveChangesAsync();

            // Detach to simulate fresh retrieval
            context.Entry(reward).State = EntityState.Detached;

            var repository = new RewardCatalogueRepository(context);
            var updatedReward = new RewardCatalogue
            {
                RewardId = 1,
                RewardName = "Updated Name",
                RewardCategory = "Updated Category",
                Points = 200,
                StockQuantity = 5,
                Description = "Updated Description",
                Availability = false
            };

            // Act
            await repository.UpdateRewardAsync(updatedReward);

            // Assert
            var result = await context.RewardCatalogues.FindAsync(1);
            result.Should().NotBeNull();
            result!.RewardName.Should().Be("Updated Name");
            result.RewardCategory.Should().Be("Updated Category");
            result.Points.Should().Be(200);
            result.StockQuantity.Should().Be(5);
            result.Description.Should().Be("Updated Description");
            result.Availability.Should().BeFalse();
            result.UpdatedDate.Date.Should().Be(DateTime.Today);
            result.CreatedDate.Should().Be(originalDate); // Should not change
        }

        #endregion

        #region DeleteRewardAsync Tests

        [Fact]
        public async Task DeleteRewardAsync_ShouldRemoveReward_WhenExists()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var reward = new RewardCatalogue
            {
                RewardId = 1,
                RewardName = "Reward to Delete",
                RewardCategory = "Test",
                Points = 100,
                StockQuantity = 10,
                Availability = true
            };
            context.RewardCatalogues.Add(reward);
            await context.SaveChangesAsync();

            var repository = new RewardCatalogueRepository(context);

            // Act
            await repository.DeleteRewardAsync(1);

            // Assert
            var deletedReward = await context.RewardCatalogues.FindAsync(1);
            deletedReward.Should().BeNull();
        }

        [Fact]
        public async Task DeleteRewardAsync_ShouldDoNothing_WhenNotExists()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var repository = new RewardCatalogueRepository(context);

            // Act & Assert
            await repository.Invoking(r => r.DeleteRewardAsync(999))
                .Should().NotThrowAsync();
        }

        #endregion

        #region GetAvailableRewardsAsync Tests

        [Fact]
        public async Task GetAvailableRewardsAsync_ShouldReturnOnlyActiveRewardsWithStock()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            context.RewardCatalogues.AddRange(
                new RewardCatalogue
                {
                    RewardId = 1,
                    RewardName = "Available Item 1",
                    RewardCategory = "Electronics",
                    Points = 100,
                    StockQuantity = 10,
                    Availability = true
                },
                new RewardCatalogue
                {
                    RewardId = 2,
                    RewardName = "Available Item 2",
                    RewardCategory = "Books",
                    Points = 50,
                    StockQuantity = 5,
                    Availability = true
                },
                new RewardCatalogue
                {
                    RewardId = 3,
                    RewardName = "Inactive Item",
                    RewardCategory = "Electronics",
                    Points = 100,
                    StockQuantity = 10,
                    Availability = false
                },
                new RewardCatalogue
                {
                    RewardId = 4,
                    RewardName = "Out of Stock Item",
                    RewardCategory = "Books",
                    Points = 75,
                    StockQuantity = 0,
                    Availability = true
                }
            );
            await context.SaveChangesAsync();

            var repository = new RewardCatalogueRepository(context);

            // Act
            var result = await repository.GetAvailableRewardsAsync();

            // Assert
            result.Should().HaveCount(2);
            result.Should().AllSatisfy(r => r.Availability.Should().BeTrue());
            result.Should().AllSatisfy(r => r.StockQuantity.Should().BeGreaterThan(0));
            result.Should().Contain(r => r.RewardId == 1);
            result.Should().Contain(r => r.RewardId == 2);
        }

        [Fact]
        public async Task GetAvailableRewardsAsync_WithNoAvailableRewards_ShouldReturnEmptyList()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            context.RewardCatalogues.AddRange(
                new RewardCatalogue
                {
                    RewardId = 1,
                    RewardName = "Inactive",
                    RewardCategory = "Test",
                    Points = 100,
                    StockQuantity = 10,
                    Availability = false
                },
                new RewardCatalogue
                {
                    RewardId = 2,
                    RewardName = "Out of Stock",
                    RewardCategory = "Test",
                    Points = 100,
                    StockQuantity = 0,
                    Availability = true
                }
            );
            await context.SaveChangesAsync();

            var repository = new RewardCatalogueRepository(context);

            // Act
            var result = await repository.GetAvailableRewardsAsync();

            // Assert
            result.Should().BeEmpty();
        }

        #endregion

        #region GetRewardsByCategoryAsync Tests

        [Fact]
        public async Task GetRewardsByCategoryAsync_ShouldReturnRewardsInSpecifiedCategory()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            context.RewardCatalogues.AddRange(
                new RewardCatalogue
                {
                    RewardId = 1,
                    RewardName = "Electronics Item 1",
                    RewardCategory = "Electronics",
                    Points = 200,
                    StockQuantity = 10,
                    Availability = true
                },
                new RewardCatalogue
                {
                    RewardId = 2,
                    RewardName = "Electronics Item 2",
                    RewardCategory = "Electronics",
                    Points = 100,
                    StockQuantity = 5,
                    Availability = true
                },
                new RewardCatalogue
                {
                    RewardId = 3,
                    RewardName = "Book Item",
                    RewardCategory = "Books",
                    Points = 50,
                    StockQuantity = 15,
                    Availability = true
                }
            );
            await context.SaveChangesAsync();

            var repository = new RewardCatalogueRepository(context);

            // Act
            var result = await repository.GetRewardsByCategoryAsync("Electronics");

            // Assert
            result.Should().HaveCount(2);
            result.Should().AllSatisfy(r => r.RewardCategory.Should().Be("Electronics"));
            result.Should().BeInAscendingOrder(r => r.Points);
            result.ToList()[0].Points.Should().Be(100);
            result.ToList()[1].Points.Should().Be(200);
        }

        [Fact]
        public async Task GetRewardsByCategoryAsync_WithNonExistentCategory_ShouldReturnEmptyList()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            context.RewardCatalogues.Add(new RewardCatalogue
            {
                RewardId = 1,
                RewardName = "Test Item",
                RewardCategory = "Electronics",
                Points = 100,
                StockQuantity = 10,
                Availability = true
            });
            await context.SaveChangesAsync();

            var repository = new RewardCatalogueRepository(context);

            // Act
            var result = await repository.GetRewardsByCategoryAsync("NonExistent");

            // Assert
            result.Should().BeEmpty();
        }

        #endregion

        #region GetAllRewardCategoriesAsync Tests

        [Fact]
        public async Task GetAllRewardCategoriesAsync_ShouldReturnDistinctCategories()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            context.RewardCatalogues.AddRange(
                new RewardCatalogue
                {
                    RewardId = 1,
                    RewardName = "Item 1",
                    RewardCategory = "Electronics",
                    Points = 100,
                    StockQuantity = 10,
                    Availability = true
                },
                new RewardCatalogue
                {
                    RewardId = 2,
                    RewardName = "Item 2",
                    RewardCategory = "Electronics",
                    Points = 200,
                    StockQuantity = 5,
                    Availability = true
                },
                new RewardCatalogue
                {
                    RewardId = 3,
                    RewardName = "Item 3",
                    RewardCategory = "Books",
                    Points = 50,
                    StockQuantity = 15,
                    Availability = true
                },
                new RewardCatalogue
                {
                    RewardId = 4,
                    RewardName = "Item 4",
                    RewardCategory = "Vouchers",
                    Points = 75,
                    StockQuantity = 20,
                    Availability = true
                }
            );
            await context.SaveChangesAsync();

            var repository = new RewardCatalogueRepository(context);

            // Act
            var result = await repository.GetAllRewardCategoriesAsync();

            // Assert
            result.Should().HaveCount(3);
            result.Should().Contain("Electronics");
            result.Should().Contain("Books");
            result.Should().Contain("Vouchers");
            result.Should().BeInAscendingOrder();
        }

        [Fact]
        public async Task GetAllRewardCategoriesAsync_WithNoRewards_ShouldReturnEmptyList()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var repository = new RewardCatalogueRepository(context);

            // Act
            var result = await repository.GetAllRewardCategoriesAsync();

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAllRewardCategoriesAsync_ShouldNotReturnDuplicates()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            context.RewardCatalogues.AddRange(
                new RewardCatalogue
                {
                    RewardId = 1,
                    RewardName = "Item 1",
                    RewardCategory = "Electronics",
                    Points = 100,
                    StockQuantity = 10,
                    Availability = true
                },
                new RewardCatalogue
                {
                    RewardId = 2,
                    RewardName = "Item 2",
                    RewardCategory = "Electronics",
                    Points = 200,
                    StockQuantity = 5,
                    Availability = true
                },
                new RewardCatalogue
                {
                    RewardId = 3,
                    RewardName = "Item 3",
                    RewardCategory = "Electronics",
                    Points = 300,
                    StockQuantity = 3,
                    Availability = true
                }
            );
            await context.SaveChangesAsync();

            var repository = new RewardCatalogueRepository(context);

            // Act
            var result = await repository.GetAllRewardCategoriesAsync();

            // Assert
            result.Should().HaveCount(1);
            result.Should().Contain("Electronics");
        }

        #endregion
    }
}
