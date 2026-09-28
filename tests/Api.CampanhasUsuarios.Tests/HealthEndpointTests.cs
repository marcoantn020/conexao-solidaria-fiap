using System.Net;
using Xunit;

namespace Api.CampanhasUsuarios.Tests;

public class HealthEndpointTests : IClassFixture<CampanhasApiFactory>
{
    private readonly CampanhasApiFactory _factory;

    public HealthEndpointTests(CampanhasApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Health_ReturnsOk()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
