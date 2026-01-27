var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

//Repository
builder.Services.AddScoped<ADWebApplication.Data.Repository.IDashboardRepository, ADWebApplication.Data.Repository.DashboardRepository>();

//SQL Connection
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
    


app.UseStaticFiles();
app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();

app.MapStaticAssets();
app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

#pragma warning disable S6966 // Await RunAsync instead
app.Run();
#pragma warning restore S6966