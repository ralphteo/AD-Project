using Microsoft.EntityFrameworkCore;
using ADWebApplication.Data;
using Microsoft.VisualBasic;
using ADWebApplication.Models;
using ADWebApplication.Services;
using ADWebApplication.Services.Collector;
using Microsoft.AspNetCore.Authentication.Cookies;
using ADWebApplication.Data.Repository;
using Azure.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IDashboardRepository, DashboardRepository>();
builder.Services.AddScoped<IAdminRepository, AdminRepository>();
builder.Services.AddScoped<ICollectorService, CollectorService>();
builder.Services.AddScoped<ICollectorDashboardService, CollectorDashboardService>();
builder.Services.AddScoped<ICollectorAssignmentService, CollectorAssignmentService>();
builder.Services.AddScoped<ICollectorIssueService, CollectorIssueService>();
builder.Services.AddScoped<IRouteAssignmentService, RouteAssignmentService>();
builder.Services.AddScoped<IRoutePlanningService, RoutePlanningService>();
builder.Services.AddScoped<ICampaignService, CampaignService>();
builder.Services.AddScoped<ICampaignRepository, CampaignRepository>();
builder.Services.AddScoped<IRewardCatalogueService, RewardCatalogueService>();
builder.Services.AddScoped<IRewardCatalogueRepository, RewardCatalogueRepository>();

// Azure Key Vault integration
builder.Configuration.AddEnvironmentVariables();

// 2. Add Key Vault only in cloud version
if (!builder.Environment.IsDevelopment())
{
    var keyVaultUrl = new Uri("https://in5nite-kv.vault.azure.net/");
    builder.Configuration.AddAzureKeyVault(
        keyVaultUrl,
        new DefaultAzureCredential()
    );
}

var mySqlConn = builder.Configuration.GetConnectionString("DefaultConnection");


builder.Services.AddDbContext<In5niteDbContext>(options =>
    options.UseMySql(
        mySqlConn,
        new MySqlServerVersion(new Version(8, 0, 36))
    )
);

// Session (needed for OTP)
builder.Services.AddSession(opt =>
{
    opt.IdleTimeout = TimeSpan.FromMinutes(10);
    opt.Cookie.HttpOnly = true;
    opt.Cookie.IsEssential = true;
});
// Antiforgery (needed if your JS uses header token)
builder.Services.AddAntiforgery(opt =>
{
    opt.HeaderName = "RequestVerificationToken";
});

// Cookie Auth (needed for RBAC + redirect)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opt =>
    {
        opt.LoginPath = "/EmpAuth/Login";
        opt.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        opt.SlidingExpiration = true;
    });

//ML Flask
builder.Services.AddHttpClient<IBinPredictionService, BinPredictionService>(client =>
{
    client.BaseAddress = new Uri("https://in5nite-ml-fdcycfe6gkfnhdg2.southeastasia-01.azurewebsites.net");
});


builder.Services.AddAuthorization();

// CORS policy for Android mobile app
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAndroid", policy =>
    {
        policy
            .WithOrigins("http://10.0.2.2")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();


app.MapGet("/health", async (In5niteDbContext db) =>
{
    try
    {
        var canConnect = await db.Database.CanConnectAsync();
        return Results.Ok(new { canConnect });
    }
    catch (Exception ex)
    {
        return Results.Ok(new
        {
            canConnect = false,
            error = ex.Message
        });
    }
});

app.MapGet("/emp-test", async (In5niteDbContext db) =>
{
    var count = await db.Employees.CountAsync();
    return Results.Ok(new { employeeCount = count });
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseRouting();
app.UseCors("AllowAndroid");
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=EmpAuth}/{action=login}/{id?}");


#pragma warning disable S6966 // Await RunAsync instead
app.Run();
#pragma warning restore S6966

// Expose Program for integration tests (e.g., WebApplicationFactory<Program>).
namespace ADWebApplication
{
    public partial class Program { }
}
