namespace Worker.Doacoes.Doacoes;

public record DoacaoRecebidaEvent(Guid IdDoacao, Guid IdCampanha, decimal ValorDoacao, DateTime DataHora);
