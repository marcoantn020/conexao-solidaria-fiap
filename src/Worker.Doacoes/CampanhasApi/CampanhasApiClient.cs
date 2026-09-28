using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace Worker.Doacoes.CampanhasApi;

public class CampanhasApiClient : ICampanhasApiClient
{
    private readonly HttpClient _httpClient;
    private readonly CampanhasApiOptions _options;

    public CampanhasApiClient(HttpClient httpClient, IOptions<CampanhasApiOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task AtualizarArrecadadoAsync(Guid idCampanha, Guid idDoacao, decimal valorDoacao, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, $"/internal/campanhas/{idCampanha}/arrecadado")
        {
            Content = JsonContent.Create(new { IdDoacao = idDoacao, ValorDoacao = valorDoacao })
        };
        request.Headers.Add("X-Internal-Token", _options.SharedSecret);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
