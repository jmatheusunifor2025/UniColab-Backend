using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using UniColab.Api.Data;

namespace UniColab.Api.Repositories;

public interface IUnidadeDeTrabalho
{
    Task SalvarAsync(CancellationToken ct);
    Task<IDbContextTransaction> IniciarTransacaoAsync(CancellationToken ct);
    Task DefinirAtorAsync(long atorId, CancellationToken ct);
    Task RecarregarAsync<T>(T entidade, CancellationToken ct) where T : class;
}

public sealed class UnidadeDeTrabalho : IUnidadeDeTrabalho
{
    private readonly UnicolabDbContext _db;

    public UnidadeDeTrabalho(UnicolabDbContext db)
    {
        _db = db;
    }

    public Task SalvarAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);

    public Task<IDbContextTransaction> IniciarTransacaoAsync(CancellationToken ct) =>
        _db.Database.BeginTransactionAsync(ct);

    public Task DefinirAtorAsync(long atorId, CancellationToken ct) =>
        _db.Database.ExecuteSqlInterpolatedAsync($"SET @unicolab_ator_id = {atorId}", ct);

    public Task RecarregarAsync<T>(T entidade, CancellationToken ct) where T : class =>
        _db.Entry(entidade).ReloadAsync(ct);
}
