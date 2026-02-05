using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace ADWebApplication.Tests.Integration
{
    public class HealthEndpointTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public HealthEndpointTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Get_Health_ReturnsOk()
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync("/health");

            if (response.StatusCode != HttpStatusCode.OK)
            {
                var body = await response.Content.ReadAsStringAsync();
                throw new Xunit.Sdk.XunitException($"Unexpected status {(int)response.StatusCode}. Body: {body}");
            }

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
