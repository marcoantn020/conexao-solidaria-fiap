using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Api.CampanhasUsuarios.Tests.Auth;

public class AuthControllerTests : IClassFixture<CampanhasApiFactory>
{
    private readonly CampanhasApiFactory _factory;

    public AuthControllerTests(CampanhasApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_ComGestorOngSeedado_Retorna200EToken()
    {
        var client = _factory.CreateClient();
        var request = new { Email = "gestor@esperancasolidaria.org", Senha = "Admin123!" };

        var response = await client.PostAsJsonAsync("/auth/login", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        Assert.False(string.IsNullOrWhiteSpace(body!.Token));
    }

    [Fact]
    public async Task Login_ComDoadorCadastrado_Retorna200EToken()
    {
        var client = _factory.CreateClient();
        await client.PostAsJsonAsync("/doadores", new
        {
            NomeCompleto = "Joao Souza",
            Email = "joao@example.com",
            Cpf = "52998224725",
            Senha = "SenhaForte123"
        });

        var response = await client.PostAsJsonAsync("/auth/login", new { Email = "joao@example.com", Senha = "SenhaForte123" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_ComSenhaErrada_Retorna401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/login", new { Email = "gestor@esperancasolidaria.org", Senha = "senha-errada" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private record LoginResponseDto(string Token);
}
