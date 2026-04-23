using TradingJournal.Core.Domain;

namespace TradingJournal.Core.Interfaces;

public interface IAccountRepository : IRepository<Account>
{
    Task<IReadOnlyList<Account>> GetByBrokerAsync(string broker, CancellationToken cancellationToken = default);
}
