namespace TradingJournal.Core.Models;

public sealed class ImportPreviewRow
{
    public int RowNumber { get; init; }

    public string Symbol { get; init; } = string.Empty;

    public string EntryPrice { get; init; } = string.Empty;

    public string ExitPrice { get; init; } = string.Empty;

    public string Quantity { get; init; } = string.Empty;

    public string TradeType { get; init; } = string.Empty;

    public string Direction { get; init; } = string.Empty;

    public string EntryTime { get; init; } = string.Empty;
}
