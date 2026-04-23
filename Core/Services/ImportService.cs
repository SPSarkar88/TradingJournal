using System.IO;
using System.Globalization;
using System.Text;
using TradingJournal.Core.Domain;
using TradingJournal.Core.Interfaces;
using TradingJournal.Core.Models;

namespace TradingJournal.Core.Services;

public sealed class ImportService(ITradeRepository? tradeRepository = null)
{
    public CsvValidationResult ValidateCSV(IReadOnlyList<string> headers, CsvColumnMapping? mapping = null)
    {
        mapping ??= new CsvColumnMapping();

        var columnIndexes = headers
            .Select((header, index) => new { Header = header.Trim(), Index = index })
            .GroupBy(x => x.Header, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First().Index, StringComparer.OrdinalIgnoreCase);

        var errors = mapping.GetRequiredColumns()
            .Where(column => !columnIndexes.ContainsKey(column))
            .Select(column => $"Missing required column '{column}'.")
            .ToList();

        return new CsvValidationResult(errors.Count == 0, columnIndexes, errors);
    }

    public Trade MapCSVToTrade(IReadOnlyDictionary<string, string> row, CsvColumnMapping? mapping = null)
        => MapCSVToTrade(row, mapping, null);

    public Trade MapCSVToTrade(
        IReadOnlyDictionary<string, string> row,
        CsvColumnMapping? mapping,
        Guid? accountId)
    {
        mapping ??= new CsvColumnMapping();

        var trade = new Trade
        {
            Symbol = GetRequiredString(row, mapping.Symbol),
            EntryPrice = GetRequiredDecimal(row, mapping.EntryPrice),
            ExitPrice = GetOptionalDecimal(row, mapping.ExitPrice),
            Quantity = GetRequiredDecimal(row, mapping.Quantity),
            TradeType = GetRequiredString(row, mapping.TradeType),
            Direction = GetRequiredString(row, mapping.Direction),
            EntryTime = GetRequiredDateTime(row, mapping.EntryTime),
            ExitTime = GetOptionalDateTime(row, mapping.ExitTime),
            StopLossPrice = GetOptionalDecimal(row, mapping.StopLossPrice),
            Brokerage = GetOptionalDecimal(row, mapping.Brokerage) ?? 0m,
            Taxes = GetOptionalDecimal(row, mapping.Taxes) ?? 0m,
            StrategyTag = GetOptionalString(row, mapping.StrategyTag),
            Notes = GetOptionalString(row, mapping.Notes),
            ScreenshotPath = GetOptionalString(row, mapping.ScreenshotPath),
            AccountId = accountId
        };

        trade.RecalculateNetPnL();
        return trade;
    }

    public async Task<CsvImportResult> ImportTradesFromCSV(
        string filePath,
        CsvColumnMapping? mapping = null,
        Guid? accountId = null,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            return new CsvImportResult(0, 0, [], [$"CSV file '{filePath}' was not found."]);
        }

        mapping ??= new CsvColumnMapping();

        var importedTrades = new List<Trade>();
        var errors = new List<string>();
        var rowsProcessed = 0;

        using var reader = new StreamReader(filePath);

        var headerLine = await reader.ReadLineAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(headerLine))
        {
            return new CsvImportResult(0, 0, [], ["CSV file is empty."]);
        }

        var headers = ParseCsvLine(headerLine);
        var validation = ValidateCSV(headers, mapping);
        if (!validation.IsValid)
        {
            return new CsvImportResult(0, 0, [], validation.Errors);
        }

        string? line;
        var rowNumber = 1;

        while ((line = await reader.ReadLineAsync(cancellationToken)) is not null)
        {
            rowNumber++;

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            rowsProcessed++;

            try
            {
                var row = BuildRow(headers, ParseCsvLine(line));
                var trade = MapCSVToTrade(row, mapping, accountId);
                importedTrades.Add(trade);

                if (tradeRepository is not null)
                {
                    await tradeRepository.AddAsync(trade, cancellationToken);
                }
            }
            catch (Exception exception)
            {
                errors.Add($"Row {rowNumber}: {exception.Message}");
            }
        }

        return new CsvImportResult(rowsProcessed, importedTrades.Count, importedTrades, errors);
    }

    private static Dictionary<string, string> BuildRow(IReadOnlyList<string> headers, IReadOnlyList<string> values)
    {
        var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < headers.Count; index++)
        {
            var value = index < values.Count ? values[index] : string.Empty;
            row[headers[index].Trim()] = value.Trim();
        }

        return row;
    }

    private static List<string> ParseCsvLine(string line)
    {
        var values = new List<string>();
        var currentValue = new StringBuilder();
        var insideQuotes = false;

        for (var index = 0; index < line.Length; index++)
        {
            var currentCharacter = line[index];

            if (currentCharacter == '"')
            {
                var escapedQuote = insideQuotes && index + 1 < line.Length && line[index + 1] == '"';
                if (escapedQuote)
                {
                    currentValue.Append('"');
                    index++;
                    continue;
                }

                insideQuotes = !insideQuotes;
                continue;
            }

            if (currentCharacter == ',' && !insideQuotes)
            {
                values.Add(currentValue.ToString());
                currentValue.Clear();
                continue;
            }

            currentValue.Append(currentCharacter);
        }

        values.Add(currentValue.ToString());
        return values;
    }

    private static string GetRequiredString(IReadOnlyDictionary<string, string> row, string columnName)
    {
        if (!row.TryGetValue(columnName, out var value) || string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Column '{columnName}' is required.");
        }

        return value.Trim();
    }

    private static string GetOptionalString(IReadOnlyDictionary<string, string> row, string columnName)
    {
        return row.TryGetValue(columnName, out var value) ? value.Trim() : string.Empty;
    }

    private static decimal GetRequiredDecimal(IReadOnlyDictionary<string, string> row, string columnName)
    {
        var rawValue = GetRequiredString(row, columnName);
        return decimal.Parse(rawValue, NumberStyles.Number, CultureInfo.InvariantCulture);
    }

    private static decimal? GetOptionalDecimal(IReadOnlyDictionary<string, string> row, string columnName)
    {
        var rawValue = GetOptionalString(row, columnName);
        return string.IsNullOrWhiteSpace(rawValue)
            ? null
            : decimal.Parse(rawValue, NumberStyles.Number, CultureInfo.InvariantCulture);
    }

    private static DateTime GetRequiredDateTime(IReadOnlyDictionary<string, string> row, string columnName)
    {
        var rawValue = GetRequiredString(row, columnName);
        return DateTime.Parse(rawValue, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal);
    }

    private static DateTime? GetOptionalDateTime(IReadOnlyDictionary<string, string> row, string columnName)
    {
        var rawValue = GetOptionalString(row, columnName);
        return string.IsNullOrWhiteSpace(rawValue)
            ? null
            : DateTime.Parse(rawValue, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal);
    }
}
