using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace UniColab.Api.Seguranca;

public interface IUsuarioAtual
{
    bool EstaAutenticado { get; }
    long Id { get; }
    long InstituicaoId { get; }
    long? CursoId { get; }
    string Papel { get; }
    string Nome { get; }
    bool Eh(params string[] papeis);
}

public sealed class UsuarioAtual : IUsuarioAtual
{
    private readonly IHttpContextAccessor _http;

    public UsuarioAtual(IHttpContextAccessor http)
    {
        _http = http;
    }

    private ClaimsPrincipal Usuario => _http.HttpContext?.User ?? new ClaimsPrincipal();

    public bool EstaAutenticado => Usuario.Identity?.IsAuthenticated == true;

    public long Id => long.Parse(Usuario.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new AcessoNegadoException("Sessão sem identificador."));

    public long InstituicaoId => long.Parse(Usuario.FindFirstValue("instituicao_id")
        ?? throw new AcessoNegadoException("Sessão sem instituição."));

    public long? CursoId =>
        long.TryParse(Usuario.FindFirstValue("curso_id"), out var curso) ? curso : null;

    public string Papel => Usuario.FindFirstValue(ClaimTypes.Role)
        ?? throw new AcessoNegadoException("Sessão sem papel.");

    public string Nome => Usuario.FindFirstValue(ClaimTypes.Name) ?? "";

    public bool Eh(params string[] papeis) => papeis.Contains(Papel);
}

public static class Papeis
{
    public const string Estudante = "estudante";
    public const string Professor = "professor";
    public const string Coordenador = "coordenador";
    public const string Administrador = "administrador";

    public static readonly string[] Todos = [Estudante, Professor, Coordenador, Administrador];
}

public static class SenhaHasher
{
    public static string Gerar(string senha) => BCrypt.Net.BCrypt.HashPassword(senha, workFactor: 10);

    public static bool Confere(string senha, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(senha, hash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return false;
        }
    }
}

public sealed class JwtEmissor
{
    private readonly IConfiguration _config;

    public JwtEmissor(IConfiguration config)
    {
        _config = config;
    }

    public (string token, DateTime expiraEm) Emitir(Entities.Usuario usuario)
    {
        var horas = _config.GetValue("Jwt:ExpiraHoras", 8);
        var expiraEm = DateTime.UtcNow.AddHours(horas);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Chave()));
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Name, usuario.Nome),
            new(ClaimTypes.Email, usuario.Email),
            new(ClaimTypes.Role, usuario.TipoPerfil),
            new("instituicao_id", usuario.InstituicaoId.ToString())
        };
        if (usuario.CursoId is long curso)
        {
            claims.Add(new Claim("curso_id", curso.ToString()));
        }

        var jwt = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: expiraEm,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return (new JwtSecurityTokenHandler().WriteToken(jwt), expiraEm);
    }

    public string Chave()
    {
        var chave = _config["Jwt:Key"] ?? "";
        if (Encoding.UTF8.GetByteCount(chave) < 32)
        {
            throw new InvalidOperationException("Jwt:Key precisa ter pelo menos 32 bytes.");
        }

        return chave;
    }
}

public sealed class NaoEncontradoException : Exception
{
    public NaoEncontradoException(string mensagem) : base(mensagem)
    {
    }
}

public sealed class RegraNegocioException : Exception
{
    public RegraNegocioException(string mensagem) : base(mensagem)
    {
    }
}

public sealed class AcessoNegadoException : Exception
{
    public AcessoNegadoException(string mensagem) : base(mensagem)
    {
    }
}
