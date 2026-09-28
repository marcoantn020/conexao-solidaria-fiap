namespace Frontend.ConexaoSolidaria.Models;

public enum StatusCampanha
{
    Ativa,
    Concluida,
    Cancelada
}

public record CampanhaPublicaResponse(Guid Id, string Titulo, decimal MetaFinanceira, decimal ValorArrecadado)
{
    public decimal PercentualAtingido => MetaFinanceira <= 0
        ? 0
        : Math.Min(100m, Math.Round(ValorArrecadado / MetaFinanceira * 100m, 1));
}

public record CampanhaRequest(string Titulo, string Descricao, DateTime DataInicio, DateTime DataFim, decimal MetaFinanceira);

public record AtualizarStatusRequest(StatusCampanha Status);
