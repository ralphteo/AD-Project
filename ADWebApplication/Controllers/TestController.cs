using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ADWebApplication.Models;
using ADWebApplication.Data;

namespace ADWebApplication.Controllers;

// Test only: EF Core example for creating a user and reward wallet
[Route("test")]
public class TestUserController : Controller
{
    private readonly In5niteDbContext _db;

    public TestUserController(In5niteDbContext db)
    {
        _db = db;
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create()
    {
        const string testEmail = "test@in5nite.sg";

        // prevent duplicate user on refresh
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