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
    public class CampaignServiceTests
    {
        private class FakeCampaignRepository : ICampaignRepository
        {
            public Campaign? LastSaved;
            public Func<int> AddResult = () => 42;
            public Task<int> AddCampaignAsync(Campaign campaign)
            {
                LastSaved = campaign;
                return Task.FromResult(AddResult());
            }
            public Task<bool> DeleteCampaignAsync(int campaignId) => Task.FromResult(true);
            public Task<IEnumerable<Campaign>> GetActiveCampaignsAsync() => Task.FromResult<IEnumerable<Campaign>>(new List<Campaign>());
            public Task<IEnumerable<Campaign>> GetAllCampaignsAsync() => Task.FromResult<IEnumerable<Campaign>>(new List<Campaign>());
            public Task<Campaign?> GetCampaignByIdAsync(int campaignId) => Task.FromResult<Campaign?>(null);
            public Task<IEnumerable<Campaign>> GetScheduledCampaignsAsync() => Task.FromResult<IEnumerable<Campaign>>(new List<Campaign>());
            public Task<IEnumerable<Campaign>> GetByStatusAsync(string status) => Task.FromResult<IEnumerable<Campaign>>(new List<Campaign>());
            public Task<Campaign?> GetCurrentCampaignAsync() => Task.FromResult<Campaign?>(null);
            public Task<bool> UpdateCampaignAsync(Campaign campaign)
            {
                LastSaved = campaign;
                return Task.FromResult(true);
            }
        }

        [Fact]
        public async Task AddCampaignAsync_WithEndBeforeStart_Throws()
        {
            var repo = new FakeCampaignRepository();
            var svc = new CampaignService(repo, NullLogger<CampaignService>.Instance);
            var campaign = new Campaign { StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(-1), IncentiveValue = 1 };

            await Assert.ThrowsAsync<InvalidOperationException>(() => svc.AddCampaignAsync(campaign));
        }

        [Fact]
        public async Task AddCampaignAsync_WithNegativeIncentive_Throws()
        {
            var repo = new FakeCampaignRepository();
            var svc = new CampaignService(repo, NullLogger<CampaignService>.Instance);
            var campaign = new Campaign { StartDate = DateTime.UtcNow.AddDays(-1), EndDate = DateTime.UtcNow.AddDays(1), IncentiveValue = -5 };

            await Assert.ThrowsAsync<InvalidOperationException>(() => svc.AddCampaignAsync(campaign));
        }

        [Fact]
        public async Task AddCampaignAsync_WithFutureStart_SetsPlannedStatusAndReturnsId()
        {
            var repo = new FakeCampaignRepository();
            repo.AddResult = () => 123;
            var svc = new CampaignService(repo, NullLogger<CampaignService>.Instance);
            var campaign = new Campaign { StartDate = DateTime.UtcNow.AddDays(10), EndDate = DateTime.UtcNow.AddDays(20), IncentiveValue = 1 };

            var id = await svc.AddCampaignAsync(campaign);

            id.Should().Be(123);
            repo.LastSaved.Should().NotBeNull();
            repo.LastSaved!.Status.Should().Be("Planned");
        }

        [Fact]
        public async Task ActivateCampaignAsync_WhenNotFound_ReturnsFalse()
        {
            var repo = new FakeCampaignRepository();
            var svc = new CampaignService(repo, NullLogger<CampaignService>.Instance);

            var result = await svc.ActivateCampaignAsync(999);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task AddCampaignAsync_WithPastEndDate_SetsCompletedStatus()
        {
            var repo = new FakeCampaignRepository();
            var svc = new CampaignService(repo, NullLogger<CampaignService>.Instance);
            var campaign = new Campaign { StartDate = DateTime.UtcNow.AddDays(-10), EndDate = DateTime.UtcNow.AddDays(-1), IncentiveValue = 1 };

            await svc.AddCampaignAsync(campaign);

            repo.LastSaved.Should().NotBeNull();
            repo.LastSaved!.Status.Should().Be("Completed");
        }

        [Fact]
        public async Task UpdateCampaignAsync_WithEndBeforeStart_Throws()
        {
            var repo = new FakeCampaignRepository();
            var svc = new CampaignService(repo, NullLogger<CampaignService>.Instance);
            var campaign = new Campaign { CampaignId = 1, StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(-1), IncentiveValue = 1 };

            await Assert.ThrowsAsync<InvalidOperationException>(() => svc.UpdateCampaignAsync(campaign));
        }

        [Fact]
        public async Task UpdateCampaignAsync_WithNegativeIncentive_Throws()
        {
            var repo = new FakeCampaignRepository();
            var svc = new CampaignService(repo, NullLogger<CampaignService>.Instance);
            var campaign = new Campaign { CampaignId = 1, StartDate = DateTime.UtcNow.AddDays(-1), EndDate = DateTime.UtcNow.AddDays(1), IncentiveValue = -5 };

            await Assert.ThrowsAsync<InvalidOperationException>(() => svc.UpdateCampaignAsync(campaign));
        }

        [Fact]
        public async Task UpdateCampaignAsync_WithFutureStart_SetsPlannedStatus()
        {
            var repo = new FakeCampaignRepository();
            var svc = new CampaignService(repo, NullLogger<CampaignService>.Instance);
            var campaign = new Campaign { CampaignId = 1, StartDate = DateTime.UtcNow.AddDays(10), EndDate = DateTime.UtcNow.AddDays(20), IncentiveValue = 1 };

            var result = await svc.UpdateCampaignAsync(campaign);

            result.Should().BeTrue();
            repo.LastSaved!.Status.Should().Be("Planned");
        }

        [Fact]
        public async Task UpdateCampaignAsync_WithPastEndDate_SetsCompletedStatus()
        {
            var repo = new FakeCampaignRepository();
            var svc = new CampaignService(repo, NullLogger<CampaignService>.Instance);
            var campaign = new Campaign { CampaignId = 1, StartDate = DateTime.UtcNow.AddDays(-10), EndDate = DateTime.UtcNow.AddDays(-1), IncentiveValue = 1 };

            var result = await svc.UpdateCampaignAsync(campaign);

            result.Should().BeTrue();
            repo.LastSaved!.Status.Should().Be("Completed");
        }

        [Fact]
        public async Task DeleteCampaignAsync_WithActiveCampaign_Throws()
        {
            var repo = new FakeCampaignRepositoryWithData(new Campaign { CampaignId = 1, Status = "Active" });
            var svc = new CampaignService(repo, NullLogger<CampaignService>.Instance);

            await Assert.ThrowsAsync<InvalidOperationException>(() => svc.DeleteCampaignAsync(1));
        }

        [Fact]
        public async Task DeleteCampaignAsync_WhenNotFound_ReturnsFalse()
        {
            var repo = new FakeCampaignRepository();
            var svc = new CampaignService(repo, NullLogger<CampaignService>.Instance);

            var result = await svc.DeleteCampaignAsync(999);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task DeleteCampaignAsync_WithInactiveCampaign_ReturnsTrue()
        {
            var repo = new FakeCampaignRepositoryWithData(new Campaign { CampaignId = 1, Status = "Inactive" });
            var svc = new CampaignService(repo, NullLogger<CampaignService>.Instance);

            var result = await svc.DeleteCampaignAsync(1);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task GetActiveCampaignsAsync_ReturnsEmptyList()
        {
            var repo = new FakeCampaignRepository();
            var svc = new CampaignService(repo, NullLogger<CampaignService>.Instance);

            var result = await svc.GetActiveCampaignsAsync();

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAllCampaignsAsync_ReturnsEmptyList()
        {
            var repo = new FakeCampaignRepository();
            var svc = new CampaignService(repo, NullLogger<CampaignService>.Instance);

            var result = await svc.GetAllCampaignsAsync();

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetCampaignByIdAsync_ReturnsNull()
        {
            var repo = new FakeCampaignRepository();
            var svc = new CampaignService(repo, NullLogger<CampaignService>.Instance);

            var result = await svc.GetCampaignByIdAsync(1);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetCurrentCampaignAsync_ReturnsNull()
        {
            var repo = new FakeCampaignRepository();
            var svc = new CampaignService(repo, NullLogger<CampaignService>.Instance);

            var result = await svc.GetCurrentCampaignAsync();

            result.Should().BeNull();
        }

        [Fact]
        public async Task ActivateCampaignAsync_OutsideDateRange_Throws()
        {
            var campaign = new Campaign { CampaignId = 1, StartDate = DateTime.UtcNow.AddDays(10), EndDate = DateTime.UtcNow.AddDays(20) };
            var repo = new FakeCampaignRepositoryWithData(campaign);
            var svc = new CampaignService(repo, NullLogger<CampaignService>.Instance);

            await Assert.ThrowsAsync<InvalidOperationException>(() => svc.ActivateCampaignAsync(1));
        }

        [Fact]
        public async Task ActivateCampaignAsync_WithinDateRange_ReturnsTrue()
        {
            var campaign = new Campaign { CampaignId = 1, StartDate = DateTime.UtcNow.AddDays(-1), EndDate = DateTime.UtcNow.AddDays(1), Status = "Planned" };
            var repo = new FakeCampaignRepositoryWithData(campaign);
            var svc = new CampaignService(repo, NullLogger<CampaignService>.Instance);

            var result = await svc.ActivateCampaignAsync(1);

            result.Should().BeTrue();
            campaign.Status.Should().Be("ACTIVE");
        }

        [Fact]
        public async Task DeactivateCampaignAsync_WhenNotFound_ReturnsFalse()
        {
            var repo = new FakeCampaignRepository();
            var svc = new CampaignService(repo, NullLogger<CampaignService>.Instance);

            var result = await svc.DeactivateCampaignAsync(999);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task DeactivateCampaignAsync_WhenFound_ReturnsTrue()
        {
            var campaign = new Campaign { CampaignId = 1, Status = "ACTIVE" };
            var repo = new FakeCampaignRepositoryWithData(campaign);
            var svc = new CampaignService(repo, NullLogger<CampaignService>.Instance);

            var result = await svc.DeactivateCampaignAsync(1);

            result.Should().BeTrue();
            campaign.Status.Should().Be("INACTIVE");
        }

        [Fact]
        public async Task CalculateTotalIncentivesAsync_WithMultiplier_ReturnsMultiplied()
        {
            var campaign = new Campaign { IncentiveType = "Multiplier", IncentiveValue = 2.5m };
            var repo = new FakeCampaignRepositoryWithCurrentCampaign(campaign);
            var svc = new CampaignService(repo, NullLogger<CampaignService>.Instance);

            var result = await svc.CalculateTotalIncentivesAsync(100);

            result.Should().Be(250);
        }

        [Fact]
        public async Task CalculateTotalIncentivesAsync_WithBonus_ReturnsAdded()
        {
            var campaign = new Campaign { IncentiveType = "Bonus", IncentiveValue = 50 };
            var repo = new FakeCampaignRepositoryWithCurrentCampaign(campaign);
            var svc = new CampaignService(repo, NullLogger<CampaignService>.Instance);

            var result = await svc.CalculateTotalIncentivesAsync(100);

            result.Should().Be(150);
        }

        [Fact]
        public async Task CalculateTotalIncentivesAsync_WithoutCampaign_ReturnsBasePoints()
        {
            var repo = new FakeCampaignRepository();
            var svc = new CampaignService(repo, NullLogger<CampaignService>.Instance);

            var result = await svc.CalculateTotalIncentivesAsync(100);

            result.Should().Be(100);
        }

        [Fact]
        public async Task CalculateTotalIncentivesAsync_WithUnknownType_ReturnsBasePoints()
        {
            var campaign = new Campaign { IncentiveType = "Unknown", IncentiveValue = 50 };
            var repo = new FakeCampaignRepositoryWithCurrentCampaign(campaign);
            var svc = new CampaignService(repo, NullLogger<CampaignService>.Instance);

            var result = await svc.CalculateTotalIncentivesAsync(100);

            result.Should().Be(100);
        }

        [Fact]
        public void Constructor_ThrowsWhenRepositoryIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => 
                new CampaignService(null!, NullLogger<CampaignService>.Instance));
        }

        [Fact]
        public void Constructor_ThrowsWhenLoggerIsNull()
        {
            var repo = new FakeCampaignRepository();
            Assert.Throws<ArgumentNullException>(() => 
                new CampaignService(repo, null!));
        }

        private class FakeCampaignRepositoryWithData : ICampaignRepository
        {
            private readonly Campaign _campaign;
            public FakeCampaignRepositoryWithData(Campaign campaign) { _campaign = campaign; }
            public Task<int> AddCampaignAsync(Campaign campaign) => Task.FromResult(1);
            public Task<bool> DeleteCampaignAsync(int campaignId) => Task.FromResult(true);
            public Task<IEnumerable<Campaign>> GetActiveCampaignsAsync() => Task.FromResult<IEnumerable<Campaign>>(new List<Campaign>());
            public Task<IEnumerable<Campaign>> GetAllCampaignsAsync() => Task.FromResult<IEnumerable<Campaign>>(new List<Campaign>());
            public Task<Campaign?> GetCampaignByIdAsync(int campaignId) => Task.FromResult<Campaign?>(_campaign);
            public Task<IEnumerable<Campaign>> GetScheduledCampaignsAsync() => Task.FromResult<IEnumerable<Campaign>>(new List<Campaign>());
            public Task<IEnumerable<Campaign>> GetByStatusAsync(string status) => Task.FromResult<IEnumerable<Campaign>>(new List<Campaign>());
            public Task<Campaign?> GetCurrentCampaignAsync() => Task.FromResult<Campaign?>(null);
            public Task<bool> UpdateCampaignAsync(Campaign campaign) => Task.FromResult(true);
        }

        private class FakeCampaignRepositoryWithCurrentCampaign : ICampaignRepository
        {
            private readonly Campaign _campaign;
            public FakeCampaignRepositoryWithCurrentCampaign(Campaign campaign) { _campaign = campaign; }
            public Task<int> AddCampaignAsync(Campaign campaign) => Task.FromResult(1);
            public Task<bool> DeleteCampaignAsync(int campaignId) => Task.FromResult(true);
            public Task<IEnumerable<Campaign>> GetActiveCampaignsAsync() => Task.FromResult<IEnumerable<Campaign>>(new List<Campaign>());
            public Task<IEnumerable<Campaign>> GetAllCampaignsAsync() => Task.FromResult<IEnumerable<Campaign>>(new List<Campaign>());
            public Task<Campaign?> GetCampaignByIdAsync(int campaignId) => Task.FromResult<Campaign?>(null);
            public Task<IEnumerable<Campaign>> GetScheduledCampaignsAsync() => Task.FromResult<IEnumerable<Campaign>>(new List<Campaign>());
            public Task<IEnumerable<Campaign>> GetByStatusAsync(string status) => Task.FromResult<IEnumerable<Campaign>>(new List<Campaign>());
            public Task<Campaign?> GetCurrentCampaignAsync() => Task.FromResult<Campaign?>(_campaign);
            public Task<bool> UpdateCampaignAsync(Campaign campaign) => Task.FromResult(true);
        }
    }
}
