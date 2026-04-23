using TradingJournal.Core.Domain;

namespace TradingJournal.Core.Models;

public sealed record CsvImportResult(
    int RowsProcessed,
    int RowsImported,
    IReadOnlyList<Trade> ImportedTrades,
    IReadOnlyList<string> Errors);
