using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace ADWebApplication.Services
{
    public class KeyVaultSecretProvider : ISecretProvider
    {
        private readonly SecretClient _client;

        public KeyVaultSecretProvider(string keyVaultUrl)
        {
            _client = new SecretClient(new Uri(keyVaultUrl), new DefaultAzureCredential());
        }

        public string GetSecret(string name)
        {
            return _client.GetSecret(name).Value.Value;
        }
    }
}
