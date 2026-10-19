using Microsoft.EntityFrameworkCore;
using UniColab.Api.Data;
using UniColab.Api.Entities;

namespace UniColab.Api.Repositories;

public interface IAvaliacaoRepositorio
{
    Task<List<Simulado>> ListarSimuladosAsync(long instituicaoId, bool somenteAtivos, CancellationToken ct);
    Task<Simulado?> ObterSimuladoAsync(long instituicaoId, long id, CancellationToken ct);
    Task<List<long>> QuestoesDoSimuladoAsync(long simuladoId, CancellationToken ct);
    void Adicionar(Simulado simulado);
    Task SubstituirQuestoesAsync(long simuladoId, long instituicaoId, IReadOnlyList<long> questaoIds, CancellationToken ct);
    Task<Tentativa?> ObterTentativaAsync(long instituicaoId, long id, CancellationToken ct);
    Task<List<Tentativa>> ListarTentativasAsync(long usuarioId, CancellationToken ct);
    Task<string?> TituloSimuladoAsync(long? simuladoId, CancellationToken ct);
    Task<List<TentativaQuestao>> FolhaAsync(long tentativaId, CancellationToken ct);
    void Adicionar(Tentativa tentativa);
    void Adicionar(TentativaQuestao item);
}

public sealed class AvaliacaoRepositorio : IAvaliacaoRepositorio
{
    private readonly UnicolabDbContext _db;

    public AvaliacaoRepositorio(UnicolabDbContext db)
    {
        _db = db;
    }

    public Task<List<Simulado>> ListarSimuladosAsync(long instituicaoId, bool somenteAtivos, CancellationToken ct)
    {
        var consulta = _db.Simulados.Where(s => s.InstituicaoId == instituicaoId);
        if (somenteAtivos)
        {
            consulta = consulta.Where(s => s.Ativo);
        }

        return consulta.OrderByDescending(s => s.CriadoEm).ToListAsync(ct);
    }

    public Task<Simulado?> ObterSimuladoAsync(long instituicaoId, long id, CancellationToken ct) =>
        _db.Simulados.FirstOrDefaultAsync(s => s.Id == id && s.InstituicaoId == instituicaoId, ct);

    public Task<List<long>> QuestoesDoSimuladoAsync(long simuladoId, CancellationToken ct) =>
        _db.SimuladoQuestoes.Where(s => s.SimuladoId == simuladoId)
            .OrderBy(s => s.Ordem)
            .Select(s => s.QuestaoId)
            .ToListAsync(ct);

    public void Adicionar(Simulado simulado) => _db.Simulados.Add(simulado);

    public async Task SubstituirQuestoesAsync(long simuladoId, long instituicaoId, IReadOnlyList<long> questaoIds, CancellationToken ct)
    {
        var atuais = await _db.SimuladoQuestoes.Where(s => s.SimuladoId == simuladoId).ToListAsync(ct);
        _db.SimuladoQuestoes.RemoveRange(atuais);
        if (atuais.Count > 0)
        {
            await _db.SaveChangesAsync(ct);
        }

        short ordem = 1;
        foreach (var questaoId in questaoIds)
        {
            _db.SimuladoQuestoes.Add(new SimuladoQuestao
            {
                SimuladoId = simuladoId,
                QuestaoId = questaoId,
                InstituicaoId = instituicaoId,
                Ordem = ordem++
            });
        }
    }

    public Task<Tentativa?> ObterTentativaAsync(long instituicaoId, long id, CancellationToken ct) =>
        _db.Tentativas.FirstOrDefaultAsync(t => t.Id == id && t.InstituicaoId == instituicaoId, ct);

    public Task<List<Tentativa>> ListarTentativasAsync(long usuarioId, CancellationToken ct) =>
        _db.Tentativas.Where(t => t.UsuarioId == usuarioId)
            .OrderByDescending(t => t.IniciadoEm)
            .ToListAsync(ct);

    public Task<string?> TituloSimuladoAsync(long? simuladoId, CancellationToken ct) =>
        simuladoId is null
            ? Task.FromResult<string?>(null)
            : _db.Simulados.Where(s => s.Id == simuladoId).Select(s => s.Titulo).FirstOrDefaultAsync(ct);

    public Task<List<TentativaQuestao>> FolhaAsync(long tentativaId, CancellationToken ct) =>
        _db.TentativaQuestoes.Where(t => t.TentativaId == tentativaId).OrderBy(t => t.Ordem).ToListAsync(ct);

    public void Adicionar(Tentativa tentativa) => _db.Tentativas.Add(tentativa);
    public void Adicionar(TentativaQuestao item) => _db.TentativaQuestoes.Add(item);
}
