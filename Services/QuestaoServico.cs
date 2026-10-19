using Microsoft.EntityFrameworkCore;
using UniColab.Api.Dtos;
using UniColab.Api.Entities;
using UniColab.Api.Repositories;
using UniColab.Api.Seguranca;

namespace UniColab.Api.Services;

public interface IQuestaoServico
{
    Task<List<AreaDto>> AreasAsync(CancellationToken ct);
    Task<Pagina<QuestaoListaItem>> ListarAsync(int? ano, short? areaId, string? dificuldade, string? status, int pagina, int tamanho, CancellationToken ct);
    Task<QuestaoDetalhe> ObterAsync(long id, CancellationToken ct);
    Task<QuestaoDetalhe> CriarAsync(SalvarQuestaoRequest pedido, CancellationToken ct);
    Task<QuestaoDetalhe> AtualizarAsync(long id, SalvarQuestaoRequest pedido, CancellationToken ct);
    Task PublicarAsync(long id, CancellationToken ct);
    Task ReabrirAsync(long id, CancellationToken ct);
    Task RetirarAsync(long id, string motivo, CancellationToken ct);
    Task<AlternativaDto> SalvarAlternativaAsync(long questaoId, long? alternativaId, SalvarAlternativaRequest pedido, CancellationToken ct);
    Task SalvarJustificativaAsync(long questaoId, long alternativaId, string explicacao, CancellationToken ct);
    Task<ResultadoResposta> ResponderAsync(long questaoId, ResponderQuestaoRequest pedido, CancellationToken ct);
}

public sealed class QuestaoServico : IQuestaoServico
{
    private static readonly string[] Dificuldades = ["facil", "medio", "dificil"];
    private static readonly string[] Letras = ["A", "B", "C", "D", "E"];

    private readonly IAcervoRepositorio _acervo;
    private readonly IAvaliacaoRepositorio _avaliacao;
    private readonly IProcedimentoRepositorio _procedimentos;
    private readonly IUnidadeDeTrabalho _unidade;
    private readonly IUsuarioAtual _atual;

    public QuestaoServico(
        IAcervoRepositorio acervo,
        IAvaliacaoRepositorio avaliacao,
        IProcedimentoRepositorio procedimentos,
        IUnidadeDeTrabalho unidade,
        IUsuarioAtual atual)
    {
        _acervo = acervo;
        _avaliacao = avaliacao;
        _procedimentos = procedimentos;
        _unidade = unidade;
        _atual = atual;
    }

    public async Task<List<AreaDto>> AreasAsync(CancellationToken ct)
    {
        var areas = await _acervo.ListarAreasAsync(ct);
        return areas.Select(a => new AreaDto { Id = a.Id, Nome = a.Nome, Slug = a.Slug, Componente = a.Componente }).ToList();
    }

    public async Task<Pagina<QuestaoListaItem>> ListarAsync(int? ano, short? areaId, string? dificuldade, string? status, int pagina, int tamanho, CancellationToken ct)
    {
        (pagina, tamanho) = Normalizar(pagina, tamanho);
        var consulta = _acervo.Questoes(_atual.InstituicaoId);
        if (_atual.Papel == Papeis.Estudante || _atual.Papel == Papeis.Coordenador)
        {
            consulta = consulta.Where(q => q.Status == "publicada");
        }
        else if (!string.IsNullOrWhiteSpace(status))
        {
            consulta = consulta.Where(q => q.Status == status);
        }

        if (ano is int anoFiltro)
        {
            consulta = consulta.Where(q => q.Ano == anoFiltro);
        }

        if (areaId is short area)
        {
            consulta = consulta.Where(q => q.AreaId == area);
        }

        if (!string.IsNullOrWhiteSpace(dificuldade))
        {
            consulta = consulta.Where(q => q.Dificuldade == dificuldade);
        }

        var total = await consulta.CountAsync(ct);
        var itens = await consulta
            .OrderByDescending(q => q.Ano)
            .ThenBy(q => q.Id)
            .Skip((pagina - 1) * tamanho)
            .Take(tamanho)
            .Select(q => new QuestaoListaItem
            {
                Id = q.Id,
                Ano = q.Ano,
                AreaId = q.AreaId,
                Dificuldade = q.Dificuldade,
                Tipo = q.Tipo,
                Status = q.Status,
                Enunciado = q.Enunciado
            })
            .ToListAsync(ct);

        foreach (var item in itens)
        {
            item.Area = await _acervo.NomeAreaAsync(item.AreaId, ct) ?? "";
        }

        return new Pagina<QuestaoListaItem> { PaginaAtual = pagina, Tamanho = tamanho, Total = total, Itens = itens };
    }

    public async Task<QuestaoDetalhe> ObterAsync(long id, CancellationToken ct)
    {
        var questao = await ExigirQuestaoAsync(id, ct);
        if (!PodeVerGabarito() && questao.Status != "publicada")
        {
            throw new NaoEncontradoException("Questão não encontrada.");
        }

        return await DetalharAsync(questao, PodeVerGabarito(), ct);
    }

    public async Task<QuestaoDetalhe> CriarAsync(SalvarQuestaoRequest pedido, CancellationToken ct)
    {
        ExigirProfessor();
        await ValidarPedidoAsync(pedido, ct);
        var questao = new Questao
        {
            InstituicaoId = _atual.InstituicaoId,
            CriadoPorId = _atual.Id,
            Enunciado = pedido.Enunciado.Trim(),
            Ano = pedido.Ano,
            AreaId = pedido.AreaId,
            Dificuldade = pedido.Dificuldade,
            Tipo = string.IsNullOrWhiteSpace(pedido.Tipo) ? "multipla_escolha" : pedido.Tipo,
            Origem = "institucional",
            ProvaId = pedido.ProvaId,
            CodigoItem = pedido.CodigoItem,
            PadraoResposta = pedido.PadraoResposta,
            Status = "rascunho"
        };
        _acervo.Adicionar(questao);
        await _unidade.SalvarAsync(ct);
        return await DetalharAsync(questao, true, ct);
    }

    public async Task<QuestaoDetalhe> AtualizarAsync(long id, SalvarQuestaoRequest pedido, CancellationToken ct)
    {
        var questao = await ExigirAutoriaAsync(id, ct);
        if (questao.Status is "publicada" or "retirada")
        {
            throw new RegraNegocioException("Reabra a questão como em revisão antes de editar o enunciado.");
        }

        await ValidarPedidoAsync(pedido, ct);
        questao.Enunciado = pedido.Enunciado.Trim();
        questao.Ano = pedido.Ano;
        questao.AreaId = pedido.AreaId;
        questao.Dificuldade = pedido.Dificuldade;
        questao.Tipo = pedido.Tipo;
        questao.ProvaId = pedido.ProvaId;
        questao.CodigoItem = pedido.CodigoItem;
        questao.PadraoResposta = pedido.PadraoResposta;
        await _unidade.SalvarAsync(ct);
        return await DetalharAsync(questao, true, ct);
    }

    public async Task PublicarAsync(long id, CancellationToken ct)
    {
        var questao = await ExigirAutoriaAsync(id, ct);
        questao.Status = "publicada";
        await _unidade.SalvarAsync(ct);
    }

    public async Task ReabrirAsync(long id, CancellationToken ct)
    {
        var questao = await ExigirAutoriaAsync(id, ct);
        questao.Status = "em_revisao";
        await _unidade.SalvarAsync(ct);
    }

    public async Task RetirarAsync(long id, string motivo, CancellationToken ct)
    {
        if (_atual.Papel != Papeis.Administrador)
        {
            throw new AcessoNegadoException("Só o administrador retira questão do acervo.");
        }

        var questao = await ExigirQuestaoAsync(id, ct);
        questao.Status = "retirada";
        questao.MotivoRetirada = motivo.Trim();
        questao.RetiradaPorId = _atual.Id;
        questao.RetiradaEm = DateTime.Now;
        await using var tx = await _unidade.IniciarTransacaoAsync(ct);
        await _unidade.DefinirAtorAsync(_atual.Id, ct);
        await _unidade.SalvarAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task<AlternativaDto> SalvarAlternativaAsync(long questaoId, long? alternativaId, SalvarAlternativaRequest pedido, CancellationToken ct)
    {
        var questao = await ExigirAutoriaAsync(questaoId, ct);
        if (questao.Status is "publicada" or "retirada")
        {
            throw new RegraNegocioException("Alternativa de questão publicada ou retirada não muda.");
        }

        var letra = pedido.Letra.Trim().ToUpperInvariant();
        if (!Letras.Contains(letra))
        {
            throw new RegraNegocioException("A letra da alternativa é A, B, C, D ou E.");
        }

        Alternativa alternativa;
        if (alternativaId is long id)
        {
            alternativa = await _acervo.ObterAlternativaAsync(questaoId, id, ct)
                ?? throw new NaoEncontradoException("Alternativa não encontrada.");
            alternativa.Letra = letra;
            alternativa.Ordem = pedido.Ordem;
            alternativa.Texto = pedido.Texto.Trim();
            alternativa.EhCorreta = pedido.EhCorreta;
        }
        else
        {
            alternativa = new Alternativa
            {
                QuestaoId = questao.Id,
                InstituicaoId = questao.InstituicaoId,
                Letra = letra,
                Ordem = pedido.Ordem,
                Texto = pedido.Texto.Trim(),
                EhCorreta = pedido.EhCorreta
            };
            _acervo.Adicionar(alternativa);
        }

        await _unidade.SalvarAsync(ct);
        return new AlternativaDto
        {
            Id = alternativa.Id,
            Letra = alternativa.Letra,
            Ordem = alternativa.Ordem,
            Texto = alternativa.Texto,
            EhCorreta = alternativa.EhCorreta
        };
    }

    public async Task SalvarJustificativaAsync(long questaoId, long alternativaId, string explicacao, CancellationToken ct)
    {
        _ = await ExigirAutoriaAsync(questaoId, ct);
        var alternativa = await _acervo.ObterAlternativaAsync(questaoId, alternativaId, ct)
            ?? throw new NaoEncontradoException("Alternativa não encontrada.");
        var justificativa = await _acervo.ObterJustificativaAsync(alternativa.Id, ct);
        if (justificativa is null)
        {
            _acervo.Adicionar(new Justificativa
            {
                AlternativaId = alternativa.Id,
                InstituicaoId = _atual.InstituicaoId,
                ProfessorId = _atual.Id,
                Explicacao = explicacao.Trim(),
                Validada = true
            });
        }
        else
        {
            if (justificativa.ProfessorId != _atual.Id)
            {
                throw new AcessoNegadoException("A justificativa permanece com o professor que a escreveu.");
            }

            justificativa.Explicacao = explicacao.Trim();
        }

        await _unidade.SalvarAsync(ct);
    }

    public async Task<ResultadoResposta> ResponderAsync(long questaoId, ResponderQuestaoRequest pedido, CancellationToken ct)
    {
        if (_atual.Papel != Papeis.Estudante)
        {
            throw new AcessoNegadoException("Só o estudante responde questão avulsa.");
        }

        var questao = await ExigirQuestaoAsync(questaoId, ct);
        if (questao.Status != "publicada")
        {
            throw new RegraNegocioException("A questão ainda não está publicada.");
        }

        var tentativa = new Tentativa
        {
            InstituicaoId = _atual.InstituicaoId,
            UsuarioId = _atual.Id,
            Modo = "pratica",
            IniciadoEm = DateTime.Now,
            Status = "em_andamento"
        };

        await using var tx = await _unidade.IniciarTransacaoAsync(ct);
        _avaliacao.Adicionar(tentativa);
        await _unidade.SalvarAsync(ct);
        _avaliacao.Adicionar(new TentativaQuestao
        {
            TentativaId = tentativa.Id,
            QuestaoId = questao.Id,
            InstituicaoId = _atual.InstituicaoId,
            Ordem = 1,
            AlternativaSelecionadaId = pedido.AlternativaId,
            TempoRespostaSegundos = Math.Max(pedido.TempoSegundos, 0)
        });
        await _unidade.SalvarAsync(ct);
        tentativa.Status = "finalizado";
        tentativa.FinalizadoEm = DateTime.Now;
        await _unidade.SalvarAsync(ct);
        await _procedimentos.AtualizarMetricasAsync(_atual.Id, ct);
        await _procedimentos.ConcederBadgesAsync(_atual.Id, ct);
        await tx.CommitAsync(ct);

        await _unidade.RecarregarAsync(tentativa, ct);
        var detalhe = await DetalharAsync(questao, true, ct);
        var folha = await _avaliacao.FolhaAsync(tentativa.Id, ct);
        return new ResultadoResposta
        {
            TentativaId = tentativa.Id,
            Acertou = folha.FirstOrDefault()?.Acertou ?? false,
            Nota = tentativa.NotaTotal,
            LetraCorreta = detalhe.Alternativas.FirstOrDefault(a => a.EhCorreta == true)?.Letra ?? "",
            Alternativas = detalhe.Alternativas
        };
    }

    private bool PodeVerGabarito() => _atual.Papel is Papeis.Professor or Papeis.Administrador;

    private void ExigirProfessor()
    {
        if (_atual.Papel != Papeis.Professor)
        {
            throw new AcessoNegadoException("Só o professor edita o acervo.");
        }
    }

    private async Task<Questao> ExigirAutoriaAsync(long id, CancellationToken ct)
    {
        ExigirProfessor();
        var questao = await ExigirQuestaoAsync(id, ct);
        if (questao.CriadoPorId != _atual.Id)
        {
            throw new AcessoNegadoException("A questão pertence a outro professor.");
        }

        return questao;
    }

    private async Task<Questao> ExigirQuestaoAsync(long id, CancellationToken ct) =>
        await _acervo.ObterQuestaoAsync(_atual.InstituicaoId, id, ct)
        ?? throw new NaoEncontradoException("Questão não encontrada.");

    private async Task ValidarPedidoAsync(SalvarQuestaoRequest pedido, CancellationToken ct)
    {
        if (!Dificuldades.Contains(pedido.Dificuldade))
        {
            throw new RegraNegocioException("Dificuldade é facil, medio ou dificil.");
        }

        if (!await _acervo.AreaExisteAsync(pedido.AreaId, ct))
        {
            throw new RegraNegocioException("Área de conhecimento não encontrada.");
        }
    }

    private async Task<QuestaoDetalhe> DetalharAsync(Questao questao, bool gabarito, CancellationToken ct)
    {
        var alternativas = await _acervo.AlternativasAsync(questao.Id, ct);
        var justificativas = gabarito
            ? await _acervo.JustificativasAsync(questao.Id, ct)
            : [];
        return new QuestaoDetalhe
        {
            Id = questao.Id,
            Ano = questao.Ano,
            AreaId = questao.AreaId,
            Area = await _acervo.NomeAreaAsync(questao.AreaId, ct) ?? "",
            Dificuldade = questao.Dificuldade,
            Tipo = questao.Tipo,
            Status = questao.Status,
            Origem = questao.Origem,
            Enunciado = questao.Enunciado,
            PadraoResposta = gabarito ? questao.PadraoResposta : null,
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

    private static (int pagina, int tamanho) Normalizar(int pagina, int tamanho)
    {
        if (pagina < 1) pagina = 1;
        if (tamanho < 1) tamanho = 20;
        if (tamanho > 50) tamanho = 50;
        return (pagina, tamanho);
    }
}
