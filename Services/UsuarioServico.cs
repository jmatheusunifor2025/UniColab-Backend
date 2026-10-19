using UniColab.Api.Dtos;
using UniColab.Api.Entities;
using UniColab.Api.Repositories;
using UniColab.Api.Seguranca;

namespace UniColab.Api.Services;

public interface IUsuarioServico
{
    Task<List<UsuarioResumo>> ListarAsync(CancellationToken ct);
    Task<UsuarioResumo> CriarAsync(CriarUsuarioRequest pedido, CancellationToken ct);
    Task<UsuarioResumo> AtualizarAsync(long id, AtualizarUsuarioRequest pedido, CancellationToken ct);
    Task<List<CursoDto>> CursosAsync(CancellationToken ct);
}

public sealed class UsuarioServico : IUsuarioServico
{
    private readonly IUsuarioRepositorio _usuarios;
    private readonly IUnidadeDeTrabalho _unidade;
    private readonly IUsuarioAtual _atual;

    public UsuarioServico(IUsuarioRepositorio usuarios, IUnidadeDeTrabalho unidade, IUsuarioAtual atual)
    {
        _usuarios = usuarios;
        _unidade = unidade;
        _atual = atual;
    }

    public async Task<List<UsuarioResumo>> ListarAsync(CancellationToken ct)
    {
        var lista = await _usuarios.ListarAsync(_atual.InstituicaoId, ct);
        return lista.Select(AuthServico.Mapear).ToList();
    }

    public async Task<UsuarioResumo> CriarAsync(CriarUsuarioRequest pedido, CancellationToken ct)
    {
        var papel = pedido.TipoPerfil.Trim().ToLowerInvariant();
        if (!Papeis.Todos.Contains(papel))
        {
            throw new RegraNegocioException("Papel inválido.");
        }

        if (await _usuarios.EmailExisteAsync(pedido.Email.Trim(), ct))
        {
            throw new RegraNegocioException("E-mail já cadastrado.");
        }

        ValidarCursoDoPapel(papel, pedido.CursoId);
        if (pedido.CursoId is long cursoId)
        {
            _ = await _usuarios.ObterCursoAsync(_atual.InstituicaoId, cursoId, ct)
                ?? throw new RegraNegocioException("Curso não pertence à instituição.");
        }

        var usuario = new Usuario
        {
            InstituicaoId = _atual.InstituicaoId,
            CursoId = papel is Papeis.Administrador ? null : pedido.CursoId,
            Nome = pedido.Nome.Trim(),
            Email = pedido.Email.Trim(),
            SenhaHash = SenhaHasher.Gerar(pedido.Senha),
            TipoPerfil = papel,
            Matricula = string.IsNullOrWhiteSpace(pedido.Matricula) ? null : pedido.Matricula.Trim(),
            ElegivelEnade = papel == Papeis.Estudante && pedido.ElegivelEnade,
            Ativo = true
        };

        await using var tx = await _unidade.IniciarTransacaoAsync(ct);
        await _usuarios.AdicionarAsync(usuario, ct);
        await _unidade.SalvarAsync(ct);

        if (papel == Papeis.Professor)
        {
            foreach (var curso in pedido.CursoIds.Distinct())
            {
                _ = await _usuarios.ObterCursoAsync(_atual.InstituicaoId, curso, ct)
                    ?? throw new RegraNegocioException("Curso do professor não pertence à instituição.");
                _usuarios.AdicionarVinculo(new DocenteCurso
                {
                    UsuarioId = usuario.Id,
                    CursoId = curso,
                    InstituicaoId = _atual.InstituicaoId
                });
            }
        }

        await _unidade.SalvarAsync(ct);
        await tx.CommitAsync(ct);
        return AuthServico.Mapear(usuario);
    }

    public async Task<UsuarioResumo> AtualizarAsync(long id, AtualizarUsuarioRequest pedido, CancellationToken ct)
    {
        var usuario = await _usuarios.ObterAsync(_atual.InstituicaoId, id, ct)
            ?? throw new NaoEncontradoException("Usuário não encontrado.");

        if (pedido.TipoPerfil is not null)
        {
            var papel = pedido.TipoPerfil.Trim().ToLowerInvariant();
            if (!Papeis.Todos.Contains(papel))
            {
                throw new RegraNegocioException("Papel inválido.");
            }

            usuario.TipoPerfil = papel;
        }

        if (pedido.LimparCurso)
        {
            usuario.CursoId = null;
        }
        else if (pedido.CursoId is long cursoId)
        {
            _ = await _usuarios.ObterCursoAsync(_atual.InstituicaoId, cursoId, ct)
                ?? throw new RegraNegocioException("Curso não pertence à instituição.");
            usuario.CursoId = cursoId;
        }

        if (pedido.Ativo is bool ativo)
        {
            usuario.Ativo = ativo;
            usuario.DesativadoPorId = ativo ? null : _atual.Id;
        }

        ValidarCursoDoPapel(usuario.TipoPerfil, usuario.CursoId);
        await using var tx = await _unidade.IniciarTransacaoAsync(ct);
        await _unidade.DefinirAtorAsync(_atual.Id, ct);
        await _unidade.SalvarAsync(ct);
        await tx.CommitAsync(ct);
        return AuthServico.Mapear(usuario);
    }

    public async Task<List<CursoDto>> CursosAsync(CancellationToken ct)
    {
        var cursos = await _usuarios.ListarCursosAsync(_atual.InstituicaoId, ct);
        return cursos.Select(c => new CursoDto { Id = c.Id, Nome = c.Nome, Codigo = c.Codigo }).ToList();
    }

    private static void ValidarCursoDoPapel(string papel, long? cursoId)
    {
        if (papel is Papeis.Estudante or Papeis.Coordenador && cursoId is null)
        {
            throw new RegraNegocioException("Estudante e coordenador precisam de um curso.");
        }

        if (papel == Papeis.Administrador && cursoId is not null)
        {
            throw new RegraNegocioException("Administrador não fica preso a um curso.");
        }
    }
}
