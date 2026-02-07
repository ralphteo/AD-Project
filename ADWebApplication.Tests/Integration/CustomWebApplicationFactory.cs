using System;
using System.Collections.Generic;
using System.Linq;
using ADWebApplication.Data;
using Azure.Security.KeyVault.Secrets;
using Azure;
using Azure.Identity;
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
                // --- Replace DB with in-memory SQLite ---
                ReplaceDbContext<In5niteDbContext>(services);

                // --- Key Vault mocking for CI ---
                var skipKeyVault = Environment.GetEnvironmentVariable("SKIP_KEYVAULT_IN_TESTS") == "true";

                if (skipKeyVault)
                {
                    // Inject fake Key Vault client
                    services.AddSingleton<ISecretClient, FakeSecretClient>();
                }
                else
                {
                    // Use real Key Vault client
                    var keyVaultUrl = "https://in5nite-keyvault.vault.azure.net/";
                    var realClient = new SecretClient(new Uri(keyVaultUrl), new DefaultAzureCredential());
                    services.AddSingleton<ISecretClient>(new RealSecretClientWrapper(realClient));
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
                services.Remove(descriptor);

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
                    connection.Dispose();
            }
        }
    }

    // --- Step 1: Define interface ---
    public interface ISecretClient
    {
        Response<KeyVaultSecret> GetSecret(string name);
    }

    // --- Step 2: Fake for CI ---
    public class FakeSecretClient : ISecretClient
    {
        public Response<KeyVaultSecret> GetSecret(string name)
        {
            return Response.FromValue(new KeyVaultSecret(name, "FAKE_SECRET"), null);
        }
    }

    // --- Step 3: Wrapper for real SecretClient ---
    public class RealSecretClientWrapper : ISecretClient
    {
        private readonly SecretClient _client;
        public RealSecretClientWrapper(SecretClient client) => _client = client;

        public Response<KeyVaultSecret> GetSecret(string name)
        {
            return _client.GetSecret(name);
        }
    }
}
