using Microsoft.EntityFrameworkCore;
using ADWebApplication.Data;
using Microsoft.VisualBasic;
using ADWebApplication.Models;
using ADWebApplication.Services;
using ADWebApplication.Services.Collector;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using ADWebApplication.Data.Repository;
using Azure.Identity;
using System.Threading.RateLimiting;

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
builder.Services.AddScoped<JwtTokenService>();

// Azure Key Vault integration
builder.Configuration.AddEnvironmentVariables();

// Check if we should skip Key Vault (for CI/CD and testing)
var skipKeyVault = Environment.GetEnvironmentVariable("SKIP_KEYVAULT_IN_TESTS");
var shouldSkipKeyVault = !string.IsNullOrEmpty(skipKeyVault) && 
                         skipKeyVault.Equals("true", StringComparison.OrdinalIgnoreCase);

// Add Key Vault only in non-development environments and when not explicitly skipped
if (!builder.Environment.IsDevelopment() && !shouldSkipKeyVault)
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
    })
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, opt =>
    {
        var key = builder.Configuration["Jwt:Key"] ?? string.Empty;
        var keyBytes = Encoding.UTF8.GetBytes(key);

        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
            ClockSkew = TimeSpan.FromMinutes(2)
        };
    });

//ML Flask
builder.Services.AddHttpClient<IBinPredictionService, BinPredictionService>(client =>
{
    client.BaseAddress = new Uri("https://in5nite-ml-fdcycfe6gkfnhdg2.southeastasia-01.azurewebsites.net");
});


builder.Services.AddAuthorization();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("mobile", context =>
    {
        var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var partitionKey = !string.IsNullOrWhiteSpace(userId)
            ? $"user:{userId}"
            : $"ip:{context.Connection.RemoteIpAddress}";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: partitionKey,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
    });
});

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
app.UseRateLimiter();
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
// namespace ADWebApplication
// {
//     public partial class Program { }
// }
