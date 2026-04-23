namespace TradingJournal.Core.Models;

public sealed record DrawdownAnalysis(
    decimal MaxDrawdownAmount,
    decimal MaxDrawdownPercentage,
    DateTime? PeakTime,
    DateTime? TroughTime);
