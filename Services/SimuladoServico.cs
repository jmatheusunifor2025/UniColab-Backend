using UniColab.Api.Dtos;
using UniColab.Api.Entities;
using UniColab.Api.Repositories;
using UniColab.Api.Seguranca;

namespace UniColab.Api.Services;

public interface ISimuladoServico
{
    Task<List<SimuladoDto>> ListarAsync(CancellationToken ct);
    Task<SimuladoDto> ObterAsync(long id, CancellationToken ct);
    Task<SimuladoDto> CriarAsync(CriarSimuladoRequest pedido, CancellationToken ct);
    Task<SimuladoDto> DefinirQuestoesAsync(long id, long[] questaoIds, CancellationToken ct);
}

public sealed class SimuladoServico : ISimuladoServico
{
    private static readonly string[] Modos = ["fixo", "aleatorio"];
    private static readonly string[] Dificuldades = ["facil", "medio", "dificil"];

    private readonly IAvaliacaoRepositorio _avaliacao;
    private readonly IAcervoRepositorio _acervo;
    private readonly IUsuarioRepositorio _usuarios;
    private readonly IUnidadeDeTrabalho _unidade;
    private readonly IUsuarioAtual _atual;

    public SimuladoServico(
        IAvaliacaoRepositorio avaliacao,
        IAcervoRepositorio acervo,
        IUsuarioRepositorio usuarios,
        IUnidadeDeTrabalho unidade,
        IUsuarioAtual atual)
    {
        _avaliacao = avaliacao;
        _acervo = acervo;
        _usuarios = usuarios;
        _unidade = unidade;
        _atual = atual;
    }

    public async Task<List<SimuladoDto>> ListarAsync(CancellationToken ct)
    {
        var restrito = _atual.Papel is Papeis.Estudante or Papeis.Coordenador;
        var lista = await _avaliacao.ListarSimuladosAsync(_atual.InstituicaoId, restrito, ct);
        if (restrito)
        {
            lista = lista.Where(s => s.CursoId is null || s.CursoId == _atual.CursoId).ToList();
        }

        var resultado = new List<SimuladoDto>();
        foreach (var simulado in lista)
        {
            resultado.Add(await MapearAsync(simulado, ct));
        }

        return resultado;
    }

    public async Task<SimuladoDto> ObterAsync(long id, CancellationToken ct)
    {
        var simulado = await ExigirVisivelAsync(id, ct);
        return await MapearAsync(simulado, ct);
    }

    public async Task<SimuladoDto> CriarAsync(CriarSimuladoRequest pedido, CancellationToken ct)
    {
        ExigirBanca();
        var modo = pedido.ModoComposicao.Trim().ToLowerInvariant();
        if (!Modos.Contains(modo))
        {
            throw new RegraNegocioException("Composição do simulado é fixo ou aleatorio.");
        }

        if (pedido.FiltroDificuldade is not null && !Dificuldades.Contains(pedido.FiltroDificuldade))
        {
            throw new RegraNegocioException("Dificuldade do filtro é facil, medio ou dificil.");
        }

        if (pedido.CursoId is long cursoId)
        {
            _ = await _usuarios.ObterCursoAsync(_atual.InstituicaoId, cursoId, ct)
                ?? throw new RegraNegocioException("Curso não pertence à instituição.");
        }

        if (pedido.FiltroAreaId is short areaId && !await _acervo.AreaExisteAsync(areaId, ct))
        {
            throw new RegraNegocioException("Área de conhecimento não encontrada.");
        }

        var simulado = new Simulado
        {
            InstituicaoId = _atual.InstituicaoId,
            CursoId = pedido.CursoId,
            CriadoPorId = _atual.Id,
            Titulo = pedido.Titulo.Trim(),
            Descricao = string.IsNullOrWhiteSpace(pedido.Descricao) ? null : pedido.Descricao.Trim(),
            ModoComposicao = modo,
            TempoLimiteMinutos = pedido.TempoLimiteMinutos,
            QuantidadeQuestoes = pedido.QuantidadeQuestoes,
            FiltroAreaId = pedido.FiltroAreaId,
            FiltroDificuldade = string.IsNullOrWhiteSpace(pedido.FiltroDificuldade) ? null : pedido.FiltroDificuldade,
            FiltroAno = pedido.FiltroAno,
            Ativo = true
        };
        _avaliacao.Adicionar(simulado);
        await _unidade.SalvarAsync(ct);
        return await MapearAsync(simulado, ct);
    }

    public async Task<SimuladoDto> DefinirQuestoesAsync(long id, long[] questaoIds, CancellationToken ct)
    {
        var simulado = await ExigirVisivelAsync(id, ct);
        if (_atual.Papel == Papeis.Professor && simulado.CriadoPorId != _atual.Id)
        {
            throw new AcessoNegadoException("O simulado pertence a outro professor.");
        }

        if (_atual.Papel is not (Papeis.Professor or Papeis.Administrador))
        {
            throw new AcessoNegadoException("Só a banca define a composição do simulado.");
        }

        if (simulado.ModoComposicao != "fixo")
        {
            throw new RegraNegocioException("Simulado aleatório usa o filtro, sem lista fixa.");
        }

        var ids = questaoIds.Distinct().ToArray();
        if (ids.Length == 0 || ids.Length != questaoIds.Length)
        {
            throw new RegraNegocioException("A lista fixa precisa de questões distintas.");
        }

        if (ids.Length > 200)
        {
            throw new RegraNegocioException("O simulado aceita no máximo 200 questões.");
        }

        foreach (var questaoId in ids)
        {
            var questao = await _acervo.ObterQuestaoAsync(_atual.InstituicaoId, questaoId, ct)
                ?? throw new RegraNegocioException("Questão não pertence à instituição.");
            if (questao.Status != "publicada")
            {
                throw new RegraNegocioException("A composição fixa só entra com questão publicada.");
            }
        }

        await using var tx = await _unidade.IniciarTransacaoAsync(ct);
        await _avaliacao.SubstituirQuestoesAsync(simulado.Id, simulado.InstituicaoId, ids, ct);
        await _unidade.SalvarAsync(ct);
        simulado.QuantidadeQuestoes = ids.Length;
        await _unidade.SalvarAsync(ct);
        await tx.CommitAsync(ct);
        return await MapearAsync(simulado, ct);
    }

    private void ExigirBanca()
    {
        if (_atual.Papel is not (Papeis.Professor or Papeis.Administrador))
        {
            throw new AcessoNegadoException("Só a banca cria simulado.");
        }
    }

    private async Task<Simulado> ExigirVisivelAsync(long id, CancellationToken ct)
    {
        var simulado = await _avaliacao.ObterSimuladoAsync(_atual.InstituicaoId, id, ct)
            ?? throw new NaoEncontradoException("Simulado não encontrado.");
        var restrito = _atual.Papel is Papeis.Estudante or Papeis.Coordenador;
        if (restrito && simulado.CursoId is long curso && curso != _atual.CursoId)
        {
            throw new NaoEncontradoException("Simulado não encontrado.");
        }

        if (restrito && !simulado.Ativo)
        {
            throw new NaoEncontradoException("Simulado não encontrado.");
        }

        return simulado;
    }

    private async Task<SimuladoDto> MapearAsync(Simulado simulado, CancellationToken ct) => new()
    {
        Id = simulado.Id,
        Titulo = simulado.Titulo,
        Descricao = simulado.Descricao,
        ModoComposicao = simulado.ModoComposicao,
        TempoLimiteMinutos = simulado.TempoLimiteMinutos,
        QuantidadeQuestoes = simulado.QuantidadeQuestoes,
        CursoId = simulado.CursoId,
        FiltroAreaId = simulado.FiltroAreaId,
        FiltroDificuldade = simulado.FiltroDificuldade,
        FiltroAno = simulado.FiltroAno,
        Ativo = simulado.Ativo,
        QuestaoIds = simulado.ModoComposicao == "fixo"
            ? await _avaliacao.QuestoesDoSimuladoAsync(simulado.Id, ct)
            : []
    };
}
