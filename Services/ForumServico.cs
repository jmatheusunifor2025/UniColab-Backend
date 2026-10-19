using UniColab.Api.Dtos;
using UniColab.Api.Entities;
using UniColab.Api.Repositories;
using UniColab.Api.Seguranca;

namespace UniColab.Api.Services;

public interface IForumServico
{
    Task<List<ComentarioDto>> ListarAsync(long questaoId, CancellationToken ct);
    Task<ComentarioDto> ComentarAsync(long questaoId, ComentarioRequest pedido, CancellationToken ct);
    Task ReportarAsync(long comentarioId, ReporteRequest pedido, CancellationToken ct);
    Task ModerarAsync(long comentarioId, string status, CancellationToken ct);
    Task<List<FilaItemDto>> FilaAsync(CancellationToken ct);
    Task JulgarAsync(long reporteId, string status, CancellationToken ct);
    Task<List<ChamadoDto>> ChamadosAsync(CancellationToken ct);
    Task<ChamadoDto> AbrirChamadoAsync(ChamadoRequest pedido, CancellationToken ct);
}

public sealed class ForumServico : IForumServico
{
    private static readonly string[] Motivos = ["ofensa", "spam", "denuncia", "outro"];
    private static readonly string[] StatusModeracao = ["oculto", "removido"];
    private static readonly string[] StatusJulgamento = ["procedente", "improcedente"];

    private readonly IComunidadeRepositorio _comunidade;
    private readonly IAcervoRepositorio _acervo;
    private readonly IUnidadeDeTrabalho _unidade;
    private readonly IUsuarioAtual _atual;

    public ForumServico(
        IComunidadeRepositorio comunidade,
        IAcervoRepositorio acervo,
        IUnidadeDeTrabalho unidade,
        IUsuarioAtual atual)
    {
        _comunidade = comunidade;
        _acervo = acervo;
        _unidade = unidade;
        _atual = atual;
    }

    public async Task<List<ComentarioDto>> ListarAsync(long questaoId, CancellationToken ct)
    {
        await ExigirQuestaoPublicaAsync(questaoId, ct);
        var verModerados = _atual.Papel == Papeis.Administrador;
        return await _comunidade.ListarComentariosAsync(_atual.InstituicaoId, questaoId, verModerados, ct);
    }

    public async Task<ComentarioDto> ComentarAsync(long questaoId, ComentarioRequest pedido, CancellationToken ct)
    {
        if (_atual.Papel is not (Papeis.Estudante or Papeis.Professor))
        {
            throw new AcessoNegadoException("Somente estudante ou professor comenta.");
        }

        await ExigirQuestaoPublicaAsync(questaoId, ct);
        if (pedido.ComentarioPaiId is long paiId)
        {
            var pai = await _comunidade.ObterComentarioAsync(_atual.InstituicaoId, paiId, ct)
                ?? throw new RegraNegocioException("Comentário pai não encontrado.");
            if (pai.QuestaoId != questaoId)
            {
                throw new RegraNegocioException("A resposta precisa ficar na mesma questão.");
            }
        }

        var comentario = new ComentarioForum
        {
            InstituicaoId = _atual.InstituicaoId,
            QuestaoId = questaoId,
            UsuarioId = _atual.Id,
            ComentarioPaiId = pedido.ComentarioPaiId,
            Mensagem = pedido.Mensagem.Trim(),
            Status = "publicado"
        };
        _comunidade.Adicionar(comentario);
        await _unidade.SalvarAsync(ct);
        await _unidade.RecarregarAsync(comentario, ct);
        return new ComentarioDto
        {
            Id = comentario.Id,
            ComentarioPaiId = comentario.ComentarioPaiId,
            UsuarioId = _atual.Id,
            Autor = _atual.Nome,
            Papel = _atual.Papel,
            Mensagem = comentario.Mensagem,
            Status = comentario.Status,
            CriadoEm = comentario.CriadoEm
        };
    }

    public async Task ReportarAsync(long comentarioId, ReporteRequest pedido, CancellationToken ct)
    {
        var motivo = pedido.Motivo.Trim().ToLowerInvariant();
        if (!Motivos.Contains(motivo))
        {
            throw new RegraNegocioException("Motivo é ofensa, spam, denuncia ou outro.");
        }

        var comentario = await _comunidade.ObterComentarioAsync(_atual.InstituicaoId, comentarioId, ct)
            ?? throw new NaoEncontradoException("Comentário não encontrado.");
        if (comentario.Status != "publicado")
        {
            throw new RegraNegocioException("Só um comentário publicado entra na fila.");
        }

        if (comentario.UsuarioId == _atual.Id)
        {
            throw new RegraNegocioException("O autor não reporta o próprio comentário.");
        }

        _comunidade.Adicionar(new ForumReporte
        {
            InstituicaoId = _atual.InstituicaoId,
            ComentarioId = comentario.Id,
            QuestaoId = comentario.QuestaoId,
            ReportadoPorId = _atual.Id,
            Motivo = motivo,
            Detalhe = string.IsNullOrWhiteSpace(pedido.Detalhe) ? null : pedido.Detalhe.Trim(),
            Status = "aberto"
        });
        await _unidade.SalvarAsync(ct);
    }

    public async Task ModerarAsync(long comentarioId, string status, CancellationToken ct)
    {
        if (_atual.Papel != Papeis.Administrador)
        {
            throw new AcessoNegadoException("Só o administrador modera o fórum.");
        }

        var destino = status.Trim().ToLowerInvariant();
        if (!StatusModeracao.Contains(destino))
        {
            throw new RegraNegocioException("Moderação usa o status oculto ou removido.");
        }

        var comentario = await _comunidade.ObterComentarioAsync(_atual.InstituicaoId, comentarioId, ct)
            ?? throw new NaoEncontradoException("Comentário não encontrado.");
        comentario.Status = destino;
        comentario.ModeradoPorId = _atual.Id;
        comentario.ModeradoEm = DateTime.Now;
        await using var tx = await _unidade.IniciarTransacaoAsync(ct);
        await _unidade.DefinirAtorAsync(_atual.Id, ct);
        await _unidade.SalvarAsync(ct);
        await tx.CommitAsync(ct);
    }

    public Task<List<FilaItemDto>> FilaAsync(CancellationToken ct)
    {
        if (_atual.Papel != Papeis.Administrador)
        {
            throw new AcessoNegadoException("Só o administrador vê a fila de moderação.");
        }

        return _comunidade.FilaAsync(_atual.InstituicaoId, ct);
    }

    public async Task JulgarAsync(long reporteId, string status, CancellationToken ct)
    {
        if (_atual.Papel != Papeis.Administrador)
        {
            throw new AcessoNegadoException("Só o administrador julga um reporte.");
        }

        var destino = status.Trim().ToLowerInvariant();
        if (!StatusJulgamento.Contains(destino))
        {
            throw new RegraNegocioException("Julgamento usa o status procedente ou improcedente.");
        }

        var reporte = await _comunidade.ObterReporteAsync(_atual.InstituicaoId, reporteId, ct)
            ?? throw new NaoEncontradoException("Reporte não encontrado.");
        if (reporte.Status != "aberto")
        {
            throw new RegraNegocioException("Reporte já julgado.");
        }

        reporte.Status = destino;
        reporte.AnalisadoPorId = _atual.Id;
        reporte.AnalisadoEm = DateTime.Now;
        await using var tx = await _unidade.IniciarTransacaoAsync(ct);
        await _unidade.DefinirAtorAsync(_atual.Id, ct);
        await _unidade.SalvarAsync(ct);
        await tx.CommitAsync(ct);
    }

    public Task<List<ChamadoDto>> ChamadosAsync(CancellationToken ct)
    {
        long? autor = _atual.Papel == Papeis.Administrador ? null : _atual.Id;
        return _comunidade.ListarChamadosAsync(_atual.InstituicaoId, autor, ct);
    }

    public async Task<ChamadoDto> AbrirChamadoAsync(ChamadoRequest pedido, CancellationToken ct)
    {
        var chamado = new ChamadoTi
        {
            InstituicaoId = _atual.InstituicaoId,
            AbertoPorId = _atual.Id,
            Assunto = pedido.Assunto.Trim(),
            Descricao = pedido.Descricao.Trim(),
            Status = "aberto"
        };
        _comunidade.Adicionar(chamado);
        await _unidade.SalvarAsync(ct);
        await _unidade.RecarregarAsync(chamado, ct);
        return new ChamadoDto
        {
            Id = chamado.Id,
            Assunto = chamado.Assunto,
            Descricao = chamado.Descricao,
            Status = chamado.Status,
            CriadoEm = chamado.CriadoEm
        };
    }

    private async Task ExigirQuestaoPublicaAsync(long questaoId, CancellationToken ct)
    {
        var questao = await _acervo.ObterQuestaoAsync(_atual.InstituicaoId, questaoId, ct)
            ?? throw new NaoEncontradoException("Questão não encontrada.");
        if (questao.Status != "publicada" && _atual.Papel is not (Papeis.Professor or Papeis.Administrador))
        {
            throw new NaoEncontradoException("Questão não encontrada.");
        }

        if (questao.Status != "publicada" && _atual.Papel == Papeis.Professor && questao.CriadoPorId != _atual.Id)
        {
            throw new NaoEncontradoException("Questão não encontrada.");
        }
    }
}
