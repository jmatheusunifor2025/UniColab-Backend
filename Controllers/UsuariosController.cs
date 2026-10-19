using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniColab.Api.Dtos;
using UniColab.Api.Services;

namespace UniColab.Api.Controllers;

[ApiController]
[Route("api")]
public sealed class UsuariosController : ControllerBase
{
    private readonly IUsuarioServico _usuarios;

    public UsuariosController(IUsuarioServico usuarios)
    {
        _usuarios = usuarios;
    }

    [HttpGet("cursos")]
    public Task<List<CursoDto>> Cursos(CancellationToken ct) => _usuarios.CursosAsync(ct);

    [HttpGet("usuarios")]
    [Authorize(Roles = "administrador")]
    public Task<List<UsuarioResumo>> Listar(CancellationToken ct) => _usuarios.ListarAsync(ct);

    [HttpPost("usuarios")]
    [Authorize(Roles = "administrador")]
    public async Task<ActionResult<UsuarioResumo>> Criar([FromBody] CriarUsuarioRequest pedido, CancellationToken ct)
    {
        var criado = await _usuarios.CriarAsync(pedido, ct);
        return Created($"/api/usuarios/{criado.Id}", criado);
    }

    [HttpPatch("usuarios/{id:long}")]
    [Authorize(Roles = "administrador")]
    public Task<UsuarioResumo> Atualizar(long id, [FromBody] AtualizarUsuarioRequest pedido, CancellationToken ct) =>
        _usuarios.AtualizarAsync(id, pedido, ct);
}
