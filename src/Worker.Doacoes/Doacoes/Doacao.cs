namespace Worker.Doacoes.Doacoes;

public class Doacao
{
    public Guid IdDoacao { get; set; }
    public Guid IdCampanha { get; set; }
    public decimal ValorDoacao { get; set; }
    public DateTime DataHora { get; set; }
    public DateTime ProcessadoEm { get; set; }
}
