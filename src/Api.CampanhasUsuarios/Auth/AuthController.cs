using Api.CampanhasUsuarios.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.CampanhasUsuarios.Auth;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly CampanhasDbContext _db;
    private readonly JwtTokenService _tokenService;

    public AuthController(CampanhasDbContext db, JwtTokenService tokenService)
    {
        _db = db;
        _tokenService = tokenService;
    }

    public record LoginRequest(string Email, string Senha);
    public record LoginResponse(string Token);

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var gestor = await _db.GestorOngs.SingleOrDefaultAsync(g => g.Email == request.Email);
        if (gestor is not null && BCrypt.Net.BCrypt.Verify(request.Senha, gestor.SenhaHash))
        {
            var tokenGestor = _tokenService.GerarToken(gestor.Id.ToString(), gestor.Email, "GestorONG");
            return Ok(new LoginResponse(tokenGestor));
        }

        var doador = await _db.Doadores.SingleOrDefaultAsync(d => d.Email == request.Email);
        if (doador is not null && BCrypt.Net.BCrypt.Verify(request.Senha, doador.SenhaHash))
        {
            var tokenDoador = _tokenService.GerarToken(doador.Id.ToString(), doador.Email, "Doador");
            return Ok(new LoginResponse(tokenDoador));
        }

        return Unauthorized();
    }
}
