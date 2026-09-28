using Api.CampanhasUsuarios.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.CampanhasUsuarios.Usuarios;

[ApiController]
[Route("doadores")]
public class DoadoresController : ControllerBase
{
    private readonly CampanhasDbContext _db;

    public DoadoresController(CampanhasDbContext db)
    {
        _db = db;
    }

    public record CadastrarDoadorRequest(string NomeCompleto, string Email, string Cpf, string Senha);

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Cadastrar(CadastrarDoadorRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.NomeCompleto))
            return BadRequest("Nome completo é obrigatório.");

        if (!CpfValidator.IsValid(request.Cpf))
            return BadRequest("CPF inválido.");

        var emailJaExiste = await _db.Doadores.AnyAsync(d => d.Email == request.Email);
        if (emailJaExiste)
            return Conflict("Email já cadastrado.");

        var doador = new Doador
        {
            Id = Guid.NewGuid(),
            NomeCompleto = request.NomeCompleto,
            Email = request.Email,
            Cpf = request.Cpf,
            SenhaHash = BCrypt.Net.BCrypt.HashPassword(request.Senha)
        };

        _db.Doadores.Add(doador);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Cadastrar), new { id = doador.Id }, new { doador.Id, doador.NomeCompleto, doador.Email });
    }
}
