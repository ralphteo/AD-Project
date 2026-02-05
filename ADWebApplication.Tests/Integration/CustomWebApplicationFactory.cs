using System.Linq;
using ADWebApplication.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ADWebApplication.Tests.Integration
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly List<SqliteConnection> _connections = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                ReplaceDbContext<In5niteDbContext>(services);

                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                scope.ServiceProvider.GetRequiredService<In5niteDbContext>().Database.EnsureCreated();
            });
        }

        private void ReplaceDbContext<TContext>(IServiceCollection services) where TContext : DbContext
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<TContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();
            _connections.Add(connection);

            services.AddDbContext<TContext>(options =>
            {
                options.UseSqlite(connection);
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                foreach (var connection in _connections)
                {
                    connection.Dispose();
                }
            }
        }
    }
}
