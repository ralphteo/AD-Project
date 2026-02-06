using Microsoft.EntityFrameworkCore;
using ADWebApplication.Data;
using Microsoft.VisualBasic;
using ADWebApplication.Models;
using ADWebApplication.Services;
using ADWebApplication.Services.Collector;
using Microsoft.AspNetCore.Authentication.Cookies;
using ADWebApplication.Data.Repository;

var builder = WebApplication.CreateBuilder(args);
var googleKey = builder.Configuration["GOOGLE_MAPS_API_KEY"];

builder.Services.AddControllers();
builder.Services.AddControllersWithViews();
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IDashboardRepository, DashboardRepository>();
builder.Services.AddScoped<RoutePlanningService>();
builder.Services.AddScoped<ICollectorService, CollectorService>();
builder.Services.AddScoped<ICollectorDashboardService, CollectorDashboardService>();
builder.Services.AddScoped<ICollectorAssignmentService, CollectorAssignmentService>();
builder.Services.AddScoped<ICollectorIssueService, CollectorIssueService>();
builder.Services.AddScoped<IRouteAssignmentService, RouteAssignmentService>();



builder.Services.AddDbContext<In5niteDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(8, 0, 36))
    )
);



// Admin Repisitory - Andrew
builder.Services.AddScoped<IAdminRepository, AdminRepository>();

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


//Campaign Service
builder.Services.AddScoped<ICampaignService, CampaignService>();
builder.Services.AddScoped<ICampaignRepository, CampaignRepository>();

//Reward Catalogue Service
builder.Services.AddScoped<IRewardCatalogueService, RewardCatalogueService>();
builder.Services.AddScoped<IRewardCatalogueRepository, RewardCatalogueRepository>();

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAndroid", policy =>
    {
        policy
            .AllowAnyOrigin()
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
    app.UseHttpsRedirection();
}
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<In5niteDbContext>();

    if (!db.Roles.Any(r => r.Name == "HR"))
    {
        db.Roles.Add(new Role { Name = "HR" });
        db.SaveChanges();
    }

    var hrRoleId = db.Roles.First(r => r.Name == "HR").RoleId;

    var hr = db.Employees.FirstOrDefault(e => e.Username == "HR-001");
    if (hr == null)
    {
        hr = new Employee
        {
            Username = "HR-001",
            FullName = "Team5 HR",
            Email = "nyetsinhtut28596@gmail.com",
            IsActive = true,
            RoleId = hrRoleId,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Hr@12345")
        };
        db.Employees.Add(hr);
    }
    else
    {
        hr.IsActive = true;
        hr.RoleId = hrRoleId;
        hr.PasswordHash = BCrypt.Net.BCrypt.HashPassword("Hr@12345");
    }

    db.SaveChanges();
}
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

namespace ADWebApplication
{
    public partial class Program { }
}
