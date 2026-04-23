namespace TradingJournal.Core.Domain;

public sealed class Account
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string Broker { get; set; } = string.Empty;

    public ICollection<Trade> Trades { get; set; } = [];
}
