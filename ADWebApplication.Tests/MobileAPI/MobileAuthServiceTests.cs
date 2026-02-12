using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ADWebApplication.Data;
using ADWebApplication.Models;
using ADWebApplication.Models.DTOs;
using ADWebApplication.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace ADWebApplication.Tests.Services.Mobile
{
    public class MobileAuthServiceTests
    {
        private static In5niteDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<In5niteDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new In5niteDbContext(options);
        }

        private static JwtTokenService CreateJwtTokenService()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Key"] = "test-secret-key-1234567890-1234567890-123456",
                    ["Jwt:Issuer"] = "ADWebApplication",
                    ["Jwt:Audience"] = "ADWebApplicationMobile",
                    ["Jwt:ExpiryMinutes"] = "60"
                })
                .Build();
            return new JwtTokenService(config);
        }

        [Fact]
        public async Task RegisterAsync_CreatesUserAndReturnsToken()
        {
            var db = CreateInMemoryDbContext();
            var service = new MobileAuthService(db, CreateJwtTokenService());

            var result = await service.RegisterAsync(new RegisterRequest
            {
                Email = "test@example.com",
                FullName = "Test User",
                Phone = "12345678",
                RegionId = 1,
                Password = "password123"
            });

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.False(string.IsNullOrWhiteSpace(result.Data!.Token));

            var user = await db.PublicUser.FirstOrDefaultAsync(u => u.Email == "test@example.com");
            Assert.NotNull(user);
        }

        [Fact]
        public async Task RegisterAsync_ReturnsConflict_WhenEmailExists()
        {
            var db = CreateInMemoryDbContext();
            db.PublicUser.Add(new PublicUser
            {
                Email = "exists@example.com",
                Name = "Existing",
                PhoneNumber = "11111111",
                IsActive = true,
                Password = "hash"
            });
            await db.SaveChangesAsync();

            var service = new MobileAuthService(db, CreateJwtTokenService());

            var result = await service.RegisterAsync(new RegisterRequest
            {
                Email = "exists@example.com",
                FullName = "New",
                Phone = "22222222",
                RegionId = 1,
                Password = "password123"
            });

            Assert.False(result.Success);
            Assert.Equal(MobileAuthError.EmailAlreadyRegistered, result.Error);
        }

        [Fact]
        public async Task LoginAsync_ReturnsInvalidCredentials_WhenEmailNotFound()
        {
            var db = CreateInMemoryDbContext();
            var service = new MobileAuthService(db, CreateJwtTokenService());

            var result = await service.LoginAsync(new LoginRequest
            {
                Email = "missing@example.com",
                Password = "password123"
            });

            Assert.False(result.Success);
            Assert.Equal(MobileAuthError.InvalidCredentials, result.Error);
        }

        [Fact]
        public async Task LoginAsync_ReturnsAccountInactive_WhenUserInactive()
        {
            var db = CreateInMemoryDbContext();
            db.PublicUser.Add(new PublicUser
            {
                Email = "inactive@example.com",
                Name = "Inactive",
                PhoneNumber = "33333333",
                IsActive = false,
                Password = BCrypt.Net.BCrypt.HashPassword("password123")
            });
            await db.SaveChangesAsync();

            var service = new MobileAuthService(db, CreateJwtTokenService());

            var result = await service.LoginAsync(new LoginRequest
            {
                Email = "inactive@example.com",
                Password = "password123"
            });

            Assert.False(result.Success);
            Assert.Equal(MobileAuthError.AccountInactive, result.Error);
        }

        [Fact]
        public async Task GetProfileAsync_ReturnsForbidden_WhenTokenUserDiffers()
        {
            var db = CreateInMemoryDbContext();
            var service = new MobileAuthService(db, CreateJwtTokenService());

            var result = await service.GetProfileAsync(tokenUserId: 1, targetUserId: 2);

            Assert.False(result.Success);
            Assert.Equal(MobileAuthError.Forbidden, result.Error);
        }

        [Fact]
        public async Task UpdateProfileAsync_ReturnsInvalidRegion_WhenRegionMissing()
        {
            var db = CreateInMemoryDbContext();
            var user = new PublicUser
            {
                Email = "profile@example.com",
                Name = "Profile",
                PhoneNumber = "44444444",
                IsActive = true,
                Password = "hash"
            };
            db.PublicUser.Add(user);
            await db.SaveChangesAsync();

            var service = new MobileAuthService(db, CreateJwtTokenService());

            var result = await service.UpdateProfileAsync(user.Id, new UpdateProfileRequestDto
            {
                PhoneNumber = "99999999",
                RegionId = 999
            });

            Assert.False(result.Success);
            Assert.Equal(MobileAuthError.InvalidRegion, result.Error);
        }
    }
}
