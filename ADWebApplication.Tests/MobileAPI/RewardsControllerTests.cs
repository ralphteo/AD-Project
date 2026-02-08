using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using ADWebApplication.Controllers;
using ADWebApplication.Data;
using ADWebApplication.Models;
using ADWebApplication.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ADWebApplication.Tests.MobileAPI
{
    public class RewardsControllerTests
    {
        private static In5niteDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<In5niteDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            return new In5niteDbContext(options);
        }

        #region GetSummary Tests

        [Fact]
        public async Task GetSummary_ReturnsCorrectSummary_WhenWalletExists()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var user = new PublicUser { Email = "test@example.com", Name = "Test", PhoneNumber = "12345678", IsActive = true, Password = "hash" };
            var wallet = new RewardWallet { UserId = user.Id, AvailablePoints = 500 };
            user.RewardWallet = wallet;
            dbContext.PublicUser.Add(user);
            await dbContext.SaveChangesAsync();

            var controller = new RewardsController(dbContext);

            // Act
            var result = await controller.GetSummary(user.Id);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var summary = Assert.IsType<RewardsSummaryDto>(okResult.Value);
            Assert.Equal(500, summary.TotalPoints);
        }

        [Fact]
        public async Task GetSummary_ReturnsEmptySummary_WhenWalletDoesNotExist()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var controller = new RewardsController(dbContext);

            // Act
            var result = await controller.GetSummary(999);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var summary = Assert.IsType<RewardsSummaryDto>(okResult.Value);
            Assert.Equal(0, summary.TotalPoints);
        }

        #endregion

        #region GetHistory Tests

        [Fact]
        public async Task GetHistory_ReturnsEmptyList_WhenNoWallet()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var controller = new RewardsController(dbContext);

            // Act
            var result = await controller.GetHistory(999);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var history = Assert.IsType<List<RewardsHistoryDto>>(okResult.Value);
            Assert.Empty(history);
        }

        [Fact]
        public async Task GetHistory_ReturnsCompletedTransactions_Only()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var user = new PublicUser { Email = "history@example.com", Name = "History", PhoneNumber = "12345678", IsActive = true, Password = "hash" };
            var wallet = new RewardWallet { UserId = user.Id, AvailablePoints = 100 };
            user.RewardWallet = wallet;
            dbContext.PublicUser.Add(user);
            await dbContext.SaveChangesAsync();

            var transactions = new List<PointTransaction>
            {
                new PointTransaction { WalletId = wallet.WalletId, Points = 50, Status = "COMPLETED", TransactionType = "DISPOSAL", CreatedDateTime = DateTime.UtcNow },
                new PointTransaction { WalletId = wallet.WalletId, Points = 30, Status = "PENDING", TransactionType = "DISPOSAL", CreatedDateTime = DateTime.UtcNow }
            };
            dbContext.PointTransactions.AddRange(transactions);
            await dbContext.SaveChangesAsync();

            var controller = new RewardsController(dbContext);

            // Act
            var result = await controller.GetHistory(user.Id);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var history = Assert.IsType<List<RewardsHistoryDto>>(okResult.Value);
            Assert.Single(history);
        }

        #endregion

        #region GetWallet Tests

        [Fact]
        public async Task GetWallet_ReturnsWalletDetails_WhenExists()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var user = new PublicUser { Email = "wallet@example.com", Name = "Wallet", PhoneNumber = "12345678", IsActive = true, Password = "hash" };
            var wallet = new RewardWallet { UserId = user.Id, AvailablePoints = 250 };
            user.RewardWallet = wallet;
            dbContext.PublicUser.Add(user);
            await dbContext.SaveChangesAsync();

            var controller = new RewardsController(dbContext);

            // Act
            var result = await controller.GetWallet(user.Id);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var walletDto = Assert.IsType<RewardWalletDto>(okResult.Value);
            Assert.Equal(user.Id, walletDto.UserId);
            Assert.Equal(250, walletDto.AvailablePoints);
        }

        [Fact]
        public async Task GetWallet_ReturnsEmptyWallet_WhenDoesNotExist()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var controller = new RewardsController(dbContext);

            // Act
            var result = await controller.GetWallet(999);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var walletDto = Assert.IsType<RewardWalletDto>(okResult.Value);
            Assert.Equal(999, walletDto.UserId);
            Assert.Equal(0, walletDto.AvailablePoints);
        }

        #endregion

        #region GetCatalogue Tests

        [Fact]
        public async Task GetCatalogue_ReturnsAvailableRewards_Only()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var rewards = new List<RewardCatalogue>
            {
                new RewardCatalogue { RewardName = "Gift Card", Points = 100, Availability = true, StockQuantity = 10, Description = "Test", RewardCategory = "Voucher", ImageUrl = "img.jpg", CreatedDate = DateTime.UtcNow, UpdatedDate = DateTime.UtcNow },
                new RewardCatalogue { RewardName = "Unavailable", Points = 200, Availability = false, StockQuantity = 5, Description = "Test", RewardCategory = "Voucher", ImageUrl = "img.jpg", CreatedDate = DateTime.UtcNow, UpdatedDate = DateTime.UtcNow },
                new RewardCatalogue { RewardName = "Out of Stock", Points = 150, Availability = true, StockQuantity = 0, Description = "Test", RewardCategory = "Voucher", ImageUrl = "img.jpg", CreatedDate = DateTime.UtcNow, UpdatedDate = DateTime.UtcNow }
            };
            dbContext.RewardCatalogues.AddRange(rewards);
            await dbContext.SaveChangesAsync();

            var controller = new RewardsController(dbContext);

            // Act
            var result = await controller.GetCatalogue();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var catalogue = Assert.IsType<List<RewardCatalogueDto>>(okResult.Value);
            Assert.Single(catalogue);
        }

        [Fact]
        public async Task GetCatalogue_ReturnsEmptyList_WhenNoRewards()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var controller = new RewardsController(dbContext);

            // Act
            var result = await controller.GetCatalogue();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var catalogue = Assert.IsType<List<RewardCatalogueDto>>(okResult.Value);
            Assert.Empty(catalogue);
        }

        #endregion

        #region Redeem Tests

        [Fact]
        public async Task Redeem_SucceedsWithValidRequest()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var user = new PublicUser { Email = "redeem@example.com", Name = "Redeem", PhoneNumber = "12345678", IsActive = true, Password = "hash" };
            var wallet = new RewardWallet { UserId = user.Id, AvailablePoints = 500 };
            user.RewardWallet = wallet;
            dbContext.PublicUser.Add(user);

            var reward = new RewardCatalogue 
            { 
                RewardName = "Gift Card", 
                Points = 100, 
                Availability = true, 
                StockQuantity = 10, 
                Description = "Test", 
                RewardCategory = "Voucher", 
                ImageUrl = "img.jpg",
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };
            dbContext.RewardCatalogues.Add(reward);
            await dbContext.SaveChangesAsync();

            var controller = new RewardsController(dbContext);
            var request = new RedeemRequestDto { UserId = user.Id, RewardId = reward.RewardId };

            // Act
            var result = await controller.Redeem(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<RedeemResponseDto>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal(400, response.RemainingPoints);
        }

        [Fact]
        public async Task Redeem_FailsWhenInsufficientPoints()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var user = new PublicUser { Email = "poor@example.com", Name = "Poor", PhoneNumber = "12345678", IsActive = true, Password = "hash" };
            var wallet = new RewardWallet { UserId = user.Id, AvailablePoints = 50 };
            user.RewardWallet = wallet;
            dbContext.PublicUser.Add(user);

            var reward = new RewardCatalogue 
            { 
                RewardName = "Expensive", 
                Points = 100, 
                Availability = true, 
                StockQuantity = 10, 
                Description = "Test", 
                RewardCategory = "Voucher", 
                ImageUrl = "img.jpg",
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };
            dbContext.RewardCatalogues.Add(reward);
            await dbContext.SaveChangesAsync();

            var controller = new RewardsController(dbContext);
            var request = new RedeemRequestDto { UserId = user.Id, RewardId = reward.RewardId };

            // Act
            var result = await controller.Redeem(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<RedeemResponseDto>(okResult.Value);
            Assert.False(response.Success);
            Assert.Equal("Insufficient points", response.Message);
        }

        [Fact]
        public async Task Redeem_FailsWhenRewardUnavailable()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var user = new PublicUser { Email = "user@example.com", Name = "User", PhoneNumber = "12345678", IsActive = true, Password = "hash" };
            var wallet = new RewardWallet { UserId = user.Id, AvailablePoints = 500 };
            user.RewardWallet = wallet;
            dbContext.PublicUser.Add(user);

            var reward = new RewardCatalogue 
            { 
                RewardName = "Unavailable", 
                Points = 100, 
                Availability = false, 
                StockQuantity = 10, 
                Description = "Test", 
                RewardCategory = "Voucher", 
                ImageUrl = "img.jpg",
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };
            dbContext.RewardCatalogues.Add(reward);
            await dbContext.SaveChangesAsync();

            var controller = new RewardsController(dbContext);
            var request = new RedeemRequestDto { UserId = user.Id, RewardId = reward.RewardId };

            // Act
            var result = await controller.Redeem(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<RedeemResponseDto>(okResult.Value);
            Assert.False(response.Success);
            Assert.Equal("Reward unavailable", response.Message);
        }

        [Fact]
        public async Task Redeem_FailsWhenWalletNotFound()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var controller = new RewardsController(dbContext);
            var request = new RedeemRequestDto { UserId = 999, RewardId = 1 };

            // Act
            var result = await controller.Redeem(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<RedeemResponseDto>(okResult.Value);
            Assert.False(response.Success);
            Assert.Equal("Wallet not found", response.Message);
        }

        [Fact]
        public async Task Redeem_ReturnsBadRequest_WhenRequestIsNull()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var controller = new RewardsController(dbContext);

            // Act
            var result = await controller.Redeem(null!);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var response = Assert.IsType<RedeemResponseDto>(badRequestResult.Value);
            Assert.False(response.Success);
        }

        #endregion

        #region GetRedemptions Tests

        [Fact]
        public async Task GetRedemptions_ReturnsUserRedemptions()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var user = new PublicUser { Email = "redemptions@example.com", Name = "Redemptions", PhoneNumber = "12345678", IsActive = true, Password = "hash" };
            var wallet = new RewardWallet { UserId = user.Id, AvailablePoints = 100 };
            user.RewardWallet = wallet;
            dbContext.PublicUser.Add(user);

            var reward = new RewardCatalogue 
            { 
                RewardName = "Gift", 
                Points = 50, 
                Availability = true, 
                StockQuantity = 10, 
                Description = "Test", 
                RewardCategory = "Voucher", 
                ImageUrl = "img.jpg",
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };
            dbContext.RewardCatalogues.Add(reward);
            await dbContext.SaveChangesAsync();

            var redemption = new RewardRedemption
            {
                UserId = user.Id,
                WalletId = wallet.WalletId,
                RewardId = reward.RewardId,
                PointsUsed = 50,
                RedemptionStatus = "COMPLETED",
                RedemptionDateTime = DateTime.UtcNow
            };
            dbContext.RewardRedemptions.Add(redemption);
            await dbContext.SaveChangesAsync();

            var controller = new RewardsController(dbContext);

            // Act
            var result = await controller.GetRedemptions(user.Id);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var redemptions = Assert.IsType<List<RewardRedemptionItemDto>>(okResult.Value);
            Assert.Single(redemptions);
        }

        [Fact]
        public async Task GetRedemptions_ReturnsEmptyList_WhenNoRedemptions()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var controller = new RewardsController(dbContext);

            // Act
            var result = await controller.GetRedemptions(999);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var redemptions = Assert.IsType<List<RewardRedemptionItemDto>>(okResult.Value);
            Assert.Empty(redemptions);
        }

        #endregion
    }
}
