using Microsoft.EntityFrameworkCore;
using UniColab.Api.Data;
using UniColab.Api.Dtos;
using UniColab.Api.Entities;

namespace UniColab.Api.Repositories;

public interface IPainelRepositorio
{
    Task<List<MetricaDto>> MetricasAsync(long usuarioId, CancellationToken ct);
    Task<List<BadgeDto>> BadgesAsync(long usuarioId, CancellationToken ct);
    Task<List<TrilhaDto>> TrilhasAsync(long usuarioId, CancellationToken ct);
    Task<TrilhaDto?> UltimaTrilhaAsync(long usuarioId, CancellationToken ct);
    Task<List<HeatmapLinha>> HeatmapAsync(long instituicaoId, long? cursoId, CancellationToken ct);
    Task<List<AdesaoLinha>> AdesaoAsync(long instituicaoId, long? cursoId, CancellationToken ct);
    Task<List<LancamentoDto>> ExtratoAsync(long instituicaoId, long? professorId, CancellationToken ct);
    Task<SaldoDto> SaldoAsync(long professorId, CancellationToken ct);
}

public sealed class PainelRepositorio : IPainelRepositorio
{
    private readonly UnicolabDbContext _db;

    public PainelRepositorio(UnicolabDbContext db)
    {
        _db = db;
    }

    public Task<List<MetricaDto>> MetricasAsync(long usuarioId, CancellationToken ct) =>
        (
            from m in _db.Metricas
            join a in _db.Areas on m.AreaId equals a.Id
            where m.UsuarioId == usuarioId
            orderby a.Nome
            select new MetricaDto
            {
                AreaId = m.AreaId,
                Area = a.Nome,
                TotalResolvidas = m.TotalResolvidas,
                TotalAcertos = m.TotalAcertos,
                PercentualAcerto = m.PercentualAcerto
            }
        ).ToListAsync(ct);

    public Task<List<BadgeDto>> BadgesAsync(long usuarioId, CancellationToken ct) =>
        (
            from ub in _db.UsuarioBadges
            join b in _db.Badges on ub.BadgeId equals b.Id
            join a in _db.Areas on ub.AreaId equals a.Id into areas
            from a in areas.DefaultIfEmpty()
            where ub.UsuarioId == usuarioId
            orderby ub.ConcedidoEm descending
            select new BadgeDto
            {
                Codigo = b.Codigo,
                Nome = b.Nome,
                Descricao = b.Descricao,
                Pontos = ub.Pontos,
                Area = a.Nome,
                ConcedidoEm = ub.ConcedidoEm
            }
        ).ToListAsync(ct);

    public async Task<List<TrilhaDto>> TrilhasAsync(long usuarioId, CancellationToken ct)
    {
        var trilhas = await _db.Trilhas.Where(t => t.UsuarioId == usuarioId)
            .OrderByDescending(t => t.CriadaEm)
            .ToListAsync(ct);
        var resultado = new List<TrilhaDto>();
        foreach (var trilha in trilhas)
        {
            resultado.Add(await MontarAsync(trilha, ct));
        }

        return resultado;
    }

    public async Task<TrilhaDto?> UltimaTrilhaAsync(long usuarioId, CancellationToken ct)
    {
        var trilha = await _db.Trilhas.Where(t => t.UsuarioId == usuarioId)
            .OrderByDescending(t => t.Id)
            .FirstOrDefaultAsync(ct);
        return trilha is null ? null : await MontarAsync(trilha, ct);
    }

    public Task<List<HeatmapLinha>> HeatmapAsync(long instituicaoId, long? cursoId, CancellationToken ct)
    {
        var consulta = _db.Heatmap.Where(h => h.InstituicaoId == instituicaoId);
        if (cursoId is long curso)
        {
            consulta = consulta.Where(h => h.CursoId == curso);
        }

        return consulta.OrderBy(h => h.Curso).ThenBy(h => h.AreaConhecimento).ToListAsync(ct);
    }

    public Task<List<AdesaoLinha>> AdesaoAsync(long instituicaoId, long? cursoId, CancellationToken ct)
    {
        var consulta = _db.Adesao.Where(a => a.InstituicaoId == instituicaoId);
        if (cursoId is long curso)
        {
            consulta = consulta.Where(a => a.CursoId == curso);
        }

        return consulta.OrderBy(a => a.Curso).ToListAsync(ct);
    }

    public Task<List<LancamentoDto>> ExtratoAsync(long instituicaoId, long? professorId, CancellationToken ct)
    {
        var consulta = _db.Lancamentos.Where(l => l.InstituicaoId == instituicaoId);
        if (professorId is long professor)
        {
            consulta = consulta.Where(l => l.ProfessorId == professor);
        }

        return consulta.OrderByDescending(l => l.CriadoEm)
            .Select(l => new LancamentoDto
            {
                Tipo = l.Tipo,
                Valor = l.Valor,
                Descricao = l.Descricao,
                CriadoEm = l.CriadoEm
            })
            .ToListAsync(ct);
    }

    public async Task<SaldoDto> SaldoAsync(long professorId, CancellationToken ct)
    {
        var linhas = _db.Lancamentos.Where(l => l.ProfessorId == professorId);
        return new SaldoDto
        {
            Lancamentos = await linhas.CountAsync(ct),
            Saldo = await linhas.SumAsync(l => (decimal?)l.Valor, ct) ?? 0
        };
    }

    private async Task<TrilhaDto> MontarAsync(Trilha trilha, CancellationToken ct)
    {
        var itens = await (
            from item in _db.TrilhaQuestoes
            join q in _db.Questoes on item.QuestaoId equals q.Id
            join a in _db.Areas on q.AreaId equals a.Id
            where item.TrilhaId == trilha.Id
            orderby item.Ordem
            select new TrilhaItemDto
            {
                Ordem = item.Ordem,
                QuestaoId = item.QuestaoId,
                Motivo = item.Motivo,
                Area = a.Nome,
                Dificuldade = q.Dificuldade
            }
        ).ToListAsync(ct);

        return new TrilhaDto
        {
            Id = trilha.Id,
            Titulo = trilha.Titulo,
            Origem = trilha.Origem,
            CriadaEm = trilha.CriadaEm,
            Questoes = itens
        };
    }
}
