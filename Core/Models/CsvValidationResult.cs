namespace TradingJournal.Core.Models;

public sealed record CsvValidationResult(
    bool IsValid,
    IReadOnlyDictionary<string, int> ColumnIndexes,
    IReadOnlyList<string> Errors);
