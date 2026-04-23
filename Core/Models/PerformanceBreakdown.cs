namespace TradingJournal.Core.Models;

public sealed record PerformanceBreakdown(
    string Label,
    int TradeCount,
    int WinningTrades,
    decimal TotalPnL,
    decimal AveragePnL,
    decimal WinRate);
