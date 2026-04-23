namespace TradingJournal.Core.Models;

public sealed record TradeViolationSummary(Guid TradeId, IReadOnlyList<string> Violations);
