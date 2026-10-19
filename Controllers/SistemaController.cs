using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniColab.Api.Data;
using UniColab.Api.Dtos;
using UniColab.Api.Services;

namespace UniColab.Api.Controllers;

[ApiController]
[Route("api")]
public sealed class SistemaController : ControllerBase
{
    private readonly ITentativaServico _tentativas;
    private readonly IForumServico _forum;
    private readonly UnicolabDbContext _db;

    public SistemaController(ITentativaServico tentativas, IForumServico forum, UnicolabDbContext db)
    {
        _tentativas = tentativas;
        _forum = forum;
        _db = db;
    }

    [HttpGet("health")]
    [AllowAnonymous]
    public async Task<IActionResult> Health(CancellationToken ct)
    {
        try
        {
            var ok = await _db.Database.CanConnectAsync(ct);
            return ok
                ? Ok(new { status = "ok" })
                : StatusCode(StatusCodes.Status503ServiceUnavailable, new { status = "indisponivel" });
        }
        catch
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { status = "indisponivel" });
        }
    }

    [HttpPost("sistema/expirar-tentativas")]
    [Authorize(Roles = "administrador")]
    public async Task<IActionResult> Expirar(CancellationToken ct)
    {
        await _tentativas.ExpirarAsync(ct);
        return NoContent();
    }

    [HttpGet("chamados")]
    public Task<List<ChamadoDto>> Chamados(CancellationToken ct) => _forum.ChamadosAsync(ct);

    [HttpPost("chamados")]
    public async Task<ActionResult<ChamadoDto>> AbrirChamado([FromBody] ChamadoRequest pedido, CancellationToken ct)
    {
        var criado = await _forum.AbrirChamadoAsync(pedido, ct);
        return Created($"/api/chamados/{criado.Id}", criado);
    }
}
