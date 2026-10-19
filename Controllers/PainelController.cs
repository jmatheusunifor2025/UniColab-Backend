using Microsoft.AspNetCore.Mvc;
using UniColab.Api.Dtos;
using UniColab.Api.Entities;
using UniColab.Api.Services;

namespace UniColab.Api.Controllers;

[ApiController]
[Route("api")]
public sealed class PainelController : ControllerBase
{
    private readonly IPainelServico _painel;

    public PainelController(IPainelServico painel)
    {
        _painel = painel;
    }

    [HttpGet("dashboard")]
    public Task<List<MetricaDto>> Dashboard(CancellationToken ct) => _painel.DashboardAsync(ct);

    [HttpGet("badges")]
    public Task<List<BadgeDto>> Badges(CancellationToken ct) => _painel.BadgesAsync(ct);

    [HttpGet("trilhas")]
    public Task<List<TrilhaDto>> Trilhas(CancellationToken ct) => _painel.TrilhasAsync(ct);

    [HttpPost("trilhas/sugerir")]
    public Task<TrilhaDto> Sugerir(CancellationToken ct) => _painel.SugerirTrilhaAsync(ct);

    [HttpGet("relatorios/heatmap")]
    public Task<List<HeatmapLinha>> Heatmap([FromQuery] long? cursoId, CancellationToken ct) =>
        _painel.HeatmapAsync(cursoId, ct);

    [HttpGet("relatorios/adesao")]
    public Task<List<AdesaoLinha>> Adesao([FromQuery] long? cursoId, CancellationToken ct) =>
        _painel.AdesaoAsync(cursoId, ct);

    [HttpGet("docente/extrato")]
    public Task<List<LancamentoDto>> Extrato(CancellationToken ct) => _painel.ExtratoAsync(ct);

    [HttpGet("docente/saldo")]
    public Task<SaldoDto> Saldo(CancellationToken ct) => _painel.SaldoAsync(ct);
}
