using Api.CampanhasUsuarios.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.CampanhasUsuarios.Campanhas;

[ApiController]
[Route("campanhas")]
[Authorize(Roles = "GestorONG")]
public class CampanhasController : ControllerBase
{
    private readonly CampanhasDbContext _db;

    public CampanhasController(CampanhasDbContext db)
    {
        _db = db;
    }

    public record CampanhaRequest(string Titulo, string Descricao, DateTime DataInicio, DateTime DataFim, decimal MetaFinanceira);

    [HttpPost]
    public async Task<IActionResult> Criar(CampanhaRequest request)
    {
        var dataInicio = DateTime.SpecifyKind(request.DataInicio, DateTimeKind.Utc);
        var dataFim = DateTime.SpecifyKind(request.DataFim, DateTimeKind.Utc);

        var erros = CampanhaValidator.Validar(dataFim, request.MetaFinanceira, DateTime.UtcNow);
        if (erros.Count > 0)
            return BadRequest(erros);

        var campanha = new Campanha
        {
            Id = Guid.NewGuid(),
            Titulo = request.Titulo,
            Descricao = request.Descricao,
            DataInicio = dataInicio,
            DataFim = dataFim,
            MetaFinanceira = request.MetaFinanceira,
            ValorArrecadado = 0m,
            Status = StatusCampanha.Ativa
        };

        _db.Campanhas.Add(campanha);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Criar), new { id = campanha.Id }, campanha);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Editar(Guid id, CampanhaRequest request)
    {
        var campanha = await _db.Campanhas.FindAsync(id);
        if (campanha is null)
            return NotFound();

        var dataInicio = DateTime.SpecifyKind(request.DataInicio, DateTimeKind.Utc);
        var dataFim = DateTime.SpecifyKind(request.DataFim, DateTimeKind.Utc);

        var erros = CampanhaValidator.Validar(dataFim, request.MetaFinanceira, DateTime.UtcNow);
        if (erros.Count > 0)
            return BadRequest(erros);

        campanha.Titulo = request.Titulo;
        campanha.Descricao = request.Descricao;
        campanha.DataInicio = dataInicio;
        campanha.DataFim = dataFim;
        campanha.MetaFinanceira = request.MetaFinanceira;

        await _db.SaveChangesAsync();

        return Ok(campanha);
    }

    public record CampanhaPublicaResponse(Guid Id, string Titulo, decimal MetaFinanceira, decimal ValorArrecadado);

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ListarPublico()
    {
        var campanhas = await _db.Campanhas
            .Where(c => c.Status == StatusCampanha.Ativa && c.DataFim >= DateTime.UtcNow)
            .Select(c => new CampanhaPublicaResponse(c.Id, c.Titulo, c.MetaFinanceira, c.ValorArrecadado))
            .ToListAsync();

        return Ok(campanhas);
    }

    public record AtualizarStatusRequest(StatusCampanha Status);

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> AtualizarStatus(Guid id, AtualizarStatusRequest request)
    {
        var campanha = await _db.Campanhas.FindAsync(id);
        if (campanha is null)
            return NotFound();

        campanha.Status = request.Status;
        await _db.SaveChangesAsync();

        return Ok(campanha);
    }
}
