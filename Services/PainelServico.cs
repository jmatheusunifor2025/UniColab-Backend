using UniColab.Api.Dtos;
using UniColab.Api.Entities;
using UniColab.Api.Repositories;
using UniColab.Api.Seguranca;

namespace UniColab.Api.Services;

public interface IPainelServico
{
    Task<List<MetricaDto>> DashboardAsync(CancellationToken ct);
    Task<List<BadgeDto>> BadgesAsync(CancellationToken ct);
    Task<List<TrilhaDto>> TrilhasAsync(CancellationToken ct);
    Task<TrilhaDto> SugerirTrilhaAsync(CancellationToken ct);
    Task<List<HeatmapLinha>> HeatmapAsync(long? cursoId, CancellationToken ct);
    Task<List<AdesaoLinha>> AdesaoAsync(long? cursoId, CancellationToken ct);
    Task<List<LancamentoDto>> ExtratoAsync(CancellationToken ct);
    Task<SaldoDto> SaldoAsync(CancellationToken ct);
}

public sealed class PainelServico : IPainelServico
{
    private readonly IPainelRepositorio _painel;
    private readonly IProcedimentoRepositorio _procedimentos;
    private readonly IUsuarioAtual _atual;

    public PainelServico(IPainelRepositorio painel, IProcedimentoRepositorio procedimentos, IUsuarioAtual atual)
    {
        _painel = painel;
        _procedimentos = procedimentos;
        _atual = atual;
    }

    public Task<List<MetricaDto>> DashboardAsync(CancellationToken ct)
    {
        ExigirEstudante("O dashboard é pessoal do estudante.");
        return _painel.MetricasAsync(_atual.Id, ct);
    }

    public Task<List<BadgeDto>> BadgesAsync(CancellationToken ct)
    {
        ExigirEstudante("As badges são do estudante.");
        return _painel.BadgesAsync(_atual.Id, ct);
    }

    public Task<List<TrilhaDto>> TrilhasAsync(CancellationToken ct)
    {
        ExigirEstudante("A trilha é do estudante.");
        return _painel.TrilhasAsync(_atual.Id, ct);
    }

    public async Task<TrilhaDto> SugerirTrilhaAsync(CancellationToken ct)
    {
        ExigirEstudante("A trilha é do estudante.");
        await _procedimentos.SugerirTrilhaAsync(_atual.Id, ct);
        return await _painel.UltimaTrilhaAsync(_atual.Id, ct)
            ?? throw new NaoEncontradoException("Trilha não encontrada.");
    }

    public Task<List<HeatmapLinha>> HeatmapAsync(long? cursoId, CancellationToken ct) =>
        _painel.HeatmapAsync(_atual.InstituicaoId, CursoDoRelatorio(cursoId), ct);

    public Task<List<AdesaoLinha>> AdesaoAsync(long? cursoId, CancellationToken ct) =>
        _painel.AdesaoAsync(_atual.InstituicaoId, CursoDoRelatorio(cursoId), ct);

    public Task<List<LancamentoDto>> ExtratoAsync(CancellationToken ct)
    {
        if (_atual.Papel == Papeis.Professor)
        {
            return _painel.ExtratoAsync(_atual.InstituicaoId, _atual.Id, ct);
        }

        if (_atual.Papel == Papeis.Administrador)
        {
            return _painel.ExtratoAsync(_atual.InstituicaoId, null, ct);
        }

        throw new AcessoNegadoException("O extrato docente fica com o professor e o administrador.");
    }

    public async Task<SaldoDto> SaldoAsync(CancellationToken ct)
    {
        if (_atual.Papel == Papeis.Professor)
        {
            return await _painel.SaldoAsync(_atual.Id, ct);
        }

        if (_atual.Papel == Papeis.Administrador)
        {
            var extrato = await _painel.ExtratoAsync(_atual.InstituicaoId, null, ct);
            return new SaldoDto { Lancamentos = extrato.Count, Saldo = extrato.Sum(l => l.Valor) };
        }

        throw new AcessoNegadoException("O extrato docente fica com o professor e o administrador.");
    }

    private void ExigirEstudante(string mensagem)
    {
        if (_atual.Papel != Papeis.Estudante)
        {
            throw new AcessoNegadoException(mensagem);
        }
    }

    private long? CursoDoRelatorio(long? cursoId)
    {
        if (_atual.Papel == Papeis.Coordenador)
        {
            if (_atual.CursoId is not long curso)
            {
                throw new AcessoNegadoException("Coordenador sem curso vinculado.");
            }

            if (cursoId is long pedido && pedido != curso)
            {
                throw new AcessoNegadoException("A coordenação vê somente o próprio curso.");
            }

            return curso;
        }

        if (_atual.Papel == Papeis.Administrador)
        {
            return cursoId;
        }

        throw new AcessoNegadoException("Relatório disponível para coordenação e administração.");
    }
}
