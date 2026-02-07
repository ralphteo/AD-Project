using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using ADWebApplication.Controllers;
using ADWebApplication.Data;
using ADWebApplication.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ADWebApplication.Tests
{
    public class TestControllerTests
    {
        private static In5niteDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<In5niteDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new In5niteDbContext(options);
        }

        private static Mock<IConfiguration> CreateMockConfiguration(string? connectionString)
        {
            var mockConfiguration = new Mock<IConfiguration>();
            var mockSection = new Mock<IConfigurationSection>();
            
            mockSection.Setup(x => x.Value).Returns(connectionString);
            
            mockConfiguration
                .Setup(x => x.GetSection("ConnectionStrings"))
                .Returns(mockSection.Object);
            
            mockConfiguration
                .Setup(x => x.GetSection("ConnectionStrings:DefaultConnection"))
                .Returns(mockSection.Object);

            return mockConfiguration;
        }

        private static Mock<ILogger<TestController>> CreateMockLogger()
        {
            return new Mock<ILogger<TestController>>();
        }

        #region TestMySql Tests

        [Fact]
        public async Task TestMySql_ReturnsServerError_WhenConnectionStringIsNull()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var mockConfiguration = CreateMockConfiguration(null);
            var mockLogger = CreateMockLogger();
            var controller = new TestController(mockConfiguration.Object, mockLogger.Object, dbContext);

            // Act
            var result = await controller.TestMySql();

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.Equal("Connection string not found", statusCodeResult.Value);
        }

        [Fact]
        public async Task TestMySql_ReturnsServerError_WhenConnectionStringIsEmpty()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var mockConfiguration = CreateMockConfiguration(string.Empty);
            var mockLogger = CreateMockLogger();
            var controller = new TestController(mockConfiguration.Object, mockLogger.Object, dbContext);

            // Act
            var result = await controller.TestMySql();

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.Equal("Connection string not found", statusCodeResult.Value);
        }

        [Fact]
        public async Task TestMySql_LogsInformation_WhenMethodIsCalled()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var mockConfiguration = CreateMockConfiguration("Server=localhost;Database=test");
            var mockLogger = CreateMockLogger();
            var controller = new TestController(mockConfiguration.Object, mockLogger.Object, dbContext);

            // Act
            try
            {
                await controller.TestMySql();
            }
            catch
            {
                // Expected to fail since connection string is not valid
            }

            // Assert
            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Testing Azure MySQL connection")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region CreateTestUser Tests

        [Fact]
        public async Task CreateTestUser_CreatesNewUser_WhenUserDoesNotExist()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var mockConfiguration = CreateMockConfiguration("Server=localhost");
            var mockLogger = CreateMockLogger();
            var controller = new TestController(mockConfiguration.Object, mockLogger.Object, dbContext);

            // Act
            var result = await controller.CreateTestUser();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;

            // Verify response structure
            var messageProperty = response!.GetType().GetProperty("message");
            var userIdProperty = response.GetType().GetProperty("userId");
            var pointsProperty = response.GetType().GetProperty("points");

            Assert.NotNull(messageProperty);
            Assert.NotNull(userIdProperty);
            Assert.NotNull(pointsProperty);

            var message = messageProperty!.GetValue(response)?.ToString();
            var userId = (int?)userIdProperty!.GetValue(response);
            var points = (int?)pointsProperty!.GetValue(response);

            Assert.Equal("Test user created successfully", message);
            Assert.True(userId > 0);
            Assert.Equal(0, points);

            // Verify user was created in database
            var createdUser = await dbContext.PublicUser
                .Include(u => u.RewardWallet)
                .FirstOrDefaultAsync(u => u.Email == "test@in5nite.sg");

            Assert.NotNull(createdUser);
            Assert.Equal("test@in5nite.sg", createdUser!.Email);
            Assert.Equal("Test User", createdUser.Name);
            Assert.Equal("00000000", createdUser.PhoneNumber);
            Assert.Null(createdUser.RegionId);
            Assert.True(createdUser.IsActive);
            Assert.Equal("Test", createdUser.Password);
            Assert.NotNull(createdUser.RewardWallet);
            Assert.Equal(0, createdUser.RewardWallet!.AvailablePoints);
        }

        [Fact]
        public async Task CreateTestUser_ReturnsExistingUser_WhenUserAlreadyExists()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var mockConfiguration = CreateMockConfiguration("Server=localhost");
            var mockLogger = CreateMockLogger();

            // Create existing user
            var existingUser = new PublicUser
            {
                Email = "test@in5nite.sg",
                Name = "Existing Test User",
                PhoneNumber = "11111111",
                IsActive = true,
                Password = "ExistingPass",
                RewardWallet = new RewardWallet
                {
                    AvailablePoints = 100
                }
            };
            dbContext.PublicUser.Add(existingUser);
            await dbContext.SaveChangesAsync();

            var controller = new TestController(mockConfiguration.Object, mockLogger.Object, dbContext);

            // Act
            var result = await controller.CreateTestUser();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;

            var messageProperty = response!.GetType().GetProperty("message");
            var userIdProperty = response.GetType().GetProperty("userId");
            var pointsProperty = response.GetType().GetProperty("points");

            var message = messageProperty!.GetValue(response)?.ToString();
            var userId = (int?)userIdProperty!.GetValue(response);
            var points = (int?)pointsProperty!.GetValue(response);

            Assert.Equal("Test user already exists", message);
            Assert.Equal(existingUser.Id, userId);
            Assert.Equal(100, points);

            // Verify no new user was created
            var userCount = await dbContext.PublicUser.CountAsync(u => u.Email == "test@in5nite.sg");
            Assert.Equal(1, userCount);
        }

        [Fact]
        public async Task CreateTestUser_CreatesRewardWallet_WithZeroPoints()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var mockConfiguration = CreateMockConfiguration("Server=localhost");
            var mockLogger = CreateMockLogger();
            var controller = new TestController(mockConfiguration.Object, mockLogger.Object, dbContext);

            // Act
            await controller.CreateTestUser();

            // Assert
            var user = await dbContext.PublicUser
                .Include(u => u.RewardWallet)
                .FirstOrDefaultAsync(u => u.Email == "test@in5nite.sg");

            Assert.NotNull(user?.RewardWallet);
            Assert.Equal(0, user!.RewardWallet!.AvailablePoints);
        }

        [Fact]
        public async Task CreateTestUser_SetsCorrectUserProperties()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var mockConfiguration = CreateMockConfiguration("Server=localhost");
            var mockLogger = CreateMockLogger();
            var controller = new TestController(mockConfiguration.Object, mockLogger.Object, dbContext);

            // Act
            await controller.CreateTestUser();

            // Assert
            var user = await dbContext.PublicUser.FirstOrDefaultAsync(u => u.Email == "test@in5nite.sg");

            Assert.NotNull(user);
            Assert.Equal("test@in5nite.sg", user!.Email);
            Assert.Equal("Test User", user.Name);
            Assert.Equal("00000000", user.PhoneNumber);
            Assert.Null(user.RegionId);
            Assert.True(user.IsActive);
            Assert.Equal("Test", user.Password);
        }

        [Fact]
        public async Task CreateTestUser_ReturnsOkResult_WithCorrectResponseStructure()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var mockConfiguration = CreateMockConfiguration("Server=localhost");
            var mockLogger = CreateMockLogger();
            var controller = new TestController(mockConfiguration.Object, mockLogger.Object, dbContext);

            // Act
            var result = await controller.CreateTestUser();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);

            var response = okResult.Value;
            var responseType = response!.GetType();

            // Verify the anonymous object has the expected properties
            Assert.NotNull(responseType.GetProperty("message"));
            Assert.NotNull(responseType.GetProperty("userId"));
            Assert.NotNull(responseType.GetProperty("points"));
        }

        [Fact]
        public async Task CreateTestUser_IncludesRewardWallet_WhenCheckingExistingUser()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var mockConfiguration = CreateMockConfiguration("Server=localhost");
            var mockLogger = CreateMockLogger();

            // Create user with wallet
            var existingUser = new PublicUser
            {
                Email = "test@in5nite.sg",
                Name = "Test",
                PhoneNumber = "12345678",
                IsActive = true,
                Password = "Pass",
                RewardWallet = new RewardWallet
                {
                    AvailablePoints = 250
                }
            };
            dbContext.PublicUser.Add(existingUser);
            await dbContext.SaveChangesAsync();

            var controller = new TestController(mockConfiguration.Object, mockLogger.Object, dbContext);

            // Act
            var result = await controller.CreateTestUser();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            var pointsProperty = response!.GetType().GetProperty("points");
            var points = (int?)pointsProperty!.GetValue(response);

            Assert.Equal(250, points);
        }

        [Fact]
        public async Task CreateTestUser_UsesCorrectTestEmail()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var mockConfiguration = CreateMockConfiguration("Server=localhost");
            var mockLogger = CreateMockLogger();
            var controller = new TestController(mockConfiguration.Object, mockLogger.Object, dbContext);

            // Act
            await controller.CreateTestUser();

            // Assert
            var user = await dbContext.PublicUser.FirstOrDefaultAsync(u => u.Email == "test@in5nite.sg");
            Assert.NotNull(user);

            // Verify no other test users were created
            var otherTestUsers = await dbContext.PublicUser
                .Where(u => u.Email != "test@in5nite.sg")
                .CountAsync();
            Assert.Equal(0, otherTestUsers);
        }

        [Fact]
        public async Task CreateTestUser_PersistsUserToDatabase()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var mockConfiguration = CreateMockConfiguration("Server=localhost");
            var mockLogger = CreateMockLogger();
            var controller = new TestController(mockConfiguration.Object, mockLogger.Object, dbContext);

            // Act
            await controller.CreateTestUser();

            // Assert - Create a new context instance to verify persistence
            var verificationContext = CreateInMemoryDbContext();
            
            // Copy the in-memory data for verification
            var usersInMemory = await dbContext.PublicUser.Include(u => u.RewardWallet).ToListAsync();
            Assert.Single(usersInMemory);
            
            var user = usersInMemory[0];
            Assert.Equal("test@in5nite.sg", user.Email);
            Assert.NotNull(user.RewardWallet);
        }

        #endregion
    }
}
