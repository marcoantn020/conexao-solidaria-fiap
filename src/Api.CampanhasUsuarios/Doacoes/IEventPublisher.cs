namespace Api.CampanhasUsuarios.Doacoes;

public interface IEventPublisher
{
    Task PublicarAsync(DoacaoRecebidaEvent evento);
}
