using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Api.CampanhasUsuarios.Tests.Usuarios;

public class DoadoresControllerTests : IClassFixture<CampanhasApiFactory>
{
    private readonly CampanhasApiFactory _factory;

    public DoadoresControllerTests(CampanhasApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Cadastrar_ComDadosValidos_Retorna201()
    {
        var client = _factory.CreateClient();
        var request = new
        {
            NomeCompleto = "Maria Silva",
            Email = "maria@example.com",
            Cpf = "52998224725",
            Senha = "SenhaForte123"
        };

        var response = await client.PostAsJsonAsync("/doadores", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Cadastrar_ComCpfInvalido_Retorna400()
    {
        var client = _factory.CreateClient();
        var request = new
        {
            NomeCompleto = "Maria Silva",
            Email = "maria2@example.com",
            Cpf = "11111111111",
            Senha = "SenhaForte123"
        };

        var response = await client.PostAsJsonAsync("/doadores", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Cadastrar_ComEmailDuplicado_Retorna409()
    {
        var client = _factory.CreateClient();
        var request = new
        {
            NomeCompleto = "Maria Silva",
            Email = "duplicado@example.com",
            Cpf = "52998224725",
            Senha = "SenhaForte123"
        };

        await client.PostAsJsonAsync("/doadores", request);
        var response = await client.PostAsJsonAsync("/doadores", request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
