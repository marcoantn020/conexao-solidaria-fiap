using System.Security.Cryptography;
using System.Text;
using Api.CampanhasUsuarios.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Api.CampanhasUsuarios.Doacoes;

[ApiController]
[Route("internal/campanhas")]
[AllowAnonymous]
public class InternalCampanhasController : ControllerBase
{
    private const string SharedSecretHeader = "X-Internal-Token";

    private readonly CampanhasDbContext _db;
    private readonly InternalOptions _options;

    public InternalCampanhasController(CampanhasDbContext db, IOptions<InternalOptions> options)
    {
        _db = db;
        _options = options.Value;
    }

    public record AtualizarArrecadadoRequest(Guid IdDoacao, decimal ValorDoacao);

    [HttpPatch("{id:guid}/arrecadado")]
    public async Task<IActionResult> AtualizarArrecadado(Guid id, AtualizarArrecadadoRequest request)
    {
        if (!SegredoValido(Request.Headers[SharedSecretHeader]))
            return Unauthorized();

        if (request.ValorDoacao <= 0)
            return BadRequest("O valor da doação deve ser maior que zero.");

        var jaProcessado = await _db.CallbacksProcessados.AnyAsync(c => c.IdDoacao == request.IdDoacao);
        if (jaProcessado)
        {
            var campanhaJaAtualizada = await _db.Campanhas.FindAsync(id);
            if (campanhaJaAtualizada is null)
                return NotFound();

            return Ok(new { campanhaJaAtualizada.Id, campanhaJaAtualizada.ValorArrecadado });
        }

        var campanha = await _db.Campanhas.FindAsync(id);
        if (campanha is null)
            return NotFound();

        campanha.ValorArrecadado += request.ValorDoacao;
        _db.CallbacksProcessados.Add(new CallbackProcessado { IdDoacao = request.IdDoacao, ProcessadoEm = DateTime.UtcNow });
        await _db.SaveChangesAsync();

        return Ok(new { campanha.Id, campanha.ValorArrecadado });
    }

    private bool SegredoValido(string? tokenRecebido)
    {
        var segredoEsperado = _options.SharedSecret;

        if (string.IsNullOrEmpty(segredoEsperado) || string.IsNullOrEmpty(tokenRecebido))
            return false;

        var bytesEsperado = Encoding.UTF8.GetBytes(segredoEsperado);
        var bytesRecebido = Encoding.UTF8.GetBytes(tokenRecebido);

        if (bytesEsperado.Length != bytesRecebido.Length)
            return false;

        return CryptographicOperations.FixedTimeEquals(bytesEsperado, bytesRecebido);
    }
}
