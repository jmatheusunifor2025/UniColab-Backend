using Microsoft.EntityFrameworkCore;
using UniColab.Api.Data;
using UniColab.Api.Dtos;
using UniColab.Api.Entities;

namespace UniColab.Api.Repositories;

public interface IComunidadeRepositorio
{
    Task<List<ComentarioDto>> ListarComentariosAsync(long instituicaoId, long questaoId, bool incluirModerados, CancellationToken ct);
    Task<ComentarioForum?> ObterComentarioAsync(long instituicaoId, long id, CancellationToken ct);
    void Adicionar(ComentarioForum comentario);
    void Adicionar(ForumReporte reporte);
    Task<List<FilaItemDto>> FilaAsync(long instituicaoId, CancellationToken ct);
    Task<ForumReporte?> ObterReporteAsync(long instituicaoId, long id, CancellationToken ct);
    Task<List<ChamadoDto>> ListarChamadosAsync(long instituicaoId, long? abertoPorId, CancellationToken ct);
    void Adicionar(ChamadoTi chamado);
}

public sealed class ComunidadeRepositorio : IComunidadeRepositorio
{
    private readonly UnicolabDbContext _db;

    public ComunidadeRepositorio(UnicolabDbContext db)
    {
        _db = db;
    }

    public Task<List<ComentarioDto>> ListarComentariosAsync(long instituicaoId, long questaoId, bool incluirModerados, CancellationToken ct)
    {
        var consulta =
            from c in _db.Comentarios
            join u in _db.Usuarios on c.UsuarioId equals u.Id
            where c.InstituicaoId == instituicaoId && c.QuestaoId == questaoId
            orderby c.CriadoEm
            select new ComentarioDto
            {
                Id = c.Id,
                ComentarioPaiId = c.ComentarioPaiId,
                UsuarioId = c.UsuarioId,
                Autor = u.Nome,
                Papel = u.TipoPerfil,
                Mensagem = c.Mensagem,
                Status = c.Status,
                CriadoEm = c.CriadoEm
            };

        if (!incluirModerados)
        {
            consulta = consulta.Where(c => c.Status == "publicado");
        }

        return consulta.ToListAsync(ct);
    }

    public Task<ComentarioForum?> ObterComentarioAsync(long instituicaoId, long id, CancellationToken ct) =>
        _db.Comentarios.FirstOrDefaultAsync(c => c.Id == id && c.InstituicaoId == instituicaoId, ct);

    public void Adicionar(ComentarioForum comentario) => _db.Comentarios.Add(comentario);
    public void Adicionar(ForumReporte reporte) => _db.Reportes.Add(reporte);

    public Task<List<FilaItemDto>> FilaAsync(long instituicaoId, CancellationToken ct) =>
        (
            from r in _db.Reportes
            join c in _db.Comentarios on r.ComentarioId equals c.Id
            where r.InstituicaoId == instituicaoId && r.Status == "aberto"
            orderby r.CriadoEm
            select new FilaItemDto
            {
                ReporteId = r.Id,
                QuestaoId = r.QuestaoId,
                ComentarioId = r.ComentarioId,
                Motivo = r.Motivo,
                Detalhe = r.Detalhe,
                Mensagem = c.Mensagem,
                CriadoEm = r.CriadoEm
            }
        ).ToListAsync(ct);

    public Task<ForumReporte?> ObterReporteAsync(long instituicaoId, long id, CancellationToken ct) =>
        _db.Reportes.FirstOrDefaultAsync(r => r.Id == id && r.InstituicaoId == instituicaoId, ct);

    public Task<List<ChamadoDto>> ListarChamadosAsync(long instituicaoId, long? abertoPorId, CancellationToken ct)
    {
        var consulta = _db.Chamados.Where(c => c.InstituicaoId == instituicaoId);
        if (abertoPorId is long autor)
        {
            consulta = consulta.Where(c => c.AbertoPorId == autor);
        }

        return consulta
            .OrderByDescending(c => c.CriadoEm)
            .Select(c => new ChamadoDto
            {
                Id = c.Id,
                Assunto = c.Assunto,
                Descricao = c.Descricao,
                Status = c.Status,
                CriadoEm = c.CriadoEm
            })
            .ToListAsync(ct);
    }

    public void Adicionar(ChamadoTi chamado) => _db.Chamados.Add(chamado);
}
