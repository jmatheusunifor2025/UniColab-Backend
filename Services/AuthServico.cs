using UniColab.Api.Dtos;
using UniColab.Api.Repositories;
using UniColab.Api.Seguranca;

namespace UniColab.Api.Services;

public interface IAuthServico
{
    Task<LoginResposta> EntrarAsync(LoginRequest pedido, CancellationToken ct);
    Task<UsuarioResumo> EuAsync(CancellationToken ct);
}

public sealed class AuthServico : IAuthServico
{
    private readonly IUsuarioRepositorio _usuarios;
    private readonly IUnidadeDeTrabalho _unidade;
    private readonly IUsuarioAtual _atual;
    private readonly JwtEmissor _jwt;

    public AuthServico(IUsuarioRepositorio usuarios, IUnidadeDeTrabalho unidade, IUsuarioAtual atual, JwtEmissor jwt)
    {
        _usuarios = usuarios;
        _unidade = unidade;
        _atual = atual;
        _jwt = jwt;
    }

    public async Task<LoginResposta> EntrarAsync(LoginRequest pedido, CancellationToken ct)
    {
        var identificador = pedido.Identificador.Trim();
        var encontrados = await _usuarios.BuscarParaLoginAsync(identificador, pedido.SiglaInstituicao?.Trim(), ct);
        if (encontrados.Count > 1)
        {
            throw new RegraNegocioException("Informe a sigla da instituição. Essa matrícula aparece em mais de uma.");
        }

        var usuario = encontrados.SingleOrDefault();
        if (usuario is null || !usuario.Ativo || !SenhaHasher.Confere(pedido.Senha, usuario.SenhaHash))
        {
            throw new RegraNegocioException("Credenciais inválidas.");
        }

        usuario.UltimoAcessoEm = DateTime.Now;
        await _unidade.SalvarAsync(ct);
        var (token, expiraEm) = _jwt.Emitir(usuario);
        return new LoginResposta
        {
            Token = token,
            ExpiraEm = expiraEm,
            Usuario = Mapear(usuario)
        };
    }

    public async Task<UsuarioResumo> EuAsync(CancellationToken ct)
    {
        var usuario = await _usuarios.ObterAsync(_atual.InstituicaoId, _atual.Id, ct)
            ?? throw new NaoEncontradoException("Usuário não encontrado.");
        return Mapear(usuario);
    }

    public static UsuarioResumo Mapear(Entities.Usuario usuario) => new()
    {
        Id = usuario.Id,
        Nome = usuario.Nome,
        Email = usuario.Email,
        TipoPerfil = usuario.TipoPerfil,
        InstituicaoId = usuario.InstituicaoId,
        CursoId = usuario.CursoId,
        Matricula = usuario.Matricula
    };
}
