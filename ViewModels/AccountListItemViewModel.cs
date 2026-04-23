namespace TradingJournal.ViewModels;

public sealed class AccountListItemViewModel
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Broker { get; init; } = string.Empty;

    public int TradeCount { get; init; }

    public bool IsActive { get; init; }

    public string ActiveLabel => IsActive ? "Active" : string.Empty;
}
