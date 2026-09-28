namespace Api.CampanhasUsuarios.Doacoes;

public record DoacaoRecebidaEvent(Guid IdDoacao, Guid IdCampanha, decimal ValorDoacao, DateTime DataHora);
