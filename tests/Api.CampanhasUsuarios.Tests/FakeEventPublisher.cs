using Api.CampanhasUsuarios.Doacoes;

namespace Api.CampanhasUsuarios.Tests;

public class FakeEventPublisher : IEventPublisher
{
    public List<DoacaoRecebidaEvent> EventosPublicados { get; } = new();

    public Task PublicarAsync(DoacaoRecebidaEvent evento)
    {
        EventosPublicados.Add(evento);
        return Task.CompletedTask;
    }
}
