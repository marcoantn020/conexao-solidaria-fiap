using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace Api.CampanhasUsuarios.Tests.Doacoes;

public class InternalCampanhasControllerTests : IClassFixture<CampanhasApiFactory>
{
    private readonly CampanhasApiFactory _factory;

    public InternalCampanhasControllerTests(CampanhasApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AtualizarArrecadado_ComCampanhaExistente_IncrementaValorEApareceNoPainel()
    {
        var clienteGestor = _factory.CreateClient();
        var loginGestor = await clienteGestor.PostAsJsonAsync("/auth/login", new { Email = "gestor@esperancasolidaria.org", Senha = "Admin123!" });
        var tokenGestor = (await loginGestor.Content.ReadFromJsonAsync<LoginResponseDto>())!.Token;
        clienteGestor.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenGestor);

        var criarCampanha = await clienteGestor.PostAsJsonAsync("/campanhas", new
        {
            Titulo = "Campanha para Callback Interno",
            Descricao = "Teste",
            DataInicio = DateTime.UtcNow,
            DataFim = DateTime.UtcNow.AddDays(10),
            MetaFinanceira = 1000m
        });
        var campanhaCriada = await criarCampanha.Content.ReadFromJsonAsync<CampanhaDto>();

        var clienteInterno = _factory.CreateClient();
        clienteInterno.DefaultRequestHeaders.Add("X-Internal-Token", CampanhasApiFactory.InternalSharedSecret);
        var response = await clienteInterno.PatchAsJsonAsync($"/internal/campanhas/{campanhaCriada!.Id}/arrecadado", new { IdDoacao = Guid.NewGuid(), ValorDoacao = 100m });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var painel = await _factory.CreateClient().GetAsync("/campanhas");
        var campanhas = await painel.Content.ReadFromJsonAsync<List<CampanhaPublicaDto>>();
        Assert.Contains(campanhas!, c => c.Id == campanhaCriada.Id && c.ValorArrecadado == 100m);
    }

    [Fact]
    public async Task AtualizarArrecadado_ComIdDoacaoRepetido_NaoIncrementaDuasVezes()
    {
        var clienteGestor = _factory.CreateClient();
        var loginGestor = await clienteGestor.PostAsJsonAsync("/auth/login", new { Email = "gestor@esperancasolidaria.org", Senha = "Admin123!" });
        var tokenGestor = (await loginGestor.Content.ReadFromJsonAsync<LoginResponseDto>())!.Token;
        clienteGestor.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenGestor);

        var criarCampanha = await clienteGestor.PostAsJsonAsync("/campanhas", new
        {
            Titulo = "Campanha para Idempotencia do Callback Interno",
            Descricao = "Teste",
            DataInicio = DateTime.UtcNow,
            DataFim = DateTime.UtcNow.AddDays(10),
            MetaFinanceira = 1000m
        });
        var campanhaCriada = await criarCampanha.Content.ReadFromJsonAsync<CampanhaDto>();

        var idDoacao = Guid.NewGuid();
        var clienteInterno = _factory.CreateClient();
        clienteInterno.DefaultRequestHeaders.Add("X-Internal-Token", CampanhasApiFactory.InternalSharedSecret);

        var primeiraChamada = await clienteInterno.PatchAsJsonAsync($"/internal/campanhas/{campanhaCriada!.Id}/arrecadado", new { IdDoacao = idDoacao, ValorDoacao = 100m });
        var segundaChamada = await clienteInterno.PatchAsJsonAsync($"/internal/campanhas/{campanhaCriada.Id}/arrecadado", new { IdDoacao = idDoacao, ValorDoacao = 100m });

        Assert.Equal(HttpStatusCode.OK, primeiraChamada.StatusCode);
        Assert.Equal(HttpStatusCode.OK, segundaChamada.StatusCode);

        var painel = await _factory.CreateClient().GetAsync("/campanhas");
        var campanhas = await painel.Content.ReadFromJsonAsync<List<CampanhaPublicaDto>>();
        Assert.Contains(campanhas!, c => c.Id == campanhaCriada.Id && c.ValorArrecadado == 100m);
    }

    [Fact]
    public async Task AtualizarArrecadado_ComCampanhaInexistente_Retorna404()
    {
        var cliente = _factory.CreateClient();
        cliente.DefaultRequestHeaders.Add("X-Internal-Token", CampanhasApiFactory.InternalSharedSecret);

        var response = await cliente.PatchAsJsonAsync($"/internal/campanhas/{Guid.NewGuid()}/arrecadado", new { IdDoacao = Guid.NewGuid(), ValorDoacao = 100m });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AtualizarArrecadado_ComValorNaoPositivo_Retorna400()
    {
        var cliente = _factory.CreateClient();
        cliente.DefaultRequestHeaders.Add("X-Internal-Token", CampanhasApiFactory.InternalSharedSecret);

        var response = await cliente.PatchAsJsonAsync($"/internal/campanhas/{Guid.NewGuid()}/arrecadado", new { IdDoacao = Guid.NewGuid(), ValorDoacao = -10m });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AtualizarArrecadado_SemTokenInternoOuComTokenInvalido_Retorna401()
    {
        var cliente = _factory.CreateClient();

        var respostaSemHeader = await cliente.PatchAsJsonAsync($"/internal/campanhas/{Guid.NewGuid()}/arrecadado", new { IdDoacao = Guid.NewGuid(), ValorDoacao = 100m });
        Assert.Equal(HttpStatusCode.Unauthorized, respostaSemHeader.StatusCode);

        cliente.DefaultRequestHeaders.Add("X-Internal-Token", "token-incorreto");
        var respostaTokenInvalido = await cliente.PatchAsJsonAsync($"/internal/campanhas/{Guid.NewGuid()}/arrecadado", new { IdDoacao = Guid.NewGuid(), ValorDoacao = 100m });
        Assert.Equal(HttpStatusCode.Unauthorized, respostaTokenInvalido.StatusCode);
    }

    private record LoginResponseDto(string Token);
    private record CampanhaDto(Guid Id);
    private record CampanhaPublicaDto(Guid Id, string Titulo, decimal MetaFinanceira, decimal ValorArrecadado);
}
