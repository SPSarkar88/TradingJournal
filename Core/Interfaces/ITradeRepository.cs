using TradingJournal.Core.Domain;
using TradingJournal.Core.Models;

namespace TradingJournal.Core.Interfaces;

public interface ITradeRepository : IRepository<Trade>
{
    Task<IReadOnlyList<Trade>> GetRecentAsync(int limit = 25, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Trade>> GetFilteredAsync(
        TradeQueryOptions queryOptions,
        CancellationToken cancellationToken = default);
}
