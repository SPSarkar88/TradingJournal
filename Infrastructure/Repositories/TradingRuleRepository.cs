using Microsoft.EntityFrameworkCore;
using TradingJournal.Core.Domain;
using TradingJournal.Core.Interfaces;
using TradingJournal.Infrastructure.Persistence;

namespace TradingJournal.Infrastructure.Repositories;

public sealed class TradingRuleRepository(AppDbContext dbContext)
    : GenericRepository<TradingRule>(dbContext), ITradingRuleRepository
{
    public async Task<IReadOnlyList<TradingRule>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }
}
