using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniColab.Api.Dtos;
using UniColab.Api.Services;

namespace UniColab.Api.Controllers;

[ApiController]
[Route("api")]
public sealed class QuestoesController : ControllerBase
{
    private readonly IQuestaoServico _questoes;

    public QuestoesController(IQuestaoServico questoes)
    {
        _questoes = questoes;
    }

    [HttpGet("areas")]
    public Task<List<AreaDto>> Areas(CancellationToken ct) => _questoes.AreasAsync(ct);

    [HttpGet("questoes")]
    public Task<Pagina<QuestaoListaItem>> Listar(
        [FromQuery] int? ano,
        [FromQuery] short? areaId,
        [FromQuery] string? dificuldade,
        [FromQuery] string? status,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanho = 20,
        CancellationToken ct = default) =>
        _questoes.ListarAsync(ano, areaId, dificuldade, status, pagina, tamanho, ct);

    [HttpGet("questoes/{id:long}")]
    public Task<QuestaoDetalhe> Obter(long id, CancellationToken ct) => _questoes.ObterAsync(id, ct);

    [HttpPost("questoes")]
    [Authorize(Roles = "professor")]
    public async Task<ActionResult<QuestaoDetalhe>> Criar([FromBody] SalvarQuestaoRequest pedido, CancellationToken ct)
    {
        var criada = await _questoes.CriarAsync(pedido, ct);
        return CreatedAtAction(nameof(Obter), new { id = criada.Id }, criada);
    }

    [HttpPut("questoes/{id:long}")]
    [Authorize(Roles = "professor")]
    public Task<QuestaoDetalhe> Atualizar(long id, [FromBody] SalvarQuestaoRequest pedido, CancellationToken ct) =>
        _questoes.AtualizarAsync(id, pedido, ct);

    [HttpPost("questoes/{id:long}/publicar")]
    [Authorize(Roles = "professor")]
    public async Task<IActionResult> Publicar(long id, CancellationToken ct)
    {
        await _questoes.PublicarAsync(id, ct);
        return NoContent();
    }

    [HttpPost("questoes/{id:long}/reabrir")]
    [Authorize(Roles = "professor")]
    public async Task<IActionResult> Reabrir(long id, CancellationToken ct)
    {
        await _questoes.ReabrirAsync(id, ct);
        return NoContent();
    }

    [HttpPost("questoes/{id:long}/retirar")]
    [Authorize(Roles = "administrador")]
    public async Task<IActionResult> Retirar(long id, [FromBody] RetirarQuestaoRequest pedido, CancellationToken ct)
    {
        await _questoes.RetirarAsync(id, pedido.Motivo, ct);
        return NoContent();
    }

    [HttpPost("questoes/{id:long}/alternativas")]
    [Authorize(Roles = "professor")]
    public Task<AlternativaDto> CriarAlternativa(long id, [FromBody] SalvarAlternativaRequest pedido, CancellationToken ct) =>
        _questoes.SalvarAlternativaAsync(id, null, pedido, ct);

    [HttpPut("questoes/{questaoId:long}/alternativas/{alternativaId:long}")]
    [Authorize(Roles = "professor")]
    public Task<AlternativaDto> AtualizarAlternativa(
        long questaoId,
        long alternativaId,
        [FromBody] SalvarAlternativaRequest pedido,
        CancellationToken ct) =>
        _questoes.SalvarAlternativaAsync(questaoId, alternativaId, pedido, ct);

    [HttpPut("questoes/{questaoId:long}/alternativas/{alternativaId:long}/justificativa")]
    [Authorize(Roles = "professor")]
    public async Task<IActionResult> Justificativa(
        long questaoId,
        long alternativaId,
        [FromBody] SalvarJustificativaRequest pedido,
        CancellationToken ct)
    {
        await _questoes.SalvarJustificativaAsync(questaoId, alternativaId, pedido.Explicacao, ct);
        return NoContent();
    }

    [HttpPost("questoes/{id:long}/responder")]
    [Authorize(Roles = "estudante")]
    public Task<ResultadoResposta> Responder(long id, [FromBody] ResponderQuestaoRequest pedido, CancellationToken ct) =>
        _questoes.ResponderAsync(id, pedido, ct);
}
