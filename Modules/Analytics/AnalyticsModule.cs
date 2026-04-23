using TradingJournal.Core.Models;

namespace TradingJournal.Modules.Analytics;

public static class AnalyticsModule
{
    public static ModuleDefinition CreateDefinition() =>
        new(
            "analytics",
            "Analytics",
            "Surface win rate, P&L trends, drawdown, and equity-curve insights.");
}
