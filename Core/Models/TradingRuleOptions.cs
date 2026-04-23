namespace TradingJournal.Core.Models;

public sealed class TradingRuleOptions
{
    public int? MaxTradesPerDay { get; init; }

    public bool RequireStopLoss { get; init; } = true;
}
