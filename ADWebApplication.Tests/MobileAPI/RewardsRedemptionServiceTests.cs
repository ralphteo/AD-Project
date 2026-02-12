using System;
using System.Threading.Tasks;
using ADWebApplication.Data;
using ADWebApplication.Models;
using ADWebApplication.Models.DTOs;
using ADWebApplication.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ADWebApplication.Tests.Services.Mobile
{
    public class RewardsRedemptionServiceTests
    {
        private static In5niteDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<In5niteDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            return new In5niteDbContext(options);
        }

        [Fact]
        public async Task RedeemAsync_Fails_WhenWalletMissing()
        {
            var db = CreateInMemoryDbContext();
            var service = new RewardsRedemptionService(db);

            var result = await service.RedeemAsync(1, new RedeemRequestDto { UserId = 1, RewardId = 1 });

            Assert.False(result.Success);
            Assert.Equal("Wallet not found", result.Message);
        }

        [Fact]
        public async Task RedeemAsync_Succeeds_WhenValid()
        {
            var db = CreateInMemoryDbContext();
            var user = new PublicUser { Email = "redeem@example.com", Name = "Redeem", PhoneNumber = "123", IsActive = true, Password = "hash" };
            var wallet = new RewardWallet { UserId = user.Id, AvailablePoints = 300 };
            user.RewardWallet = wallet;
            db.PublicUser.Add(user);

            var reward = new RewardCatalogue
            {
                RewardName = "Gift",
                Points = 100,
                Availability = true,
                StockQuantity = 5,
                Description = "Test",
                RewardCategory = "Voucher",
                ImageUrl = "img.jpg",
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };
            db.RewardCatalogues.Add(reward);
            await db.SaveChangesAsync();

            var service = new RewardsRedemptionService(db);
            var result = await service.RedeemAsync(user.Id, new RedeemRequestDto { UserId = user.Id, RewardId = reward.RewardId });

            Assert.True(result.Success);
            Assert.Equal(200, result.RemainingPoints);
        }

        [Fact]
        public async Task UseRedemptionAsync_Fails_WhenInvalidVendorCode()
        {
            var db = CreateInMemoryDbContext();
            var service = new RewardsRedemptionService(db);

            var result = await service.UseRedemptionAsync(1, new UseRedemptionRequestDto
            {
                RedemptionId = 1,
                VendorCode = "1111"
            });

            Assert.False(result.Success);
            Assert.Equal("Invalid vendor code", result.Message);
        }
    }
}
