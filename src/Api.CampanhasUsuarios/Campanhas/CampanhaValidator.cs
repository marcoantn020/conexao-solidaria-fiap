namespace Api.CampanhasUsuarios.Campanhas;

public static class CampanhaValidator
{
    public static IReadOnlyList<string> Validar(DateTime dataFim, decimal metaFinanceira, DateTime agora)
    {
        var erros = new List<string>();

        if (dataFim <= agora)
            erros.Add("A data de término não pode estar no passado.");

        if (metaFinanceira <= 0)
            erros.Add("A meta financeira deve ser maior que zero.");

        return erros;
    }
}
