using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniColab.Api.Dtos;
using UniColab.Api.Services;

namespace UniColab.Api.Controllers;

[ApiController]
[Route("api/tentativas")]
public sealed class TentativasController : ControllerBase
{
    private readonly ITentativaServico _tentativas;

    public TentativasController(ITentativaServico tentativas)
    {
        _tentativas = tentativas;
    }

    [HttpGet("minhas")]
    [Authorize(Roles = "estudante")]
    public Task<List<TentativaResumo>> Minhas(CancellationToken ct) => _tentativas.MinhasAsync(ct);

    [HttpGet("{id:long}")]
    [Authorize(Roles = "estudante")]
    public Task<TentativaDto> Obter(long id, CancellationToken ct) => _tentativas.ObterAsync(id, ct);

    [HttpPost("{id:long}/respostas")]
    [Authorize(Roles = "estudante")]
    public Task<FolhaItemDto> Registrar(long id, [FromBody] RegistrarRespostaRequest pedido, CancellationToken ct) =>
        _tentativas.RegistrarAsync(id, pedido, ct);

    [HttpPost("{id:long}/finalizar")]
    [Authorize(Roles = "estudante")]
    public Task<TentativaDto> Finalizar(long id, CancellationToken ct) => _tentativas.FinalizarAsync(id, ct);
}
