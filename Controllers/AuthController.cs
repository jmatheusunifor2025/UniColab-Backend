using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniColab.Api.Dtos;
using UniColab.Api.Services;

namespace UniColab.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthServico _auth;

    public AuthController(IAuthServico auth)
    {
        _auth = auth;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public Task<LoginResposta> Login([FromBody] LoginRequest pedido, CancellationToken ct) =>
        _auth.EntrarAsync(pedido, ct);

    [HttpGet("eu")]
    public Task<UsuarioResumo> Eu(CancellationToken ct) => _auth.EuAsync(ct);
}
