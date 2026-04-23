using TradingJournal.Core.Models;

namespace TradingJournal.Modules.Trades;

public static class TradesModule
{
    public static ModuleDefinition CreateDefinition() =>
        new(
            "trades",
            "Trades",
            "Capture execution details, pricing, and risk for every position.");
}
