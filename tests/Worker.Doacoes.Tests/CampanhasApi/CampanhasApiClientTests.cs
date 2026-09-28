using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Worker.Doacoes.CampanhasApi;
using Xunit;

namespace Worker.Doacoes.Tests.CampanhasApi;

public class CampanhasApiClientTests
{
    private class StubHttpMessageHandler : HttpMessageHandler
    {
        public HttpRequestMessage? RequisicaoCapturada { get; private set; }
        public string? CorpoCapturado { get; private set; }
        public HttpStatusCode RespostaStatus { get; set; } = HttpStatusCode.OK;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequisicaoCapturada = request;
            CorpoCapturado = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(RespostaStatus);
        }
    }

    private record CorpoRequisicao(Guid IdDoacao, decimal ValorDoacao);

    private static CampanhasApiClient CriarCliente(StubHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://api-campanhas-usuarios.local") };
        var options = Options.Create(new CampanhasApiOptions { BaseUrl = "http://api-campanhas-usuarios.local", SharedSecret = "segredo-de-teste" });
        return new CampanhasApiClient(httpClient, options);
    }

    [Fact]
    public async Task AtualizarArrecadadoAsync_EnviaPatchComHeaderECorpoCorretos()
    {
        var handler = new StubHttpMessageHandler();
        var cliente = CriarCliente(handler);
        var idCampanha = Guid.NewGuid();
        var idDoacao = Guid.NewGuid();

        await cliente.AtualizarArrecadadoAsync(idCampanha, idDoacao, 150m, CancellationToken.None);

        Assert.Equal(HttpMethod.Patch, handler.RequisicaoCapturada!.Method);
        Assert.Equal($"/internal/campanhas/{idCampanha}/arrecadado", handler.RequisicaoCapturada.RequestUri!.AbsolutePath);
        Assert.Equal("segredo-de-teste", handler.RequisicaoCapturada.Headers.GetValues("X-Internal-Token").Single());

        var corpo = JsonSerializer.Deserialize<CorpoRequisicao>(handler.CorpoCapturado!, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.Equal(idDoacao, corpo!.IdDoacao);
        Assert.Equal(150m, corpo.ValorDoacao);
    }

    [Fact]
    public async Task AtualizarArrecadadoAsync_ComRespostaDeErro_LancaExcecao()
    {
        var handler = new StubHttpMessageHandler { RespostaStatus = HttpStatusCode.NotFound };
        var cliente = CriarCliente(handler);

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            cliente.AtualizarArrecadadoAsync(Guid.NewGuid(), Guid.NewGuid(), 50m, CancellationToken.None));
    }
}
