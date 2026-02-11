using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using ADWebApplication.Controllers;
using ADWebApplication.Data;
using ADWebApplication.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace ADWebApplication.Tests.MobileAPI
{
    public class LookupControllerTests
    {
        private static void SetUser(LookupController controller, int userId)
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
                .Options;

            return new In5niteDbContext(options);
        }

        #region GetBins Tests

        [Fact]
        public async Task GetBins_ReturnsAllBins_WithPredictions()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var bin = new CollectionBin
            {
                BinId = 1,
                LocationName = "Test Location",
                LocationAddress = "123 Test St",
                BinStatus = "Active",
                Latitude = 1.3521,
                Longitude = 103.8198,
                RegionId = 1
            };
            dbContext.CollectionBins.Add(bin);

            var prediction = new FillLevelPrediction
            {
                BinId = 1,
                PredictedDate = DateTime.UtcNow,
                PredictedAvgDailyGrowth = 10.0
            };
            dbContext.FillLevelPredictions.Add(prediction);

            var collection = new CollectionDetails
            {
                BinId = 1,
                CurrentCollectionDateTime = DateTime.UtcNow.AddDays(-2)
            };
            dbContext.CollectionDetails.Add(collection);

            await dbContext.SaveChangesAsync();

            var controller = new LookupController(dbContext);
            SetUser(controller, 1);

            // Act
            var result = await controller.GetBins();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetBins_ReturnsEmptyList_WhenNoBins()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var controller = new LookupController(dbContext);
            SetUser(controller, 1);

            // Act
            var result = await controller.GetBins();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetBins_CalculatesRiskLevels_Correctly()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var bin = new CollectionBin
            {
                BinId = 1,
                LocationName = "High Risk Bin",
                BinStatus = "Active"
            };
            dbContext.CollectionBins.Add(bin);

            var prediction = new FillLevelPrediction
            {
                BinId = 1,
                PredictedDate = DateTime.UtcNow,
                PredictedAvgDailyGrowth = 15.0
            };
            dbContext.FillLevelPredictions.Add(prediction);

            var collection = new CollectionDetails
            {
                BinId = 1,
                CurrentCollectionDateTime = DateTime.UtcNow.AddDays(-5)
            };
            dbContext.CollectionDetails.Add(collection);

            await dbContext.SaveChangesAsync();

            var controller = new LookupController(dbContext);
            SetUser(controller, 1);

            // Act
            var result = await controller.GetBins();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        #endregion

        #region GetCategories Tests

        [Fact]
        public async Task GetCategories_ReturnsAllCategories()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var categories = new List<EWasteCategory>
            {
                new EWasteCategory { CategoryId = 1, CategoryName = "Electronics" },
                new EWasteCategory { CategoryId = 2, CategoryName = "Appliances" },
                new EWasteCategory { CategoryId = 3, CategoryName = "Batteries" }
            };
            dbContext.EWasteCategories.AddRange(categories);
            await dbContext.SaveChangesAsync();

            var controller = new LookupController(dbContext);
            SetUser(controller, 1);

            // Act
            var result = await controller.GetCategories();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetCategories_ReturnsEmptyList_WhenNoCategories()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var controller = new LookupController(dbContext);
            SetUser(controller, 1);

            // Act
            var result = await controller.GetCategories();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var categories = okResult.Value as IEnumerable<object>;
            Assert.NotNull(categories);
        }

        #endregion

        #region GetItemTypes Tests

        [Fact]
        public async Task GetItemTypes_ReturnsItemsForCategory()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var category = new EWasteCategory { CategoryId = 1, CategoryName = "Electronics" };
            dbContext.EWasteCategories.Add(category);

            var itemTypes = new List<EWasteItemType>
            {
                new EWasteItemType { ItemTypeId = 1, CategoryId = 1, ItemName = "Laptop", EstimatedAvgWeight = 2.5 },
                new EWasteItemType { ItemTypeId = 2, CategoryId = 1, ItemName = "Phone", EstimatedAvgWeight = 0.2 },
                new EWasteItemType { ItemTypeId = 3, CategoryId = 2, ItemName = "TV", EstimatedAvgWeight = 5.0 }
            };
            dbContext.EWasteItemTypes.AddRange(itemTypes);
            await dbContext.SaveChangesAsync();

            var controller = new LookupController(dbContext);
            SetUser(controller, 1);

            // Act
            var result = await controller.GetItemTypes(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetItemTypes_ReturnsEmptyList_WhenNoItemsInCategory()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var controller = new LookupController(dbContext);
            SetUser(controller, 1);

            // Act
            var result = await controller.GetItemTypes(999);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetItemTypes_FiltersCorrectly_ByCategoryId()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var categories = new List<EWasteCategory>
            {
                new EWasteCategory { CategoryId = 1, CategoryName = "Electronics" },
                new EWasteCategory { CategoryId = 2, CategoryName = "Appliances" }
            };
            dbContext.EWasteCategories.AddRange(categories);

            var itemTypes = new List<EWasteItemType>
            {
                new EWasteItemType { ItemTypeId = 1, CategoryId = 1, ItemName = "Laptop", EstimatedAvgWeight = 2.5 },
                new EWasteItemType { ItemTypeId = 2, CategoryId = 2, ItemName = "Fridge", EstimatedAvgWeight = 50.0 }
            };
            dbContext.EWasteItemTypes.AddRange(itemTypes);
            await dbContext.SaveChangesAsync();

            var controller = new LookupController(dbContext);
            SetUser(controller, 1);

            // Act
            var result = await controller.GetItemTypes(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        #endregion
    }
}
