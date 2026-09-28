using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Api.CampanhasUsuarios.Campanhas;
using Api.CampanhasUsuarios.Data;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Api.CampanhasUsuarios.Tests.Doacoes;

public class DoacoesControllerTests : IClassFixture<CampanhasApiFactory>
{
    private readonly CampanhasApiFactory _factory;

    public DoacoesControllerTests(CampanhasApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<(HttpClient ClienteGestor, HttpClient ClienteDoador, Guid CampanhaId)> PrepararCenarioAsync()
    {
        var clienteGestor = _factory.CreateClient();
        var loginGestor = await clienteGestor.PostAsJsonAsync("/auth/login", new { Email = "gestor@esperancasolidaria.org", Senha = "Admin123!" });
        var tokenGestor = (await loginGestor.Content.ReadFromJsonAsync<LoginResponseDto>())!.Token;
        clienteGestor.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenGestor);

        var criarCampanha = await clienteGestor.PostAsJsonAsync("/campanhas", new
        {
            Titulo = "Campanha para Doação",
            Descricao = "Teste",
            DataInicio = DateTime.UtcNow,
            DataFim = DateTime.UtcNow.AddDays(10),
            MetaFinanceira = 1000m
        });
        var campanhaCriada = await criarCampanha.Content.ReadFromJsonAsync<CampanhaDto>();

        var clienteDoador = _factory.CreateClient();
        var emailDoador = $"doador-{Guid.NewGuid()}@example.com";
        await clienteDoador.PostAsJsonAsync("/doadores", new
        {
            NomeCompleto = "Doador de Teste",
            Email = emailDoador,
            Cpf = "52998224725",
            Senha = "SenhaForte123"
        });
        var loginDoador = await clienteDoador.PostAsJsonAsync("/auth/login", new { Email = emailDoador, Senha = "SenhaForte123" });
        var tokenDoador = (await loginDoador.Content.ReadFromJsonAsync<LoginResponseDto>())!.Token;
        clienteDoador.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenDoador);

        return (clienteGestor, clienteDoador, campanhaCriada!.Id);
    }

    [Fact]
    public async Task Doar_ParaCampanhaAtiva_Retorna202EPublicaEvento()
    {
        var (_, clienteDoador, campanhaId) = await PrepararCenarioAsync();

        var response = await clienteDoador.PostAsJsonAsync("/doacoes", new { IdCampanha = campanhaId, ValorDoacao = 50m });

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Contains(_factory.EventPublisher.EventosPublicados, e => e.IdCampanha == campanhaId && e.ValorDoacao == 50m);
    }

    [Fact]
    public async Task Doar_ParaCampanhaCancelada_Retorna400()
    {
        var (clienteGestor, clienteDoador, campanhaId) = await PrepararCenarioAsync();
        await clienteGestor.PatchAsJsonAsync($"/campanhas/{campanhaId}/status", new { Status = "Cancelada" });

        var response = await clienteDoador.PostAsJsonAsync("/doacoes", new { IdCampanha = campanhaId, ValorDoacao = 50m });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Doar_ParaCampanhaComDataFimExpirada_Retorna400()
    {
        var clienteDoador = _factory.CreateClient();
        var emailDoador = $"doador-{Guid.NewGuid()}@example.com";
        await clienteDoador.PostAsJsonAsync("/doadores", new
        {
            NomeCompleto = "Doador de Teste Expirada",
            Email = emailDoador,
            Cpf = "52998224725",
            Senha = "SenhaForte123"
        });
        var loginDoador = await clienteDoador.PostAsJsonAsync("/auth/login", new { Email = emailDoador, Senha = "SenhaForte123" });
        var tokenDoador = (await loginDoador.Content.ReadFromJsonAsync<LoginResponseDto>())!.Token;
        clienteDoador.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenDoador);

        Guid campanhaId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CampanhasDbContext>();
            var campanhaExpirada = new Campanha
            {
                Id = Guid.NewGuid(),
                Titulo = "Campanha com prazo encerrado",
                Descricao = "DataFim no passado, mas Status ainda Ativa",
                DataInicio = DateTime.UtcNow.AddDays(-30),
                DataFim = DateTime.UtcNow.AddDays(-1),
                MetaFinanceira = 1000m,
                ValorArrecadado = 0m,
                Status = StatusCampanha.Ativa
            };
            db.Campanhas.Add(campanhaExpirada);
            await db.SaveChangesAsync();
            campanhaId = campanhaExpirada.Id;
        }

        var response = await clienteDoador.PostAsJsonAsync("/doacoes", new { IdCampanha = campanhaId, ValorDoacao = 50m });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Doar_SemAutenticacao_Retorna401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/doacoes", new { IdCampanha = Guid.NewGuid(), ValorDoacao = 50m });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private record LoginResponseDto(string Token);
    private record CampanhaDto(Guid Id);
}
