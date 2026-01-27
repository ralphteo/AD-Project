using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using ADWebApplication.Models;
using ADWebApplication.Data;

namespace ADWebApplication.Controllers;

[Route("test")]
public class TestController : Controller
{
    // test MySql connection - con string from get from appsettings.json
    private readonly IConfiguration _configuration;
    private readonly ILogger<TestController> _logger;
    private readonly In5niteDbContext _db;

    public TestController(
        IConfiguration configuration,
        ILogger<TestController> logger,
        In5niteDbContext db)
    {
        _configuration = configuration;
        _logger = logger;
        _db = db;
    }

    // Raw MySQL connectivity test
    [HttpGet("mysql")]
    public async Task<IActionResult> TestMySql()
    {
        _logger.LogInformation("Testing Azure MySQL connection");

        var connectionString = _configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrEmpty(connectionString))
        {
            return StatusCode(500, "Connection string not found");
        }

        await using var conn = new MySqlConnection(connectionString);
        await conn.OpenAsync();

        _logger.LogInformation("Connected to Azure MySQL");
        return Ok("MySQL connection successful");
    }

    // Test only: EF Core example for creating a user and reward wallet
    [HttpGet("create")]
    public async Task<IActionResult> CreateTestUser()
    {
        const string testEmail = "test@in5nite.sg";

        var existingUser = await _db.PublicUser
            .Include(u => u.RewardWallet)
            .FirstOrDefaultAsync(u => u.Email == testEmail);

        if (existingUser != null)
        {
            return Ok(new
            {
                message = "Test user already exists",
                userId = existingUser.Id,
                points = existingUser.RewardWallet.Points
            });
        }

        var user = new PublicUser
        {
            Email = testEmail,
            Name = "Test User",
            Password = "Test",
            RewardWallet = new RewardWallet
            {
                Points = 0
            }
        };

        _db.PublicUser.Add(user);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = "Test user created successfully",
            userId = user.Id,
            points = user.RewardWallet.Points
        });
    }
}