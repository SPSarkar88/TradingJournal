using Microsoft.EntityFrameworkCore;
using TradingJournal.Core.Interfaces;
using TradingJournal.Infrastructure.Persistence;

namespace TradingJournal.Infrastructure.Repositories;

public class GenericRepository<T>(AppDbContext dbContext) : IRepository<T> where T : class
{
    protected AppDbContext DbContext { get; } = dbContext;

    protected DbSet<T> DbSet { get; } = dbContext.Set<T>();

    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet.FindAsync([id], cancellationToken);
    }

    public async Task AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await DbSet.AddAsync(entity, cancellationToken);
        await DbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        DbSet.Update(entity);
        await DbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            return;
        }

        DbSet.Remove(entity);
        await DbContext.SaveChangesAsync(cancellationToken);
    }
}
