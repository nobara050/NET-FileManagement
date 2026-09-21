using System.Diagnostics;
using System.Linq.Expressions;
using Drive.Application.Common.Interfaces;
using Drive.Application.Common.Telemetry;
using Microsoft.EntityFrameworkCore;

namespace Drive.Infrastructure.Persistence.Repositories;

public class Repository<TEntity> : IRepository<TEntity>
    where TEntity : class
{
    protected readonly DriveDbContext DbContext;

    public Repository(DriveDbContext dbContext)
    {
        DbContext = dbContext;
    }

    public async Task<IReadOnlyList<TEntity>> ListAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        using var activity = StartActivity(nameof(ListAsync));
        return await DbContext.Set<TEntity>()
            .Where(predicate)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        using var activity = StartActivity(nameof(CountAsync));
        return await DbContext.Set<TEntity>()
            .CountAsync(predicate, cancellationToken);
    }

    public async Task<TEntity?> SingleOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        using var activity = StartActivity(nameof(SingleOrDefaultAsync));
        return await DbContext.Set<TEntity>()
            .SingleOrDefaultAsync(predicate, cancellationToken);
    }

    public async Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        using var activity = StartActivity(nameof(AnyAsync));
        return await DbContext.Set<TEntity>()
            .AnyAsync(predicate, cancellationToken);
    }

    public async Task AddAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        using var activity = StartActivity(nameof(AddAsync));
        await DbContext.Set<TEntity>()
            .AddAsync(entity, cancellationToken);
    }

    public void Update(TEntity entity)
    {
        using var activity = StartActivity(nameof(Update));
        DbContext.Set<TEntity>().Update(entity);
    }

    public void Remove(TEntity entity)
    {
        using var activity = StartActivity(nameof(Remove));
        DbContext.Set<TEntity>().Remove(entity);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        using var activity = StartActivity(nameof(SaveChangesAsync));
        await DbContext.SaveChangesAsync(cancellationToken);
    }

    // ---------------------------------------------------------------
    // Helper: tạo Activity với tên chuẩn cho Infrastructure layer
    // ---------------------------------------------------------------
    protected Activity? StartActivity(string methodName) =>
        ActivitySources.Infrastructure.StartActivity(
            $"Repository {typeof(TEntity).Name}.{methodName}",
            ActivityKind.Internal);
}
