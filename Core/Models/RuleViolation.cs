namespace TradingJournal.Core.Models;

public sealed record RuleViolation(
    string RuleCode,
    string Message,
    DateTime OccurredAt,
    Guid? TradeId = null);
