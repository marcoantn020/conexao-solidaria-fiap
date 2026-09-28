using System.Net.Http.Json;
using Frontend.ConexaoSolidaria.Models;

namespace Frontend.ConexaoSolidaria.Services;

public class CampanhasClient
{
    private readonly HttpClient _http;

    public CampanhasClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<CampanhaPublicaResponse>> ListarAtivasAsync()
    {
        var campanhas = await _http.GetFromJsonAsync<List<CampanhaPublicaResponse>>("campanhas");
        return campanhas ?? [];
    }

    public async Task<HttpResponseMessage> CriarAsync(CampanhaRequest request) =>
        await _http.PostAsJsonAsync("campanhas", request);

    public async Task<HttpResponseMessage> EditarAsync(Guid id, CampanhaRequest request) =>
        await _http.PutAsJsonAsync($"campanhas/{id}", request);

    public async Task<HttpResponseMessage> AtualizarStatusAsync(Guid id, StatusCampanha status) =>
        await _http.PatchAsJsonAsync($"campanhas/{id}/status", new AtualizarStatusRequest(status));
}
