using TradingJournal.Core.Models;

namespace TradingJournal.Modules.Journal;

public static class JournalModule
{
    public static ModuleDefinition CreateDefinition() =>
        new(
            "journal",
            "Journal",
            "Store pre-trade plans, post-trade reviews, and behavioral notes.");
}
