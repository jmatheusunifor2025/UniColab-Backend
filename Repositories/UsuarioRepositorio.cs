using Microsoft.EntityFrameworkCore;
using UniColab.Api.Data;
using UniColab.Api.Entities;

namespace UniColab.Api.Repositories;

public interface IUsuarioRepositorio
{
    Task<List<Usuario>> BuscarParaLoginAsync(string identificador, string? sigla, CancellationToken ct);
    Task<Usuario?> ObterAsync(long instituicaoId, long id, CancellationToken ct);
    Task<bool> EmailExisteAsync(string email, CancellationToken ct);
    Task<List<Usuario>> ListarAsync(long instituicaoId, CancellationToken ct);
    Task<List<Curso>> ListarCursosAsync(long instituicaoId, CancellationToken ct);
    Task<Curso?> ObterCursoAsync(long instituicaoId, long cursoId, CancellationToken ct);
    Task AdicionarAsync(Usuario usuario, CancellationToken ct);
    void AdicionarVinculo(DocenteCurso vinculo);
    Task RemoverVinculosAsync(long usuarioId, CancellationToken ct);
    Task<List<long>> CursosDoDocenteAsync(long usuarioId, CancellationToken ct);
}

public sealed class UsuarioRepositorio : IUsuarioRepositorio
{
    private readonly UnicolabDbContext _db;

    public UsuarioRepositorio(UnicolabDbContext db)
    {
        _db = db;
    }

    public Task<List<Usuario>> BuscarParaLoginAsync(string identificador, string? sigla, CancellationToken ct)
    {
        var consulta = _db.Usuarios.AsQueryable();
        consulta = identificador.Contains('@')
            ? consulta.Where(u => u.Email == identificador)
            : consulta.Where(u => u.Matricula == identificador);

        if (!string.IsNullOrWhiteSpace(sigla))
        {
            consulta = consulta.Where(u =>
                _db.Instituicoes.Any(i => i.Id == u.InstituicaoId && i.Sigla == sigla));
        }

        return consulta.ToListAsync(ct);
    }

    public Task<Usuario?> ObterAsync(long instituicaoId, long id, CancellationToken ct) =>
        _db.Usuarios.FirstOrDefaultAsync(u => u.Id == id && u.InstituicaoId == instituicaoId, ct);

    public Task<bool> EmailExisteAsync(string email, CancellationToken ct) =>
        _db.Usuarios.AnyAsync(u => u.Email == email, ct);

    public Task<List<Usuario>> ListarAsync(long instituicaoId, CancellationToken ct) =>
        _db.Usuarios.Where(u => u.InstituicaoId == instituicaoId).OrderBy(u => u.Nome).ToListAsync(ct);

    public Task<List<Curso>> ListarCursosAsync(long instituicaoId, CancellationToken ct) =>
        _db.Cursos.Where(c => c.InstituicaoId == instituicaoId && c.Ativo).OrderBy(c => c.Nome).ToListAsync(ct);

    public Task<Curso?> ObterCursoAsync(long instituicaoId, long cursoId, CancellationToken ct) =>
        _db.Cursos.FirstOrDefaultAsync(c => c.Id == cursoId && c.InstituicaoId == instituicaoId, ct);

    public Task AdicionarAsync(Usuario usuario, CancellationToken ct) =>
        _db.Usuarios.AddAsync(usuario, ct).AsTask();

    public void AdicionarVinculo(DocenteCurso vinculo) => _db.DocenteCursos.Add(vinculo);

    public async Task RemoverVinculosAsync(long usuarioId, CancellationToken ct)
    {
        var vinculos = await _db.DocenteCursos.Where(v => v.UsuarioId == usuarioId).ToListAsync(ct);
        _db.DocenteCursos.RemoveRange(vinculos);
    }

    public Task<List<long>> CursosDoDocenteAsync(long usuarioId, CancellationToken ct) =>
        _db.DocenteCursos.Where(v => v.UsuarioId == usuarioId).Select(v => v.CursoId).ToListAsync(ct);
}
