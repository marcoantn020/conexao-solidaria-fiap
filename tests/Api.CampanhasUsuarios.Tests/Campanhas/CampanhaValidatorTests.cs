using Api.CampanhasUsuarios.Campanhas;
using Xunit;

namespace Api.CampanhasUsuarios.Tests.Campanhas;

public class CampanhaValidatorTests
{
    private static readonly DateTime Agora = new(2026, 8, 29, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Validar_ComDataFimNoPassado_RetornaErro()
    {
        var erros = CampanhaValidator.Validar(Agora.AddDays(-1), 1000m, Agora);

        Assert.Contains("A data de término não pode estar no passado.", erros);
    }

    [Fact]
    public void Validar_ComMetaFinanceiraZeroOuNegativa_RetornaErro()
    {
        var erros = CampanhaValidator.Validar(Agora.AddDays(30), 0m, Agora);

        Assert.Contains("A meta financeira deve ser maior que zero.", erros);
    }

    [Fact]
    public void Validar_ComDadosValidos_NaoRetornaErros()
    {
        var erros = CampanhaValidator.Validar(Agora.AddDays(30), 1000m, Agora);

        Assert.Empty(erros);
    }
}
