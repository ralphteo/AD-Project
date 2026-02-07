using ADWebApplication.Services;

namespace ADWebApplication.Tests.Integration
{
    public class FakeSecretProvider : ISecretProvider
    {
        public string GetSecret(string name) => "FAKE_SECRET";
    }
}
