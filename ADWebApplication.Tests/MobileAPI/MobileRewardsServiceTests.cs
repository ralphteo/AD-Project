using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ADWebApplication.Data;
using ADWebApplication.Models.DTOs;
using ADWebApplication.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace ADWebApplication.Tests.MobileAPI
{
    public class MobileRewardsServiceTests
    {
        private static In5niteDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<In5niteDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new In5niteDbContext(options);
        }

        [Fact]
        public async Task GetSummaryAsync_DelegatesToWalletService()
        {
            var db = CreateInMemoryDbContext();
            var wallet = new Mock<IWalletService>();
            var redemption = new Mock<IRewardsRedemptionService>();
            wallet.Setup(x => x.GetSummaryAsync(1))
                .ReturnsAsync(new RewardsSummaryDto { TotalPoints = 123 });

            var service = new MobileRewardsService(db, wallet.Object, redemption.Object);

            var result = await service.GetSummaryAsync(1);

            Assert.Equal(123, result.TotalPoints);
            wallet.Verify(x => x.GetSummaryAsync(1), Times.Once);
        }

        [Fact]
        public async Task GetHistoryAsync_DelegatesToWalletService()
        {
            var db = CreateInMemoryDbContext();
            var wallet = new Mock<IWalletService>();
            var redemption = new Mock<IRewardsRedemptionService>();
            wallet.Setup(x => x.GetHistoryAsync(2))
                .ReturnsAsync(new List<RewardsHistoryDto>());

            var service = new MobileRewardsService(db, wallet.Object, redemption.Object);

            var result = await service.GetHistoryAsync(2);

            Assert.Empty(result);
            wallet.Verify(x => x.GetHistoryAsync(2), Times.Once);
        }

        [Fact]
        public async Task RedeemAsync_DelegatesToRedemptionService()
        {
            var db = CreateInMemoryDbContext();
            var wallet = new Mock<IWalletService>();
            var redemption = new Mock<IRewardsRedemptionService>();
            redemption.Setup(x => x.RedeemAsync(3, It.IsAny<RedeemRequestDto>()))
                .ReturnsAsync(new RedeemResponseDto { Success = true });

            var service = new MobileRewardsService(db, wallet.Object, redemption.Object);

            var result = await service.RedeemAsync(3, new RedeemRequestDto { UserId = 3, RewardId = 1 });

            Assert.True(result.Success);
            redemption.Verify(x => x.RedeemAsync(3, It.IsAny<RedeemRequestDto>()), Times.Once);
        }
    }
}
