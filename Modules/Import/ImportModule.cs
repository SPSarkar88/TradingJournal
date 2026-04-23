using TradingJournal.Core.Models;

namespace TradingJournal.Modules.Import;

public static class ImportModule
{
    public static ModuleDefinition CreateDefinition() =>
        new(
            "import",
            "Import",
            "Prepare CSV and broker feed ingestion with validation-ready workflows.");
}
