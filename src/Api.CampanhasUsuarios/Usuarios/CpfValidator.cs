namespace Api.CampanhasUsuarios.Usuarios;

public static class CpfValidator
{
    public static bool IsValid(string? cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf))
            return false;

        var digits = new string(cpf.Where(char.IsDigit).ToArray());

        if (digits.Length != 11)
            return false;

        if (digits.Distinct().Count() == 1)
            return false;

        var multiplicador1 = new[] { 10, 9, 8, 7, 6, 5, 4, 3, 2 };
        var multiplicador2 = new[] { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2 };

        var primeirosNoveDigitos = digits[..9];
        var soma = 0;
        for (var i = 0; i < 9; i++)
            soma += (primeirosNoveDigitos[i] - '0') * multiplicador1[i];

        var resto = soma % 11;
        var primeiroDigitoVerificador = resto < 2 ? 0 : 11 - resto;

        var primeirosDezDigitos = primeirosNoveDigitos + primeiroDigitoVerificador;
        soma = 0;
        for (var i = 0; i < 10; i++)
            soma += (primeirosDezDigitos[i] - '0') * multiplicador2[i];

        resto = soma % 11;
        var segundoDigitoVerificador = resto < 2 ? 0 : 11 - resto;

        return digits == primeirosDezDigitos + segundoDigitoVerificador;
    }
}
