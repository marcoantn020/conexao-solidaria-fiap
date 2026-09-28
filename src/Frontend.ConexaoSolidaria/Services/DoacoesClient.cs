using System.Net.Http.Json;
using Frontend.ConexaoSolidaria.Models;

namespace Frontend.ConexaoSolidaria.Services;

public class DoacoesClient
{
    private readonly HttpClient _http;

    public DoacoesClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<HttpResponseMessage> DoarAsync(Guid idCampanha, decimal valor) =>
        await _http.PostAsJsonAsync("doacoes", new DoarRequest(idCampanha, valor));
}
