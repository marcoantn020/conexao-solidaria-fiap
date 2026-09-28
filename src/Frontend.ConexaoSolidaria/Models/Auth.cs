namespace Frontend.ConexaoSolidaria.Models;

public record LoginRequest(string Email, string Senha);
public record LoginResponse(string Token);
