using Microsoft.EntityFrameworkCore;
using Worker.Doacoes.Consumo;
using Worker.Doacoes.Data;
using Worker.Doacoes.Doacoes;
using Xunit;

namespace Worker.Doacoes.Tests.Consumo;

public class DoacaoProcessorTests
{
    private static DoacoesDbContext CriarDbContext()
    {
        var options = new DbContextOptionsBuilder<DoacoesDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new DoacoesDbContext(options);
    }

    [Fact]
    public async Task ProcessarAsync_ComEventoNovo_ChamaApiEPersiste()
    {
        var db = CriarDbContext();
        var apiClient = new FakeCampanhasApiClient();
        var processor = new DoacaoProcessor(db, apiClient);
        var evento = new DoacaoRecebidaEvent(Guid.NewGuid(), Guid.NewGuid(), 100m, DateTime.UtcNow);

        await processor.ProcessarAsync(evento);

        var doacaoPersistida = await db.Doacoes.SingleAsync(d => d.IdDoacao == evento.IdDoacao);
        Assert.Equal(evento.IdCampanha, doacaoPersistida.IdCampanha);
        Assert.Equal(evento.ValorDoacao, doacaoPersistida.ValorDoacao);
        Assert.Single(apiClient.Chamadas);
        Assert.Equal((evento.IdCampanha, evento.IdDoacao, evento.ValorDoacao), apiClient.Chamadas[0]);
        Assert.Equal(DateTimeKind.Utc, doacaoPersistida.DataHora.Kind);
    }

    [Fact]
    public async Task ProcessarAsync_ComEventoJaProcessado_NaoChamaApiNovamente()
    {
        var db = CriarDbContext();
        var apiClient = new FakeCampanhasApiClient();
        var processor = new DoacaoProcessor(db, apiClient);
        var evento = new DoacaoRecebidaEvent(Guid.NewGuid(), Guid.NewGuid(), 100m, DateTime.UtcNow);

        await processor.ProcessarAsync(evento);
        await processor.ProcessarAsync(evento);

        Assert.Single(apiClient.Chamadas);
        Assert.Equal(1, await db.Doacoes.CountAsync());
    }

    [Fact]
    public async Task ProcessarAsync_QuandoApiFalha_NaoPersisteEPropagaExcecao()
    {
        var db = CriarDbContext();
        var apiClient = new FakeCampanhasApiClient { DeveLancarExcecao = true };
        var processor = new DoacaoProcessor(db, apiClient);
        var evento = new DoacaoRecebidaEvent(Guid.NewGuid(), Guid.NewGuid(), 100m, DateTime.UtcNow);

        await Assert.ThrowsAsync<HttpRequestException>(() => processor.ProcessarAsync(evento));

        Assert.Equal(0, await db.Doacoes.CountAsync());
    }
}
