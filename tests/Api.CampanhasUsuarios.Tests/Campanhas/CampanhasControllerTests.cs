using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Api.CampanhasUsuarios.Campanhas;
using Api.CampanhasUsuarios.Data;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Api.CampanhasUsuarios.Tests.Campanhas;

public class CampanhasControllerTests : IClassFixture<CampanhasApiFactory>
{
    private readonly CampanhasApiFactory _factory;

    public CampanhasControllerTests(CampanhasApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> CriarClienteAutenticadoComoGestorAsync()
    {
        var client = _factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/auth/login", new
        {
            Email = "gestor@esperancasolidaria.org",
            Senha = "Admin123!"
        });
        var body = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
        return client;
    }

    [Fact]
    public async Task Criar_ComoGestorEDadosValidos_Retorna201()
    {
        var client = await CriarClienteAutenticadoComoGestorAsync();
        var request = new
        {
            Titulo = "Campanha de Inverno",
            Descricao = "Arrecadação de agasalhos",
            DataInicio = DateTime.UtcNow,
            DataFim = DateTime.UtcNow.AddDays(30),
            MetaFinanceira = 5000m
        };

        var response = await client.PostAsJsonAsync("/campanhas", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Criar_ComDataFimNoPassado_Retorna400()
    {
        var client = await CriarClienteAutenticadoComoGestorAsync();
        var request = new
        {
            Titulo = "Campanha Inválida",
            Descricao = "Data no passado",
            DataInicio = DateTime.UtcNow.AddDays(-10),
            DataFim = DateTime.UtcNow.AddDays(-1),
            MetaFinanceira = 5000m
        };

        var response = await client.PostAsJsonAsync("/campanhas", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Criar_SemAutenticacao_Retorna401()
    {
        var client = _factory.CreateClient();
        var request = new
        {
            Titulo = "Campanha sem token",
            Descricao = "Deve falhar",
            DataInicio = DateTime.UtcNow,
            DataFim = DateTime.UtcNow.AddDays(30),
            MetaFinanceira = 5000m
        };

        var response = await client.PostAsJsonAsync("/campanhas", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Criar_ComoDoador_Retorna403()
    {
        var client = _factory.CreateClient();
        await client.PostAsJsonAsync("/doadores", new
        {
            NomeCompleto = "Doador Teste",
            Email = "doador.campanha@example.com",
            Cpf = "52998224725",
            Senha = "SenhaForte123"
        });
        var loginResponse = await client.PostAsJsonAsync("/auth/login", new { Email = "doador.campanha@example.com", Senha = "SenhaForte123" });
        var body = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);

        var request = new
        {
            Titulo = "Campanha via doador",
            Descricao = "Deve falhar",
            DataInicio = DateTime.UtcNow,
            DataFim = DateTime.UtcNow.AddDays(30),
            MetaFinanceira = 5000m
        };
        var response = await client.PostAsJsonAsync("/campanhas", request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Editar_ComoGestorEDadosValidos_Retorna200()
    {
        var client = await CriarClienteAutenticadoComoGestorAsync();
        var createRequest = new
        {
            Titulo = "Campanha Original",
            Descricao = "Descrição original",
            DataInicio = DateTime.UtcNow,
            DataFim = DateTime.UtcNow.AddDays(30),
            MetaFinanceira = 5000m
        };

        var createResponse = await client.PostAsJsonAsync("/campanhas", createRequest);
        var campanhaDto = await createResponse.Content.ReadFromJsonAsync<CampanhaDto>();

        var editRequest = new
        {
            Titulo = "Campanha Editada",
            Descricao = "Descrição editada",
            DataInicio = DateTime.UtcNow,
            DataFim = DateTime.UtcNow.AddDays(60),
            MetaFinanceira = 10000m
        };

        var editResponse = await client.PutAsJsonAsync($"/campanhas/{campanhaDto!.Id}", editRequest);

        Assert.Equal(HttpStatusCode.OK, editResponse.StatusCode);
    }

    [Fact]
    public async Task Editar_ComDataFimNoPassado_Retorna400()
    {
        var client = await CriarClienteAutenticadoComoGestorAsync();
        var createRequest = new
        {
            Titulo = "Campanha Original",
            Descricao = "Descrição original",
            DataInicio = DateTime.UtcNow,
            DataFim = DateTime.UtcNow.AddDays(30),
            MetaFinanceira = 5000m
        };

        var createResponse = await client.PostAsJsonAsync("/campanhas", createRequest);
        var campanhaDto = await createResponse.Content.ReadFromJsonAsync<CampanhaDto>();

        var editRequest = new
        {
            Titulo = "Campanha Editada",
            Descricao = "Descrição editada",
            DataInicio = DateTime.UtcNow.AddDays(-10),
            DataFim = DateTime.UtcNow.AddDays(-1),
            MetaFinanceira = 5000m
        };

        var editResponse = await client.PutAsJsonAsync($"/campanhas/{campanhaDto!.Id}", editRequest);

        Assert.Equal(HttpStatusCode.BadRequest, editResponse.StatusCode);
    }

    [Fact]
    public async Task Editar_ComCampanhaInexistente_Retorna404()
    {
        var client = await CriarClienteAutenticadoComoGestorAsync();
        var inexistenteId = Guid.NewGuid();
        var editRequest = new
        {
            Titulo = "Campanha",
            Descricao = "Descrição",
            DataInicio = DateTime.UtcNow,
            DataFim = DateTime.UtcNow.AddDays(30),
            MetaFinanceira = 5000m
        };

        var editResponse = await client.PutAsJsonAsync($"/campanhas/{inexistenteId}", editRequest);

        Assert.Equal(HttpStatusCode.NotFound, editResponse.StatusCode);
    }

    [Fact]
    public async Task Editar_SemAutenticacao_Retorna401()
    {
        var client = _factory.CreateClient();
        var campaignaId = Guid.NewGuid();
        var editRequest = new
        {
            Titulo = "Campanha",
            Descricao = "Descrição",
            DataInicio = DateTime.UtcNow,
            DataFim = DateTime.UtcNow.AddDays(30),
            MetaFinanceira = 5000m
        };

        var editResponse = await client.PutAsJsonAsync($"/campanhas/{campaignaId}", editRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, editResponse.StatusCode);
    }

    [Fact]
    public async Task Editar_ComoDoador_Retorna403()
    {
        // Create a campaign as gestor
        var gestorClient = await CriarClienteAutenticadoComoGestorAsync();
        var createRequest = new
        {
            Titulo = "Campanha Original",
            Descricao = "Descrição original",
            DataInicio = DateTime.UtcNow,
            DataFim = DateTime.UtcNow.AddDays(30),
            MetaFinanceira = 5000m
        };
        var createResponse = await gestorClient.PostAsJsonAsync("/campanhas", createRequest);
        var campanhaDto = await createResponse.Content.ReadFromJsonAsync<CampanhaDto>();

        // Register and authenticate as doador in a new client
        var doadorClient = _factory.CreateClient();
        await doadorClient.PostAsJsonAsync("/doadores", new
        {
            NomeCompleto = "Doador Teste Editar",
            Email = "doador.editar.test@example.com",
            Cpf = "52998224725",
            Senha = "SenhaForte123"
        });
        var loginResponse = await doadorClient.PostAsJsonAsync("/auth/login", new { Email = "doador.editar.test@example.com", Senha = "SenhaForte123" });
        var body = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();
        doadorClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);

        var editRequest = new
        {
            Titulo = "Campanha Editada",
            Descricao = "Descrição editada",
            DataInicio = DateTime.UtcNow,
            DataFim = DateTime.UtcNow.AddDays(60),
            MetaFinanceira = 10000m
        };
        var response = await doadorClient.PutAsJsonAsync($"/campanhas/{campanhaDto!.Id}", editRequest);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ListarPublico_RetornaApenasCampanhasAtivas()
    {
        // Create an active campaign via API
        var clienteGestor = await CriarClienteAutenticadoComoGestorAsync();
        await clienteGestor.PostAsJsonAsync("/campanhas", new
        {
            Titulo = "Campanha Ativa Pública",
            Descricao = "Deve aparecer no painel",
            DataInicio = DateTime.UtcNow,
            DataFim = DateTime.UtcNow.AddDays(10),
            MetaFinanceira = 1000m
        });

        // Directly insert a cancelled campaign into the DB (bypassing API)
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CampanhasDbContext>();
            var campanhasCancelada = new Campanha
            {
                Id = Guid.NewGuid(),
                Titulo = "Campanha Cancelada",
                Descricao = "Não deve aparecer",
                DataInicio = DateTime.UtcNow,
                DataFim = DateTime.UtcNow.AddDays(5),
                MetaFinanceira = 500m,
                ValorArrecadado = 0m,
                Status = StatusCampanha.Cancelada
            };
            db.Campanhas.Add(campanhasCancelada);
            await db.SaveChangesAsync();
        }

        // Call the public endpoint
        var clientePublico = _factory.CreateClient();
        var response = await clientePublico.GetAsync("/campanhas");

        // Assert only active campaigns are returned
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var campanhas = await response.Content.ReadFromJsonAsync<List<CampanhaPublicaDto>>();
        Assert.NotNull(campanhas);
        Assert.Single(campanhas, c => c.Titulo == "Campanha Ativa Pública");
        Assert.DoesNotContain(campanhas, c => c.Titulo == "Campanha Cancelada");
    }

    [Fact]
    public async Task ListarPublico_NaoRetornaCampanhaComDataFimExpirada()
    {
        // Create an active campaign via API
        var clienteGestor = await CriarClienteAutenticadoComoGestorAsync();
        await clienteGestor.PostAsJsonAsync("/campanhas", new
        {
            Titulo = "Campanha Ativa Dentro do Prazo",
            Descricao = "Deve aparecer no painel",
            DataInicio = DateTime.UtcNow,
            DataFim = DateTime.UtcNow.AddDays(10),
            MetaFinanceira = 1000m
        });

        // Directly insert a campaign with Status Ativa but DataFim in the past
        // (the public POST /campanhas endpoint rejects past dates by design, so this
        // scenario must be seeded straight into the DbContext)
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CampanhasDbContext>();
            var campanhaExpirada = new Campanha
            {
                Id = Guid.NewGuid(),
                Titulo = "Campanha com Prazo Encerrado",
                Descricao = "Não deve aparecer",
                DataInicio = DateTime.UtcNow.AddDays(-30),
                DataFim = DateTime.UtcNow.AddDays(-1),
                MetaFinanceira = 500m,
                ValorArrecadado = 0m,
                Status = StatusCampanha.Ativa
            };
            db.Campanhas.Add(campanhaExpirada);
            await db.SaveChangesAsync();
        }

        var clientePublico = _factory.CreateClient();
        var response = await clientePublico.GetAsync("/campanhas");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var campanhas = await response.Content.ReadFromJsonAsync<List<CampanhaPublicaDto>>();
        Assert.NotNull(campanhas);
        Assert.Contains(campanhas, c => c.Titulo == "Campanha Ativa Dentro do Prazo");
        Assert.DoesNotContain(campanhas, c => c.Titulo == "Campanha com Prazo Encerrado");
    }

    private record LoginResponseDto(string Token);
    private record CampanhaDto(Guid Id);
    private record CampanhaPublicaDto(Guid Id, string Titulo, decimal MetaFinanceira, decimal ValorArrecadado);
}
