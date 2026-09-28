using Worker.Doacoes.CampanhasApi;

namespace Worker.Doacoes.Tests;

public class FakeCampanhasApiClient : ICampanhasApiClient
{
    public List<(Guid IdCampanha, Guid IdDoacao, decimal ValorDoacao)> Chamadas { get; } = new();
    public bool DeveLancarExcecao { get; set; }

    public Task AtualizarArrecadadoAsync(Guid idCampanha, Guid idDoacao, decimal valorDoacao, CancellationToken cancellationToken)
    {
        if (DeveLancarExcecao)
            throw new HttpRequestException("Falha simulada");

        Chamadas.Add((idCampanha, idDoacao, valorDoacao));
        return Task.CompletedTask;
    }
}
