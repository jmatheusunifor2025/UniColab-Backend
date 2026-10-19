using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniColab.Api.Dtos;
using UniColab.Api.Services;

namespace UniColab.Api.Controllers;

[ApiController]
[Route("api/simulados")]
public sealed class SimuladosController : ControllerBase
{
    private readonly ISimuladoServico _simulados;
    private readonly ITentativaServico _tentativas;

    public SimuladosController(ISimuladoServico simulados, ITentativaServico tentativas)
    {
        _simulados = simulados;
        _tentativas = tentativas;
    }

    [HttpGet]
    public Task<List<SimuladoDto>> Listar(CancellationToken ct) => _simulados.ListarAsync(ct);

    [HttpGet("{id:long}")]
    public Task<SimuladoDto> Obter(long id, CancellationToken ct) => _simulados.ObterAsync(id, ct);

    [HttpPost]
    [Authorize(Roles = "professor,administrador")]
    public async Task<ActionResult<SimuladoDto>> Criar([FromBody] CriarSimuladoRequest pedido, CancellationToken ct)
    {
        var criado = await _simulados.CriarAsync(pedido, ct);
        return CreatedAtAction(nameof(Obter), new { id = criado.Id }, criado);
    }

    [HttpPut("{id:long}/questoes")]
    [Authorize(Roles = "professor,administrador")]
    public Task<SimuladoDto> DefinirQuestoes(long id, [FromBody] DefinirQuestoesSimuladoRequest pedido, CancellationToken ct) =>
        _simulados.DefinirQuestoesAsync(id, pedido.QuestaoIds, ct);

    [HttpPost("{id:long}/iniciar")]
    [Authorize(Roles = "estudante")]
    public Task<TentativaDto> Iniciar(long id, CancellationToken ct) => _tentativas.IniciarAsync(id, ct);
}
