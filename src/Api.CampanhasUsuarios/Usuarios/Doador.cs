namespace Api.CampanhasUsuarios.Usuarios;

public class Doador
{
    public Guid Id { get; set; }
    public string NomeCompleto { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Cpf { get; set; } = string.Empty;
    public string SenhaHash { get; set; } = string.Empty;
}
