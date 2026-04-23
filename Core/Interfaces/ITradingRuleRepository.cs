using TradingJournal.Core.Domain;

namespace TradingJournal.Core.Interfaces;

public interface ITradingRuleRepository : IRepository<TradingRule>
{
    Task<IReadOnlyList<TradingRule>> GetActiveAsync(CancellationToken cancellationToken = default);
}
