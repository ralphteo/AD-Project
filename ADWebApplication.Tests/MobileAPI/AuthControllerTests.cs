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
    public class AuthControllerTests
    {
        private static In5niteDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<In5niteDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new In5niteDbContext(options);
        }

        #region GetRegions Tests

        [Fact]
        public async Task GetRegions_ReturnsOrderedRegions()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var regions = new List<Region>
            {
                new Region { RegionId = 1, RegionName = "Central" },
                new Region { RegionId = 2, RegionName = "East" },
                new Region { RegionId = 3, RegionName = "North" }
            };
            dbContext.Regions.AddRange(regions);
            await dbContext.SaveChangesAsync();

            var controller = new AuthController(dbContext);

            // Act
            var result = await controller.GetRegions();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedRegions = Assert.IsType<List<Region>>(okResult.Value, exactMatch: false);
            Assert.Equal(3, returnedRegions.Count);
            Assert.Equal("Central", returnedRegions[0].RegionName);
        }

        [Fact]
        public async Task GetRegions_ReturnsEmptyList_WhenNoRegions()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var controller = new AuthController(dbContext);

            // Act
            var result = await controller.GetRegions();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedRegions = Assert.IsType<List<Region>>(okResult.Value, exactMatch: false);
            Assert.Empty(returnedRegions);
        }

        #endregion

        #region Register Tests

        [Fact]
        public async Task Register_CreatesNewUser_WithValidData()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var controller = new AuthController(dbContext);
            var request = new RegisterRequest
            {
                Email = "test@example.com",
                FullName = "Test User",
                Phone = "12345678",
                RegionId = 1,
                Password = "password123"
            };

            // Act
            var result = await controller.Register(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<RegisterResponse>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal("Registered", response.Message);
            Assert.True(response.UserId > 0);

            // Verify user was created
            var user = await dbContext.PublicUser.FirstOrDefaultAsync(u => u.Email == "test@example.com");
            Assert.NotNull(user);
            Assert.Equal("Test User", user!.Name);
        }

        [Fact]
        public async Task Register_CreatesRewardWallet_WithZeroPoints()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var controller = new AuthController(dbContext);
            var request = new RegisterRequest
            {
                Email = "wallet@example.com",
                FullName = "Wallet User",
                Phone = "87654321",
                RegionId = 1,
                Password = "password123"
            };

            // Act
            await controller.Register(request);

            // Assert
            var user = await dbContext.PublicUser
                .Include(u => u.RewardWallet)
                .FirstOrDefaultAsync(u => u.Email == "wallet@example.com");

            Assert.NotNull(user?.RewardWallet);
            Assert.Equal(0, user!.RewardWallet!.AvailablePoints);
        }

        [Fact]
        public async Task Register_HashesPassword()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var controller = new AuthController(dbContext);
            var request = new RegisterRequest
            {
                Email = "secure@example.com",
                FullName = "Secure User",
                Phone = "11111111",
                RegionId = 1,
                Password = "mypassword"
            };

            // Act
            await controller.Register(request);

            // Assert
            var user = await dbContext.PublicUser.FirstOrDefaultAsync(u => u.Email == "secure@example.com");
            Assert.NotNull(user);
            Assert.NotEqual("mypassword", user!.Password);
            Assert.StartsWith("$2", user.Password); // BCrypt hash prefix
        }

        [Fact]
        public async Task Register_TrimsAndLowercasesEmail()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var controller = new AuthController(dbContext);
            var request = new RegisterRequest
            {
                Email = "  UPPER@Example.COM  ",
                FullName = "Test User",
                Phone = "12345678",
                RegionId = 1,
                Password = "password123"
            };

            // Act
            await controller.Register(request);

            // Assert
            var user = await dbContext.PublicUser.FirstOrDefaultAsync(u => u.Email == "upper@example.com");
            Assert.NotNull(user);
        }

        [Fact]
        public async Task Register_ReturnsConflict_WhenEmailExists()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var existingUser = new PublicUser
            {
                Email = "existing@example.com",
                Name = "Existing User",
                PhoneNumber = "99999999",
                IsActive = true,
                Password = "hashedpassword"
            };
            dbContext.PublicUser.Add(existingUser);
            await dbContext.SaveChangesAsync();

            var controller = new AuthController(dbContext);
            var request = new RegisterRequest
            {
                Email = "existing@example.com",
                FullName = "New User",
                Phone = "11111111",
                RegionId = 1,
                Password = "password123"
            };

            // Act
            var result = await controller.Register(request);

            // Assert
            var conflictResult = Assert.IsType<ConflictObjectResult>(result.Result);
            var response = Assert.IsType<RegisterResponse>(conflictResult.Value);
            Assert.False(response.Success);
            Assert.Equal("Email already registered", response.Message);
        }

        [Fact]
        public async Task Register_SetsUserIsActiveToTrue()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var controller = new AuthController(dbContext);
            var request = new RegisterRequest
            {
                Email = "active@example.com",
                FullName = "Active User",
                Phone = "22222222",
                RegionId = 1,
                Password = "password123"
            };

            // Act
            await controller.Register(request);

            // Assert
            var user = await dbContext.PublicUser.FirstOrDefaultAsync(u => u.Email == "active@example.com");
            Assert.True(user?.IsActive);
        }

        #endregion

        #region Login Tests

        [Fact]
        public async Task Login_ReturnsSuccess_WithValidCredentials()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword("mypassword");
            var user = new PublicUser
            {
                Email = "login@example.com",
                Name = "Login User",
                PhoneNumber = "33333333",
                IsActive = true,
                Password = hashedPassword
            };
            dbContext.PublicUser.Add(user);
            await dbContext.SaveChangesAsync();

            var controller = new AuthController(dbContext);
            var request = new LoginRequest
            {
                Email = "login@example.com",
                Password = "mypassword"
            };

            // Act
            var result = await controller.Login(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<LoginResponse>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal("Logged in", response.Message);
            Assert.Equal(user.Id, response.UserId);
        }

        [Fact]
        public async Task Login_ReturnsUnauthorized_WhenEmailNotFound()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var controller = new AuthController(dbContext);
            var request = new LoginRequest
            {
                Email = "notfound@example.com",
                Password = "password123"
            };

            // Act
            var result = await controller.Login(request);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
            var response = Assert.IsType<LoginResponse>(unauthorizedResult.Value);
            Assert.False(response.Success);
            Assert.Equal("Invalid email or password", response.Message);
        }

        [Fact]
        public async Task Login_ReturnsUnauthorized_WhenPasswordIncorrect()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword("correctpassword");
            var user = new PublicUser
            {
                Email = "wrongpass@example.com",
                Name = "Wrong Pass User",
                PhoneNumber = "44444444",
                IsActive = true,
                Password = hashedPassword
            };
            dbContext.PublicUser.Add(user);
            await dbContext.SaveChangesAsync();

            var controller = new AuthController(dbContext);
            var request = new LoginRequest
            {
                Email = "wrongpass@example.com",
                Password = "wrongpassword"
            };

            // Act
            var result = await controller.Login(request);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
            var response = Assert.IsType<LoginResponse>(unauthorizedResult.Value);
            Assert.False(response.Success);
            Assert.Equal("Invalid email or password", response.Message);
        }

        [Fact]
        public async Task Login_ReturnsUnauthorized_WhenAccountInactive()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword("password123");
            var user = new PublicUser
            {
                Email = "inactive@example.com",
                Name = "Inactive User",
                PhoneNumber = "55555555",
                IsActive = false,
                Password = hashedPassword
            };
            dbContext.PublicUser.Add(user);
            await dbContext.SaveChangesAsync();

            var controller = new AuthController(dbContext);
            var request = new LoginRequest
            {
                Email = "inactive@example.com",
                Password = "password123"
            };

            // Act
            var result = await controller.Login(request);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
            var response = Assert.IsType<LoginResponse>(unauthorizedResult.Value);
            Assert.False(response.Success);
            Assert.Equal("Account inactive", response.Message);
        }

        [Fact]
        public async Task Login_TrimsAndLowercasesEmail()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword("password123");
            var user = new PublicUser
            {
                Email = "trimtest@example.com",
                Name = "Trim User",
                PhoneNumber = "66666666",
                IsActive = true,
                Password = hashedPassword
            };
            dbContext.PublicUser.Add(user);
            await dbContext.SaveChangesAsync();

            var controller = new AuthController(dbContext);
            var request = new LoginRequest
            {
                Email = "  TRIMTEST@Example.COM  ",
                Password = "password123"
            };

            // Act
            var result = await controller.Login(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<LoginResponse>(okResult.Value);
            Assert.True(response.Success);
        }

        #endregion

        #region GetProfile Tests

        [Fact]
        public async Task GetProfile_ReturnsUserProfile_WhenUserExists()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var user = new PublicUser
            {
                Email = "profile@example.com",
                Name = "Profile User",
                PhoneNumber = "77777777",
                IsActive = true,
                Password = "hashedpassword"
            };
            dbContext.PublicUser.Add(user);
            await dbContext.SaveChangesAsync();

            var controller = new AuthController(dbContext);

            // Act
            var result = await controller.GetProfile(user.Id);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var profile = Assert.IsType<UserProfileDto>(okResult.Value);
            Assert.Equal(user.Id, profile.UserId);
            Assert.Equal("Profile User", profile.UserName);
            Assert.Equal("profile@example.com", profile.Email);
            Assert.Equal("77777777", profile.PhoneNumber);
        }

        [Fact]
        public async Task GetProfile_ReturnsNotFound_WhenUserDoesNotExist()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var controller = new AuthController(dbContext);

            // Act
            var result = await controller.GetProfile(999);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        #endregion
    }
}
