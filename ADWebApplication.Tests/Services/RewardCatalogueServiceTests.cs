using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ADWebApplication.Models;
using ADWebApplication.Services;
using ADWebApplication.Data.Repository;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ADWebApplication.Tests.Services
{
    public class RewardCatalogueServiceTests
    {
        private class FakeRepo : IRewardCatalogueRepository
        {
            public RewardCatalogue? LastSaved;
            public Task<int> AddRewardAsync(RewardCatalogue reward)
            {
                LastSaved = reward;
                return Task.FromResult(7);
            }
            public Task<bool> DeleteRewardAsync(int rewardId) => Task.FromResult(true);
            public Task<IEnumerable<RewardCatalogue>> GetAllRewardsAsync() => Task.FromResult<IEnumerable<RewardCatalogue>>(new List<RewardCatalogue>());
            public Task<RewardCatalogue?> GetRewardByIdAsync(int rewardId) => Task.FromResult<RewardCatalogue?>(null);
            public Task<IEnumerable<RewardCatalogue>> GetAvailableRewardsAsync() => Task.FromResult<IEnumerable<RewardCatalogue>>(new List<RewardCatalogue>());
            public Task<IEnumerable<string>> GetAllRewardCategoriesAsync() => Task.FromResult<IEnumerable<string>>(new List<string>());
            public Task<IEnumerable<RewardCatalogue>> GetRewardsByCategoryAsync(string category) => Task.FromResult<IEnumerable<RewardCatalogue>>(new List<RewardCatalogue>());
            public Task<bool> UpdateRewardAsync(RewardCatalogue reward) => Task.FromResult(true);
        }

        [Fact]
        public async Task AddRewardAsync_WithZeroPoints_Throws()
        {
            var repo = new FakeRepo();
            var svc = new RewardCatalogueService(repo, NullLogger<RewardCatalogueService>.Instance);
            var reward = new RewardCatalogue { Points = 0, StockQuantity = 1 };

            await Assert.ThrowsAsync<InvalidOperationException>(() => svc.AddRewardAsync(reward));
        }

        [Fact]
        public async Task CheckRewardAvailabilityAsync_ReturnsTrueWhenAvailable()
        {
            IRewardCatalogueRepository repo = new FakeRepoWithGet(new RewardCatalogue { RewardId = 1, Availability = true, StockQuantity = 5 });
            var svc = new RewardCatalogueService(repo, NullLogger<RewardCatalogueService>.Instance);

            var ok = await svc.CheckRewardAvailabilityAsync(1, 3);
            ok.Should().BeTrue();
        }

        [Fact]
        public async Task AddRewardAsync_WithNegativeStock_Throws()
        {
            var repo = new FakeRepo();
            var svc = new RewardCatalogueService(repo, NullLogger<RewardCatalogueService>.Instance);
            var reward = new RewardCatalogue { Points = 100, StockQuantity = -1 };

            await Assert.ThrowsAsync<InvalidOperationException>(() => svc.AddRewardAsync(reward));
        }

        [Fact]
        public async Task AddRewardAsync_WithValidReward_ReturnsId()
        {
            var repo = new FakeRepo();
            var svc = new RewardCatalogueService(repo, NullLogger<RewardCatalogueService>.Instance);
            var reward = new RewardCatalogue { Points = 100, StockQuantity = 10 };

            var id = await svc.AddRewardAsync(reward);

            id.Should().Be(7);
            repo.LastSaved.Should().NotBeNull();
        }

        [Fact]
        public async Task UpdateRewardAsync_WhenStockZeroAndAvailable_SetsAvailabilityFalse()
        {
            var repo = new FakeRepo();
            var svc = new RewardCatalogueService(repo, NullLogger<RewardCatalogueService>.Instance);
            var reward = new RewardCatalogue { RewardId = 1, StockQuantity = 0, Availability = true };

            await svc.UpdateRewardAsync(reward);

            reward.Availability.Should().BeFalse();
        }

        [Fact]
        public async Task UpdateRewardAsync_ReturnsTrue()
        {
            var repo = new FakeRepo();
            var svc = new RewardCatalogueService(repo, NullLogger<RewardCatalogueService>.Instance);
            var reward = new RewardCatalogue { RewardId = 1, StockQuantity = 5, Availability = true };

            var result = await svc.UpdateRewardAsync(reward);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task DeleteRewardAsync_ReturnsTrue()
        {
            var repo = new FakeRepo();
            var svc = new RewardCatalogueService(repo, NullLogger<RewardCatalogueService>.Instance);

            var result = await svc.DeleteRewardAsync(1);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task GetAllRewardsAsync_ReturnsEmptyList()
        {
            var repo = new FakeRepo();
            var svc = new RewardCatalogueService(repo, NullLogger<RewardCatalogueService>.Instance);

            var result = await svc.GetAllRewardsAsync();

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetRewardByIdAsync_ReturnsNull()
        {
            var repo = new FakeRepo();
            var svc = new RewardCatalogueService(repo, NullLogger<RewardCatalogueService>.Instance);

            var result = await svc.GetRewardByIdAsync(1);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetAvailableRewardsAsync_ReturnsEmptyList()
        {
            var repo = new FakeRepo();
            var svc = new RewardCatalogueService(repo, NullLogger<RewardCatalogueService>.Instance);

            var result = await svc.GetAvailableRewardsAsync();

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAllRewardCategoriesAsync_ReturnsEmptyList()
        {
            var repo = new FakeRepo();
            var svc = new RewardCatalogueService(repo, NullLogger<RewardCatalogueService>.Instance);

            var result = await svc.GetAllRewardCategoriesAsync();

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetRewardsByCategoryAsync_ReturnsEmptyList()
        {
            var repo = new FakeRepo();
            var svc = new RewardCatalogueService(repo, NullLogger<RewardCatalogueService>.Instance);

            var result = await svc.GetRewardsByCategoryAsync("Electronics");

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task CheckRewardAvailabilityAsync_ReturnsFalseWhenNotFound()
        {
            var repo = new FakeRepo();
            var svc = new RewardCatalogueService(repo, NullLogger<RewardCatalogueService>.Instance);

            var result = await svc.CheckRewardAvailabilityAsync(999);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task CheckRewardAvailabilityAsync_ReturnsFalseWhenUnavailable()
        {
            IRewardCatalogueRepository repo = new FakeRepoWithGet(new RewardCatalogue { RewardId = 1, Availability = false, StockQuantity = 5 });
            var svc = new RewardCatalogueService(repo, NullLogger<RewardCatalogueService>.Instance);

            var result = await svc.CheckRewardAvailabilityAsync(1);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task CheckRewardAvailabilityAsync_ReturnsFalseWhenInsufficientStock()
        {
            IRewardCatalogueRepository repo = new FakeRepoWithGet(new RewardCatalogue { RewardId = 1, Availability = true, StockQuantity = 2 });
            var svc = new RewardCatalogueService(repo, NullLogger<RewardCatalogueService>.Instance);

            var result = await svc.CheckRewardAvailabilityAsync(1, 5);

            result.Should().BeFalse();
        }

        [Fact]
        public void Constructor_ThrowsWhenRepositoryIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => 
                new RewardCatalogueService(null!, NullLogger<RewardCatalogueService>.Instance));
        }

        [Fact]
        public void Constructor_ThrowsWhenLoggerIsNull()
        {
            var repo = new FakeRepo();
            Assert.Throws<ArgumentNullException>(() => 
                new RewardCatalogueService(repo, null!));
        }

        private class FakeRepoWithGet : IRewardCatalogueRepository
        {
            private readonly RewardCatalogue _r;
            public FakeRepoWithGet(RewardCatalogue r) { _r = r; }
            public Task<int> AddRewardAsync(RewardCatalogue reward) => Task.FromResult(1);
            public Task<bool> DeleteRewardAsync(int rewardId) => Task.FromResult(true);
            public Task<IEnumerable<RewardCatalogue>> GetAllRewardsAsync() => Task.FromResult<IEnumerable<RewardCatalogue>>(new List<RewardCatalogue>());
            public Task<RewardCatalogue?> GetRewardByIdAsync(int rewardId) => Task.FromResult<RewardCatalogue?>(_r);
            public Task<IEnumerable<RewardCatalogue>> GetAvailableRewardsAsync() => Task.FromResult<IEnumerable<RewardCatalogue>>(new List<RewardCatalogue>());
            public Task<IEnumerable<string>> GetAllRewardCategoriesAsync() => Task.FromResult<IEnumerable<string>>(new List<string>());
            public Task<IEnumerable<RewardCatalogue>> GetRewardsByCategoryAsync(string category) => Task.FromResult<IEnumerable<RewardCatalogue>>(new List<RewardCatalogue>());
            public Task<bool> UpdateRewardAsync(RewardCatalogue reward) => Task.FromResult(true);
        }
    }
}
