using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MySqlConnector;
using UniColab.Api.Data;

namespace UniColab.Api.Repositories;

public interface IProcedimentoRepositorio
{
    Task<long> IniciarTentativaAsync(long usuarioId, long simuladoId, CancellationToken ct);
    Task RegistrarRespostaAsync(long tentativaId, long questaoId, long? alternativaId, bool marcadaRevisao, int tempoSegundos, CancellationToken ct);
    Task FinalizarTentativaAsync(long tentativaId, string motivo, CancellationToken ct);
    Task ExpirarTentativasAsync(CancellationToken ct);
    Task ReconciliarMetricasAsync(CancellationToken ct);
    Task AtualizarMetricasAsync(long usuarioId, CancellationToken ct);
    Task ConcederBadgesAsync(long usuarioId, CancellationToken ct);
    Task SugerirTrilhaAsync(long usuarioId, CancellationToken ct);
}

public sealed class ProcedimentoRepositorio : IProcedimentoRepositorio
{
    private readonly UnicolabDbContext _db;

    public ProcedimentoRepositorio(UnicolabDbContext db)
    {
        _db = db;
    }

    public async Task<long> IniciarTentativaAsync(long usuarioId, long simuladoId, CancellationToken ct)
    {
        var conexao = await AbrirAsync(ct);
        await using var comando = conexao.CreateCommand();
        comando.CommandText = "CALL sp_iniciar_tentativa(@usuario, @simulado, @saida); SELECT @saida;";
        comando.Parameters.Add(new MySqlParameter("@usuario", usuarioId));
        comando.Parameters.Add(new MySqlParameter("@simulado", simuladoId));
        var saida = await comando.ExecuteScalarAsync(ct);
        return Convert.ToInt64(saida);
    }

    public Task RegistrarRespostaAsync(long tentativaId, long questaoId, long? alternativaId, bool marcadaRevisao, int tempoSegundos, CancellationToken ct) =>
        ExecutarAsync(
            "sp_registrar_resposta",
            ct,
            new MySqlParameter("p_tentativa_id", tentativaId),
            new MySqlParameter("p_questao_id", questaoId),
            new MySqlParameter("p_alternativa_id", alternativaId.HasValue ? alternativaId.Value : DBNull.Value),
            new MySqlParameter("p_marcada_revisao", marcadaRevisao),
            new MySqlParameter("p_tempo_segundos", tempoSegundos));

    public Task FinalizarTentativaAsync(long tentativaId, string motivo, CancellationToken ct) =>
        ExecutarAsync(
            "sp_finalizar_tentativa",
            ct,
            new MySqlParameter("p_tentativa_id", tentativaId),
            new MySqlParameter("p_motivo", motivo));

    public Task ExpirarTentativasAsync(CancellationToken ct) =>
        ExecutarAsync("sp_expirar_tentativas", ct);

    public Task ReconciliarMetricasAsync(CancellationToken ct) =>
        ExecutarAsync("sp_reconciliar_metricas", ct);

    public Task AtualizarMetricasAsync(long usuarioId, CancellationToken ct) =>
        ExecutarAsync("sp_atualizar_metricas", ct, new MySqlParameter("p_usuario_id", usuarioId));

    public Task ConcederBadgesAsync(long usuarioId, CancellationToken ct) =>
        ExecutarAsync("sp_conceder_badges", ct, new MySqlParameter("p_usuario_id", usuarioId));

    public async Task SugerirTrilhaAsync(long usuarioId, CancellationToken ct)
    {
        var conexao = await AbrirAsync(ct);
        await using var comando = conexao.CreateCommand();
        comando.CommandText = "sp_sugerir_trilha";
        comando.CommandType = CommandType.StoredProcedure;
        comando.Parameters.Add(new MySqlParameter("p_usuario_id", usuarioId));
        await using var leitor = await comando.ExecuteReaderAsync(ct);
        while (await leitor.ReadAsync(ct))
        {
        }
    }

    private async Task ExecutarAsync(string procedimento, CancellationToken ct, params MySqlParameter[] parametros)
    {
        var conexao = await AbrirAsync(ct);
        await using var comando = conexao.CreateCommand();
        comando.CommandText = procedimento;
        comando.CommandType = CommandType.StoredProcedure;
        comando.Transaction = _db.Database.CurrentTransaction?.GetDbTransaction();
        foreach (var parametro in parametros)
        {
            comando.Parameters.Add(parametro);
        }

        await comando.ExecuteNonQueryAsync(ct);
    }

    private async Task<System.Data.Common.DbConnection> AbrirAsync(CancellationToken ct)
    {
        var conexao = _db.Database.GetDbConnection();
        if (conexao.State != ConnectionState.Open)
        {
            await conexao.OpenAsync(ct);
        }

        return conexao;
    }
}
