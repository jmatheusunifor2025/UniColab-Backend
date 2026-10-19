using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniColab.Api.Dtos;
using UniColab.Api.Services;

namespace UniColab.Api.Controllers;

[ApiController]
[Route("api")]
public sealed class ForumController : ControllerBase
{
    private readonly IForumServico _forum;

    public ForumController(IForumServico forum)
    {
        _forum = forum;
    }

    [HttpGet("questoes/{questaoId:long}/comentarios")]
    public Task<List<ComentarioDto>> Listar(long questaoId, CancellationToken ct) =>
        _forum.ListarAsync(questaoId, ct);

    [HttpPost("questoes/{questaoId:long}/comentarios")]
    [Authorize(Roles = "estudante,professor")]
    public async Task<ActionResult<ComentarioDto>> Comentar(long questaoId, [FromBody] ComentarioRequest pedido, CancellationToken ct)
    {
        var criado = await _forum.ComentarAsync(questaoId, pedido, ct);
        return Created($"/api/questoes/{questaoId}/comentarios", criado);
    }

    [HttpPost("comentarios/{id:long}/reportes")]
    public async Task<IActionResult> Reportar(long id, [FromBody] ReporteRequest pedido, CancellationToken ct)
    {
        await _forum.ReportarAsync(id, pedido, ct);
        return NoContent();
    }

    [HttpPost("comentarios/{id:long}/moderacao")]
    [Authorize(Roles = "administrador")]
    public async Task<IActionResult> Moderar(long id, [FromBody] ModerarComentarioRequest pedido, CancellationToken ct)
    {
        await _forum.ModerarAsync(id, pedido.Status, ct);
        return NoContent();
    }

    [HttpGet("moderacao/fila")]
    [Authorize(Roles = "administrador")]
    public Task<List<FilaItemDto>> Fila(CancellationToken ct) => _forum.FilaAsync(ct);

    [HttpPost("reportes/{id:long}/julgamento")]
    [Authorize(Roles = "administrador")]
    public async Task<IActionResult> Julgar(long id, [FromBody] JulgarReporteRequest pedido, CancellationToken ct)
    {
        await _forum.JulgarAsync(id, pedido.Status, ct);
        return NoContent();
    }
}
