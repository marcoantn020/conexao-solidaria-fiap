namespace Worker.Doacoes.CampanhasApi;

public interface ICampanhasApiClient
{
    Task AtualizarArrecadadoAsync(Guid idCampanha, Guid idDoacao, decimal valorDoacao, CancellationToken cancellationToken);
}
