namespace Api.CampanhasUsuarios.Campanhas;

public class Campanha
{
    public Guid Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public DateTime DataInicio { get; set; }
    public DateTime DataFim { get; set; }
    public decimal MetaFinanceira { get; set; }
    public decimal ValorArrecadado { get; set; }
    public StatusCampanha Status { get; set; }
}
