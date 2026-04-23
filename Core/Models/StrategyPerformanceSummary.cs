namespace TradingJournal.Core.Models;

public sealed record StrategyPerformanceSummary(
    Guid Id,
    string Name,
    string Description,
    int TradeCount,
    decimal TotalPnL,
    decimal WinRate);
