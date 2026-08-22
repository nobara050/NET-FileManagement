using System.Linq.Expressions;
using Drive.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Drive.Infrastructure.Persistence;

public sealed class Repository<TEntity> : IRepository<TEntity>
    where TEntity : class
{
    private readonly DriveDbContext _dbContext;

    public Repository(DriveDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<TEntity>> ListAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<TEntity>()
            .Where(predicate)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountAsync(
    Expression<Func<TEntity, bool>> predicate,
    CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<TEntity>()
            .CountAsync(predicate, cancellationToken);
    }

    public async Task<TEntity?> SingleOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<TEntity>()
            .SingleOrDefaultAsync(predicate, cancellationToken);
    }

    public async Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<TEntity>()
            .AnyAsync(predicate, cancellationToken);
    }

    public async Task AddAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Set<TEntity>()
            .AddAsync(entity, cancellationToken);
    }

    public void Update(TEntity entity)
    {
        _dbContext.Set<TEntity>().Update(entity);
    }

    public Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}