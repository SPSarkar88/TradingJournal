using Microsoft.EntityFrameworkCore;
using TradingJournal.Core.Domain;
using TradingJournal.Core.Interfaces;
using TradingJournal.Infrastructure.Persistence;

namespace TradingJournal.Infrastructure.Repositories;

public sealed class AccountRepository(AppDbContext dbContext)
    : GenericRepository<Account>(dbContext), IAccountRepository
{
    public async Task<IReadOnlyList<Account>> GetByBrokerAsync(string broker, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Include(x => x.Trades)
            .Where(x => x.Broker == broker)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }
}
