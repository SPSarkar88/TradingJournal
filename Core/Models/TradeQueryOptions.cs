namespace TradingJournal.Core.Models;

public sealed class TradeQueryOptions
{
    public Guid? AccountId { get; init; }

    public bool RequireAccountScope { get; init; }

    public string? Symbol { get; init; }

    public string? StrategyTag { get; init; }

    public string? Direction { get; init; }

    public string? TradeType { get; init; }

    public DateTime? FromEntryTime { get; init; }

    public DateTime? ToEntryTime { get; init; }

    public bool? IsWinningTrade { get; init; }

    public TradeSortField SortBy { get; init; } = TradeSortField.EntryTime;

    public bool Descending { get; init; } = true;
}

public enum TradeSortField
{
    EntryTime = 0,
    ExitTime = 1,
    Symbol = 2,
    NetPnL = 3,
    Quantity = 4
}
