using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ADWebApplication.Models;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace ADWebApplication.Controllers;

public class HomeController : Controller
{

    // test MySql connection - con string from get from appsettings.json
    private readonly IConfiguration _configuration;
    private readonly ILogger<HomeController> _logger;

    public HomeController(IConfiguration configuration, ILogger<HomeController> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }
    // test MySql connection - HomeController test MySql connection
    public async Task<IActionResult> IndexAsync()
    {
        // test MySql connection
        _logger.LogInformation("Test Azure MySQL!");

        var connectionString = _configuration.GetConnectionString("DefaultConnection");

        await using var conn = new MySqlConnection(connectionString);
        await conn.OpenAsync();

        _logger.LogInformation("Connected to Azure MySQL!");
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
