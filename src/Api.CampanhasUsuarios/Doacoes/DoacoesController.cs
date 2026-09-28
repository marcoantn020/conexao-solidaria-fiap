using Api.CampanhasUsuarios.Campanhas;
using Api.CampanhasUsuarios.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.CampanhasUsuarios.Doacoes;

[ApiController]
[Route("doacoes")]
[Authorize(Roles = "Doador")]
public class DoacoesController : ControllerBase
{
    private readonly CampanhasDbContext _db;
    private readonly IEventPublisher _eventPublisher;

    public DoacoesController(CampanhasDbContext db, IEventPublisher eventPublisher)
    {
        _db = db;
        _eventPublisher = eventPublisher;
    }

    public record DoarRequest(Guid IdCampanha, decimal ValorDoacao);

    [HttpPost]
    public async Task<IActionResult> Doar(DoarRequest request)
    {
        var campanha = await _db.Campanhas.FindAsync(request.IdCampanha);
        if (campanha is null)
            return NotFound("Campanha não encontrada.");

        if (campanha.Status != StatusCampanha.Ativa)
            return BadRequest("Não é possível doar para uma campanha encerrada ou cancelada.");

        if (campanha.DataFim < DateTime.UtcNow)
            return BadRequest("Não é possível doar para uma campanha com prazo encerrado.");

        if (request.ValorDoacao <= 0)
            return BadRequest("O valor da doação deve ser maior que zero.");

        var evento = new DoacaoRecebidaEvent(Guid.NewGuid(), campanha.Id, request.ValorDoacao, DateTime.UtcNow);
        await _eventPublisher.PublicarAsync(evento);

        return Accepted(new { evento.IdDoacao });
    }
}
