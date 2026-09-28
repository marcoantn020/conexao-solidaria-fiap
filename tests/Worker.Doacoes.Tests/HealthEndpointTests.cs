using System.Net;
using Xunit;

namespace Worker.Doacoes.Tests;

public class HealthEndpointTests : IClassFixture<DoacoesFactory>
{
    private readonly DoacoesFactory _factory;

    public HealthEndpointTests(DoacoesFactory factory)
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
