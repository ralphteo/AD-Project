using System.Linq;
using ADWebApplication.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Azure.Security.KeyVault.Secrets;
using Azure.Identity;

namespace ADWebApplication.Tests.Integration
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly List<SqliteConnection> _connections = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // --- Existing DB replacement ---
                ReplaceDbContext<In5niteDbContext>(services);

                // --- Key Vault mocking for CI ---
                var skipKeyVault = Environment.GetEnvironmentVariable("SKIP_KEYVAULT_IN_TESTS") == "true";

                if (skipKeyVault)
                {
                    // Replace SecretClient with a fake
                    services.AddSingleton<SecretClient>(provider => new FakeSecretClient());
                }
                else
                {
                    var keyVaultUrl = "https://in5nite-keyvault.vault.azure.net/";
                    services.AddSingleton(new SecretClient(new Uri(keyVaultUrl), new DefaultAzureCredential()));
                }

                // --- Ensure DB is created ---
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

    // --- Fake KeyVault client for CI ---
    public class FakeSecretClient : SecretClient
    {
        public FakeSecretClient() : base(new Uri("https://fake/"), new DefaultAzureCredential()) { }

        public override Azure.Response<KeyVaultSecret> GetSecret(string name, string version = null, System.Threading.CancellationToken cancellationToken = default)
        {
            return Azure.Response.FromValue(new KeyVaultSecret(name, "FAKE_SECRET"), null);
        }
    }
}
