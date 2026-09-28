using Api.CampanhasUsuarios.Usuarios;
using Xunit;

namespace Api.CampanhasUsuarios.Tests.Usuarios;

public class CpfValidatorTests
{
    [Theory]
    [InlineData("52998224725")]
    [InlineData("529.982.247-25")]
    public void IsValid_ComCpfValido_RetornaTrue(string cpf)
    {
        Assert.True(CpfValidator.IsValid(cpf));
    }

    [Theory]
    [InlineData("11111111111")]
    [InlineData("12345678900")]
    [InlineData("123")]
    [InlineData("")]
    public void IsValid_ComCpfInvalido_RetornaFalse(string cpf)
    {
        Assert.False(CpfValidator.IsValid(cpf));
    }
}
