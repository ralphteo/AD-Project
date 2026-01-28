using Microsoft.EntityFrameworkCore;
using ADWebApplication.Data;
using Microsoft.VisualBasic;
using ADWebApplication.Models;
using ADWebApplication.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddControllersWithViews();
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddScoped<IEmailService, EmailService>();


builder.Services.AddDbContext<In5niteDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(8, 0, 36))
    )
);
builder.Services.AddDbContext<EmpDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
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
        opt.ExpireTimeSpan = TimeSpan.FromHours(8);
        opt.SlidingExpiration = true;
    });
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
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<EmpDbContext>();

    // Create tables if you already have migrations:
    db.Database.Migrate();

    // 1) Ensure Roles exist
    if (!db.Roles.Any())
    {
        db.Roles.Add(new Role { Name = "HR" });
        db.Roles.Add(new Role { Name = "Admin" });
        db.Roles.Add(new Role { Name = "Collector" });
        db.SaveChanges();
    }

    // 2) Ensure HR user exists
    var hrUser = db.EmpAccounts.FirstOrDefault(u => u.Username == "hr001");
    if (hrUser == null)
    {
        hrUser = new EmpAccount
        {
            Username = "Hr001",                     
            FullName = "Team5",
            Email = "nyetsinhtut28596@gmail.com",      
            PhoneNumber = "91234567",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Hr@12345"),
            IsActive = true
        };

        db.EmpAccounts.Add(hrUser);
        db.SaveChanges();
    }

    // 3) Assign HR role
    var hrRole = db.Roles.First(r => r.Name == "HR");

    bool hasRole = db.EmpRoles.Any(ur => ur.EmpAccountId == hrUser.Id && ur.RoleId == hrRole.Id);
    if (!hasRole)
    {
        db.EmpRoles.Add(new EmpRole
        {
            EmpAccountId = hrUser.Id,
            RoleId = hrRole.Id
        });
        db.SaveChanges();
    }
}
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

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

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