using TradingJournal.Core.Domain;

namespace TradingJournal.Core.Interfaces;

public interface IStrategyRepository : IRepository<Strategy>
{
    Task<Strategy?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
}
