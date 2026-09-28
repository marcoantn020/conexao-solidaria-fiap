using Microsoft.EntityFrameworkCore;
using Worker.Doacoes.CampanhasApi;
using Worker.Doacoes.Data;
using Worker.Doacoes.Doacoes;

namespace Worker.Doacoes.Consumo;

public class DoacaoProcessor
{
    private readonly DoacoesDbContext _db;
    private readonly ICampanhasApiClient _apiClient;

    public DoacaoProcessor(DoacoesDbContext db, ICampanhasApiClient apiClient)
    {
        _db = db;
        _apiClient = apiClient;
    }

    public async Task ProcessarAsync(DoacaoRecebidaEvent evento, CancellationToken cancellationToken = default)
    {
        var jaProcessado = await _db.Doacoes.AnyAsync(d => d.IdDoacao == evento.IdDoacao, cancellationToken);
        if (jaProcessado)
            return;

        await _apiClient.AtualizarArrecadadoAsync(evento.IdCampanha, evento.IdDoacao, evento.ValorDoacao, cancellationToken);

        _db.Doacoes.Add(new Doacao
        {
            IdDoacao = evento.IdDoacao,
            IdCampanha = evento.IdCampanha,
            ValorDoacao = evento.ValorDoacao,
            DataHora = DateTime.SpecifyKind(evento.DataHora, DateTimeKind.Utc),
            ProcessadoEm = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);
    }
}
