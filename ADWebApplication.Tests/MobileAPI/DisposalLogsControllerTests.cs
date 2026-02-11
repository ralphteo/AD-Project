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
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace ADWebApplication.Tests.MobileAPI
{
    public class DisposalLogsControllerTests
    {
        private static void SetUser(DisposalLogsController controller, int userId)
        {
            var identity = new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) },
                "TestAuth");
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };
        }
        private static In5niteDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<In5niteDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            return new In5niteDbContext(options);
        }

        #region Create Tests

        [Fact]
        public async Task Create_CreatesDisposalLog_AndAwardsPoints()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var user = new PublicUser 
            { 
                Email = "disposal@example.com", 
                Name = "Disposal", 
                PhoneNumber = "12345678", 
                IsActive = true, 
                Password = "hash" 
            };
            var wallet = new RewardWallet { UserId = user.Id, AvailablePoints = 0 };
            user.RewardWallet = wallet;
            dbContext.PublicUser.Add(user);

            var category = new EWasteCategory { CategoryName = "Electronics" };
            dbContext.EWasteCategories.Add(category);
            await dbContext.SaveChangesAsync();

            var itemType = new EWasteItemType 
            { 
                CategoryId = category.CategoryId, 
                ItemName = "Laptop", 
                EstimatedAvgWeight = 2.5,
                BasePoints = 50
            };
            dbContext.EWasteItemTypes.Add(itemType);

            var bin = new CollectionBin 
            { 
                LocationName = "Test Bin", 
                BinStatus = "Active" 
            };
            dbContext.CollectionBins.Add(bin);
            await dbContext.SaveChangesAsync();

            var controller = new DisposalLogsController(dbContext);
            SetUser(controller, user.Id);
            var request = new CreateDisposalLogRequest
            {
                UserId = user.Id,
                BinId = bin.BinId,
                ItemTypeId = itemType.ItemTypeId,
                EstimatedWeightKg = 2.5,
                SerialNo = "SN123456",
                Feedback = "Good condition"
            };

            // Act
            var result = await controller.Create(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);

            // Verify disposal log created
            var log = await dbContext.DisposalLogs.FirstOrDefaultAsync();
            Assert.NotNull(log);
            Assert.Equal(user.Id, log!.UserId);
            Assert.Equal(bin.BinId, log.BinId);

            // Verify points awarded
            var updatedWallet = await dbContext.RewardWallet.FirstOrDefaultAsync(w => w.UserId == user.Id);
            Assert.Equal(50, updatedWallet!.AvailablePoints);
        }

        [Fact]
        public async Task Create_CreatesWallet_IfNotExists()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var user = new PublicUser 
            { 
                Email = "nowallet@example.com", 
                Name = "NoWallet", 
                PhoneNumber = "12345678", 
                IsActive = true, 
                Password = "hash" 
            };
            dbContext.PublicUser.Add(user);

            var category = new EWasteCategory { CategoryName = "Electronics" };
            dbContext.EWasteCategories.Add(category);
            await dbContext.SaveChangesAsync();

            var itemType = new EWasteItemType 
            { 
                CategoryId = category.CategoryId, 
                ItemName = "Phone", 
                EstimatedAvgWeight = 0.2,
                BasePoints = 20
            };
            dbContext.EWasteItemTypes.Add(itemType);

            var bin = new CollectionBin { LocationName = "Test Bin", BinStatus = "Active" };
            dbContext.CollectionBins.Add(bin);
            await dbContext.SaveChangesAsync();

            var controller = new DisposalLogsController(dbContext);
            SetUser(controller, user.Id);
            var request = new CreateDisposalLogRequest
            {
                UserId = user.Id,
                BinId = bin.BinId,
                ItemTypeId = itemType.ItemTypeId,
                EstimatedWeightKg = 0.2,
                SerialNo = "PHONE123"
            };

            // Act
            var result = await controller.Create(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            
            // Verify wallet was created
            var wallet = await dbContext.RewardWallet.FirstOrDefaultAsync(w => w.UserId == user.Id);
            Assert.NotNull(wallet);
            Assert.Equal(20, wallet!.AvailablePoints);
        }

        [Fact]
        public async Task Create_CreatesDisposalLogItem()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var user = new PublicUser { Email = "item@example.com", Name = "Item", PhoneNumber = "12345678", IsActive = true, Password = "hash" };
            dbContext.PublicUser.Add(user);

            var category = new EWasteCategory { CategoryName = "Batteries" };
            dbContext.EWasteCategories.Add(category);
            await dbContext.SaveChangesAsync();

            var itemType = new EWasteItemType 
            { 
                CategoryId = category.CategoryId, 
                ItemName = "AA Battery", 
                EstimatedAvgWeight = 0.05,
                BasePoints = 5
            };
            dbContext.EWasteItemTypes.Add(itemType);

            var bin = new CollectionBin { LocationName = "Test Bin", BinStatus = "Active" };
            dbContext.CollectionBins.Add(bin);
            await dbContext.SaveChangesAsync();

            var controller = new DisposalLogsController(dbContext);
            SetUser(controller, user.Id);
            var request = new CreateDisposalLogRequest
            {
                UserId = user.Id,
                BinId = bin.BinId,
                ItemTypeId = itemType.ItemTypeId,
                EstimatedWeightKg = 0.05,
                SerialNo = "BATT001"
            };

            // Act
            await controller.Create(request);

            // Assert
            var item = await dbContext.DisposalLogItems.FirstOrDefaultAsync();
            Assert.NotNull(item);
            Assert.Equal(itemType.ItemTypeId, item!.ItemTypeId);
            Assert.Equal("BATT001", item.SerialNo);
        }

        [Fact]
        public async Task Create_CreatesPointTransaction()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var user = new PublicUser { Email = "trans@example.com", Name = "Trans", PhoneNumber = "12345678", IsActive = true, Password = "hash" };
            var wallet = new RewardWallet { UserId = user.Id, AvailablePoints = 0 };
            user.RewardWallet = wallet;
            dbContext.PublicUser.Add(user);

            var category = new EWasteCategory { CategoryName = "Appliances" };
            dbContext.EWasteCategories.Add(category);
            await dbContext.SaveChangesAsync();

            var itemType = new EWasteItemType 
            { 
                CategoryId = category.CategoryId, 
                ItemName = "Microwave", 
                EstimatedAvgWeight = 15.0,
                BasePoints = 100
            };
            dbContext.EWasteItemTypes.Add(itemType);

            var bin = new CollectionBin { LocationName = "Test Bin", BinStatus = "Active" };
            dbContext.CollectionBins.Add(bin);
            await dbContext.SaveChangesAsync();

            var controller = new DisposalLogsController(dbContext);
            SetUser(controller, user.Id);
            var request = new CreateDisposalLogRequest
            {
                UserId = user.Id,
                BinId = bin.BinId,
                ItemTypeId = itemType.ItemTypeId,
                EstimatedWeightKg = 15.0,
                SerialNo = "MW12345"
            };

            // Act
            await controller.Create(request);

            // Assert
            var transaction = await dbContext.PointTransactions.FirstOrDefaultAsync();
            Assert.NotNull(transaction);
            Assert.Equal(100, transaction!.Points);
            Assert.Equal("DISPOSAL", transaction.TransactionType);
            Assert.Equal("COMPLETED", transaction.Status);
        }

        [Fact]
        public async Task Create_HandlesZeroPoints()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var user = new PublicUser { Email = "zero@example.com", Name = "Zero", PhoneNumber = "12345678", IsActive = true, Password = "hash" };
            var wallet = new RewardWallet { UserId = user.Id, AvailablePoints = 50 };
            user.RewardWallet = wallet;
            dbContext.PublicUser.Add(user);

            var category = new EWasteCategory { CategoryName = "Other" };
            dbContext.EWasteCategories.Add(category);
            await dbContext.SaveChangesAsync();

            var itemType = new EWasteItemType 
            { 
                CategoryId = category.CategoryId, 
                ItemName = "Cable", 
                EstimatedAvgWeight = 0.1,
                BasePoints = 0
            };
            dbContext.EWasteItemTypes.Add(itemType);

            var bin = new CollectionBin { LocationName = "Test Bin", BinStatus = "Active" };
            dbContext.CollectionBins.Add(bin);
            await dbContext.SaveChangesAsync();

            var controller = new DisposalLogsController(dbContext);
            SetUser(controller, user.Id);
            var request = new CreateDisposalLogRequest
            {
                UserId = user.Id,
                BinId = bin.BinId,
                ItemTypeId = itemType.ItemTypeId,
                EstimatedWeightKg = 0.1,
                SerialNo = "CBL001"
            };

            // Act
            await controller.Create(request);

            // Assert
            var updatedWallet = await dbContext.RewardWallet.FirstOrDefaultAsync(w => w.UserId == user.Id);
            Assert.Equal(50, updatedWallet!.AvailablePoints); // No change

            // No transaction should be created for zero points
            var transactions = await dbContext.PointTransactions.CountAsync();
            Assert.Equal(0, transactions);
        }

        #endregion

        #region GetHistory Tests

        [Fact]
        public async Task GetHistory_ReturnsUserDisposals()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var user = new PublicUser { Email = "history@example.com", Name = "History", PhoneNumber = "12345678", IsActive = true, Password = "hash" };
            dbContext.PublicUser.Add(user);

            var bin = new CollectionBin { LocationName = "Test Location", BinStatus = "Active" };
            dbContext.CollectionBins.Add(bin);

            var category = new EWasteCategory { CategoryName = "Electronics" };
            dbContext.EWasteCategories.Add(category);
            await dbContext.SaveChangesAsync();

            var itemType = new EWasteItemType 
            { 
                CategoryId = category.CategoryId, 
                ItemName = "Tablet", 
                EstimatedAvgWeight = 0.5,
                BasePoints = 30
            };
            dbContext.EWasteItemTypes.Add(itemType);
            await dbContext.SaveChangesAsync();

            var log = new DisposalLogs
            {
                UserId = user.Id,
                BinId = bin.BinId,
                EstimatedTotalWeight = 0.5,
                DisposalTimeStamp = DateTime.UtcNow
            };
            dbContext.DisposalLogs.Add(log);
            await dbContext.SaveChangesAsync();

            var item = new DisposalLogItem
            {
                LogId = log.LogId,
                ItemTypeId = itemType.ItemTypeId,
                SerialNo = "TAB001"
            };
            dbContext.DisposalLogItems.Add(item);
            await dbContext.SaveChangesAsync();

            var controller = new DisposalLogsController(dbContext);
            SetUser(controller, user.Id);

            // Act
            var result = await controller.GetHistory(user.Id, "all");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var history = Assert.IsType<List<DisposalHistoryDto>>(okResult.Value);
            Assert.Single(history);
        }

        [Fact]
        public async Task GetHistory_FiltersByMonth_Correctly()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var user = new PublicUser { Email = "month@example.com", Name = "Month", PhoneNumber = "12345678", IsActive = true, Password = "hash" };
            dbContext.PublicUser.Add(user);

            var category = new EWasteCategory { CategoryName = "Test" };
            dbContext.EWasteCategories.Add(category);
            await dbContext.SaveChangesAsync();

            var itemType = new EWasteItemType { CategoryId = category.CategoryId, ItemName = "Item", EstimatedAvgWeight = 1.0, BasePoints = 10 };
            dbContext.EWasteItemTypes.Add(itemType);

            var bin = new CollectionBin { LocationName = "Test", BinStatus = "Active" };
            dbContext.CollectionBins.Add(bin);
            await dbContext.SaveChangesAsync();

            var log1 = new DisposalLogs { UserId = user.Id, BinId = bin.BinId, DisposalTimeStamp = DateTime.UtcNow, EstimatedTotalWeight = 1.0 };
            var log2 = new DisposalLogs { UserId = user.Id, BinId = bin.BinId, DisposalTimeStamp = DateTime.UtcNow.AddMonths(-2), EstimatedTotalWeight = 1.0 };
            dbContext.DisposalLogs.AddRange(log1, log2);
            await dbContext.SaveChangesAsync();

            var item1 = new DisposalLogItem { LogId = log1.LogId, ItemTypeId = itemType.ItemTypeId, SerialNo = "SN1" };
            var item2 = new DisposalLogItem { LogId = log2.LogId, ItemTypeId = itemType.ItemTypeId, SerialNo = "SN2" };
            dbContext.DisposalLogItems.AddRange(item1, item2);
            await dbContext.SaveChangesAsync();

            var controller = new DisposalLogsController(dbContext);
            SetUser(controller, user.Id);

            // Act
            var result = await controller.GetHistory(user.Id, "month");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var history = Assert.IsType<List<DisposalHistoryDto>>(okResult.Value);
            Assert.Single(history);
        }

        [Fact]
        public async Task GetHistory_ReturnsEmptyList_WhenNoDisposals()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var controller = new DisposalLogsController(dbContext);
            SetUser(controller, 999);

            // Act
            var result = await controller.GetHistory(999, "all");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var history = Assert.IsType<List<DisposalHistoryDto>>(okResult.Value);
            Assert.Empty(history);
        }

        [Fact]
        public async Task GetHistory_FiltersByLast3Months_Correctly()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var user = new PublicUser { Email = "last3@example.com", Name = "Last3", PhoneNumber = "12345678", IsActive = true, Password = "hash" };
            dbContext.PublicUser.Add(user);

            var category = new EWasteCategory { CategoryName = "Test" };
            dbContext.EWasteCategories.Add(category);
            await dbContext.SaveChangesAsync();

            var itemType = new EWasteItemType { CategoryId = category.CategoryId, ItemName = "Item", EstimatedAvgWeight = 1.0, BasePoints = 10 };
            dbContext.EWasteItemTypes.Add(itemType);

            var bin = new CollectionBin { LocationName = "Test", BinStatus = "Active" };
            dbContext.CollectionBins.Add(bin);
            await dbContext.SaveChangesAsync();

            var log1 = new DisposalLogs { UserId = user.Id, BinId = bin.BinId, DisposalTimeStamp = DateTime.UtcNow, EstimatedTotalWeight = 1.0 };
            var log2 = new DisposalLogs { UserId = user.Id, BinId = bin.BinId, DisposalTimeStamp = DateTime.UtcNow.AddMonths(-2), EstimatedTotalWeight = 1.0 };
            var log3 = new DisposalLogs { UserId = user.Id, BinId = bin.BinId, DisposalTimeStamp = DateTime.UtcNow.AddMonths(-4), EstimatedTotalWeight = 1.0 };
            dbContext.DisposalLogs.AddRange(log1, log2, log3);
            await dbContext.SaveChangesAsync();

            var item1 = new DisposalLogItem { LogId = log1.LogId, ItemTypeId = itemType.ItemTypeId, SerialNo = "SN1" };
            var item2 = new DisposalLogItem { LogId = log2.LogId, ItemTypeId = itemType.ItemTypeId, SerialNo = "SN2" };
            var item3 = new DisposalLogItem { LogId = log3.LogId, ItemTypeId = itemType.ItemTypeId, SerialNo = "SN3" };
            dbContext.DisposalLogItems.AddRange(item1, item2, item3);
            await dbContext.SaveChangesAsync();

            var controller = new DisposalLogsController(dbContext);
            SetUser(controller, user.Id);

            // Act
            var result = await controller.GetHistory(user.Id, "last 3");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var history = Assert.IsType<List<DisposalHistoryDto>>(okResult.Value);
            Assert.Equal(2, history.Count);
        }

        #endregion
    }
}
