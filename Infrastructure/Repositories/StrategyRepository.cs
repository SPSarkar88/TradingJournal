using Microsoft.EntityFrameworkCore;
using TradingJournal.Core.Domain;
using TradingJournal.Core.Interfaces;
using TradingJournal.Infrastructure.Persistence;

namespace TradingJournal.Infrastructure.Repositories;

public sealed class StrategyRepository(AppDbContext dbContext)
    : GenericRepository<Strategy>(dbContext), IStrategyRepository
{
    public async Task<Strategy?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Include(x => x.Trades)
            .FirstOrDefaultAsync(x => x.Name == name, cancellationToken);
    }
}
