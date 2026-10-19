using UniColab.Api.Dtos;
using UniColab.Api.Entities;
using UniColab.Api.Repositories;
using UniColab.Api.Seguranca;

namespace UniColab.Api.Services;

public interface ITentativaServico
{
    Task<TentativaDto> IniciarAsync(long simuladoId, CancellationToken ct);
    Task<List<TentativaResumo>> MinhasAsync(CancellationToken ct);
    Task<TentativaDto> ObterAsync(long id, CancellationToken ct);
    Task<FolhaItemDto> RegistrarAsync(long id, RegistrarRespostaRequest pedido, CancellationToken ct);
    Task<TentativaDto> FinalizarAsync(long id, CancellationToken ct);
    Task ExpirarAsync(CancellationToken ct);
}

public sealed class TentativaServico : ITentativaServico
{
    private readonly IAvaliacaoRepositorio _avaliacao;
    private readonly IAcervoRepositorio _acervo;
    private readonly IProcedimentoRepositorio _procedimentos;
    private readonly IUnidadeDeTrabalho _unidade;
    private readonly IUsuarioAtual _atual;

    public TentativaServico(
        IAvaliacaoRepositorio avaliacao,
        IAcervoRepositorio acervo,
        IProcedimentoRepositorio procedimentos,
        IUnidadeDeTrabalho unidade,
        IUsuarioAtual atual)
    {
        _avaliacao = avaliacao;
        _acervo = acervo;
        _procedimentos = procedimentos;
        _unidade = unidade;
        _atual = atual;
    }

    public async Task<TentativaDto> IniciarAsync(long simuladoId, CancellationToken ct)
    {
        if (_atual.Papel != Papeis.Estudante)
        {
            throw new AcessoNegadoException("Só o estudante inicia simulado.");
        }

        var simulado = await _avaliacao.ObterSimuladoAsync(_atual.InstituicaoId, simuladoId, ct)
            ?? throw new NaoEncontradoException("Simulado não encontrado.");
        if (!simulado.Ativo)
        {
            throw new RegraNegocioException("Simulado inativo.");
        }

        if (simulado.CursoId is long curso && curso != _atual.CursoId)
        {
            throw new NaoEncontradoException("Simulado não encontrado.");
        }

        var tentativaId = await _procedimentos.IniciarTentativaAsync(_atual.Id, simulado.Id, ct);
        var tentativa = await _avaliacao.ObterTentativaAsync(_atual.InstituicaoId, tentativaId, ct)
            ?? throw new NaoEncontradoException("Tentativa não encontrada.");
        return await MontarAsync(tentativa, false, ct);
    }

    public async Task<List<TentativaResumo>> MinhasAsync(CancellationToken ct)
    {
        if (_atual.Papel != Papeis.Estudante)
        {
            throw new AcessoNegadoException("O histórico de tentativas é do estudante.");
        }

        var tentativas = await _avaliacao.ListarTentativasAsync(_atual.Id, ct);
        var resumos = new List<TentativaResumo>();
        foreach (var tentativa in tentativas)
        {
            resumos.Add(new TentativaResumo
            {
                Id = tentativa.Id,
                Modo = tentativa.Modo,
                Titulo = await _avaliacao.TituloSimuladoAsync(tentativa.SimuladoId, ct),
                Status = tentativa.Status,
                IniciadoEm = tentativa.IniciadoEm,
                FinalizadoEm = tentativa.FinalizadoEm,
                NotaTotal = tentativa.NotaTotal
            });
        }

        return resumos;
    }

    public async Task<TentativaDto> ObterAsync(long id, CancellationToken ct)
    {
        var tentativa = await ExigirDonoAsync(id, ct);
        return await MontarAsync(tentativa, tentativa.Status != "em_andamento", ct);
    }

    public async Task<FolhaItemDto> RegistrarAsync(long id, RegistrarRespostaRequest pedido, CancellationToken ct)
    {
        var tentativa = await ExigirDonoAsync(id, ct);
        if (tentativa.Status != "em_andamento")
        {
            throw new RegraNegocioException("Tentativa já encerrada.");
        }

        await _procedimentos.RegistrarRespostaAsync(
            tentativa.Id,
            pedido.QuestaoId,
            pedido.AlternativaId,
            pedido.MarcadaRevisao,
            Math.Max(pedido.TempoSegundos, 0),
            ct);

        var folha = await _avaliacao.FolhaAsync(tentativa.Id, ct);
        var item = folha.FirstOrDefault(f => f.QuestaoId == pedido.QuestaoId)
            ?? throw new NaoEncontradoException("Questão não pertence a esta tentativa.");
        return await MontarItemAsync(item, false, ct);
    }

    public async Task<TentativaDto> FinalizarAsync(long id, CancellationToken ct)
    {
        var tentativa = await ExigirDonoAsync(id, ct);
        await _procedimentos.FinalizarTentativaAsync(tentativa.Id, "manual", ct);
        await _unidade.RecarregarAsync(tentativa, ct);
        return await MontarAsync(tentativa, true, ct);
    }

    public Task ExpirarAsync(CancellationToken ct)
    {
        if (_atual.Papel != Papeis.Administrador)
        {
            throw new AcessoNegadoException("Só o administrador dispara a expiração.");
        }

        return _procedimentos.ExpirarTentativasAsync(ct);
    }

    private async Task<Tentativa> ExigirDonoAsync(long id, CancellationToken ct)
    {
        var tentativa = await _avaliacao.ObterTentativaAsync(_atual.InstituicaoId, id, ct);
        if (tentativa is null || tentativa.UsuarioId != _atual.Id)
        {
            throw new NaoEncontradoException("Tentativa não encontrada.");
        }

        return tentativa;
    }

    private async Task<TentativaDto> MontarAsync(Tentativa tentativa, bool gabarito, CancellationToken ct)
    {
        var folha = await _avaliacao.FolhaAsync(tentativa.Id, ct);
        var itens = new List<FolhaItemDto>();
        foreach (var item in folha)
        {
            itens.Add(await MontarItemAsync(item, gabarito, ct));
        }

        return new TentativaDto
        {
            Id = tentativa.Id,
            Modo = tentativa.Modo,
            SimuladoId = tentativa.SimuladoId,
            SimuladoTitulo = await _avaliacao.TituloSimuladoAsync(tentativa.SimuladoId, ct),
            Status = tentativa.Status,
            IniciadoEm = tentativa.IniciadoEm,
            LimiteEm = tentativa.LimiteEm,
            FinalizadoEm = tentativa.FinalizadoEm,
            TempoGastoSegundos = tentativa.TempoGastoSegundos,
            NotaTotal = gabarito ? tentativa.NotaTotal : null,
            Itens = itens
        };
    }

    private async Task<FolhaItemDto> MontarItemAsync(TentativaQuestao item, bool gabarito, CancellationToken ct)
    {
        var questao = await _acervo.ObterQuestaoAsync(_atual.InstituicaoId, item.QuestaoId, ct);
        var alternativas = await _acervo.AlternativasAsync(item.QuestaoId, ct);
        var justificativas = gabarito ? await _acervo.JustificativasAsync(item.QuestaoId, ct) : [];
        return new FolhaItemDto
        {
            Ordem = item.Ordem,
            QuestaoId = item.QuestaoId,
            Enunciado = questao?.Enunciado ?? "",
            AlternativaSelecionadaId = item.AlternativaSelecionadaId,
            MarcadaRevisao = item.MarcadaRevisao,
            Acertou = gabarito ? item.Acertou : null,
            TempoRespostaSegundos = item.TempoRespostaSegundos,
            Alternativas = alternativas.Select(a => new AlternativaDto
            {
                Id = a.Id,
                Letra = a.Letra,
                Ordem = a.Ordem,
                Texto = a.Texto,
                EhCorreta = gabarito ? a.EhCorreta : null,
                Explicacao = gabarito ? justificativas.FirstOrDefault(j => j.AlternativaId == a.Id)?.Explicacao : null
            }).ToList()
        };
    }
}
