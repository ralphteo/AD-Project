using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ADWebApplication.Data;
using ADWebApplication.Models;
using ADWebApplication.Models.DTOs;
using ADWebApplication.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ADWebApplication.Tests.Services.Mobile
{
    public class WalletServiceTests
    {
        private static In5niteDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<In5niteDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new In5niteDbContext(options);
        }

        [Fact]
        public async Task GetSummaryAsync_ReturnsEmpty_WhenWalletMissing()
        {
            var db = CreateInMemoryDbContext();
            var service = new WalletService(db);

            var result = await service.GetSummaryAsync(1);

            Assert.Equal(0, result.TotalPoints);
            Assert.Equal(0, result.TotalDisposals);
            Assert.Equal(0, result.TotalRedeemed);
        }

        [Fact]
        public async Task GetSummaryAsync_ReturnsTotals_WhenWalletExists()
        {
            var db = CreateInMemoryDbContext();
            var user = new PublicUser { Email = "wallet@example.com", Name = "Wallet", PhoneNumber = "123", IsActive = true, Password = "hash" };
            var wallet = new RewardWallet { UserId = user.Id, AvailablePoints = 200 };
            user.RewardWallet = wallet;
            db.PublicUser.Add(user);
            db.DisposalLogs.Add(new DisposalLogs { UserId = user.Id });
            db.PointTransactions.Add(new PointTransaction
            {
                WalletId = wallet.WalletId,
                Points = -50,
                Status = "COMPLETED",
                CreatedDateTime = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            var service = new WalletService(db);
            var result = await service.GetSummaryAsync(user.Id);

            Assert.Equal(200, result.TotalPoints);
            Assert.Equal(1, result.TotalDisposals);
            Assert.Equal(1, result.TotalRedeemed);
        }

        [Fact]
        public async Task GetHistoryAsync_ReturnsEmpty_WhenWalletMissing()
        {
            var db = CreateInMemoryDbContext();
            var service = new WalletService(db);

            var result = await service.GetHistoryAsync(1);

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetWalletAsync_ReturnsZero_WhenWalletMissing()
        {
            var db = CreateInMemoryDbContext();
            var service = new WalletService(db);

            var result = await service.GetWalletAsync(9);

            Assert.Equal(9, result.UserId);
            Assert.Equal(0, result.AvailablePoints);
        }
    }
}
