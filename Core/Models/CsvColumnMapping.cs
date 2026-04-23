namespace TradingJournal.Core.Models;

public sealed class CsvColumnMapping
{
    public string Symbol { get; init; } = nameof(Domain.Trade.Symbol);

    public string EntryPrice { get; init; } = nameof(Domain.Trade.EntryPrice);

    public string ExitPrice { get; init; } = nameof(Domain.Trade.ExitPrice);

    public string Quantity { get; init; } = nameof(Domain.Trade.Quantity);

    public string TradeType { get; init; } = nameof(Domain.Trade.TradeType);

    public string Direction { get; init; } = nameof(Domain.Trade.Direction);

    public string EntryTime { get; init; } = nameof(Domain.Trade.EntryTime);

    public string ExitTime { get; init; } = nameof(Domain.Trade.ExitTime);

    public string StopLossPrice { get; init; } = nameof(Domain.Trade.StopLossPrice);

    public string Brokerage { get; init; } = nameof(Domain.Trade.Brokerage);

    public string Taxes { get; init; } = nameof(Domain.Trade.Taxes);

    public string StrategyTag { get; init; } = nameof(Domain.Trade.StrategyTag);

    public string Notes { get; init; } = nameof(Domain.Trade.Notes);

    public string ScreenshotPath { get; init; } = nameof(Domain.Trade.ScreenshotPath);

    public IReadOnlyList<string> GetRequiredColumns() =>
    [
        Symbol,
        EntryPrice,
        Quantity,
        TradeType,
        Direction,
        EntryTime
    ];

    public IReadOnlyList<string> GetAllColumns() =>
    [
        Symbol,
        EntryPrice,
        ExitPrice,
        Quantity,
        TradeType,
        Direction,
        EntryTime,
        ExitTime,
        StopLossPrice,
        Brokerage,
        Taxes,
        StrategyTag,
        Notes,
        ScreenshotPath
    ];
}
