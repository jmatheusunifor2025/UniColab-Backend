using UniColab.Api.Repositories;

namespace UniColab.Api.Services;

public sealed class ExpiracaoDeTentativas : BackgroundService
{
    private readonly IServiceScopeFactory _escopos;
    private readonly ILogger<ExpiracaoDeTentativas> _logger;

    public ExpiracaoDeTentativas(IServiceScopeFactory escopos, ILogger<ExpiracaoDeTentativas> logger)
    {
        _escopos = escopos;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var ciclos = 0;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                ciclos++;
                using var escopo = _escopos.CreateScope();
                var procedimentos = escopo.ServiceProvider.GetRequiredService<IProcedimentoRepositorio>();
                await procedimentos.ExpirarTentativasAsync(stoppingToken);
                if (ciclos % 5 == 0)
                {
                    await procedimentos.ReconciliarMetricasAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao expirar tentativas ou reconciliar métricas.");
            }
        }
    }
}
