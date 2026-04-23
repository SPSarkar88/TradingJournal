using Microsoft.EntityFrameworkCore;
using TradingJournal.Core.Domain;

namespace TradingJournal.Infrastructure.Persistence;

public static class AppDataSeeder
{
    private const string SeedMarker = "[Seeded Demo]";
    private const int MinimumSeededTradeCount = 200;

    private static readonly (string Name, string Broker)[] DemoAccounts =
    [
        ("ZZ Test Equity", "Paper Broker"),
        ("ZZ Test Swing", "Interactive Brokers"),
        ("ZZ Test Derivatives", "Zerodha")
    ];

    private static readonly (string Name, string Description)[] DemoStrategies =
    [
        ("Opening Range Breakout", "Captures early session momentum after price confirms above the range."),
        ("VWAP Reclaim", "Enters after price reclaims VWAP with volume support and a defined stop."),
        ("Pullback Continuation", "Buys or sells the first clean retracement in a trending move."),
        ("Trend Day Add-on", "Scales into strong intraday trends after confirming higher highs or lower lows."),
        ("Reversal Fade", "Fades exhaustion at stretched levels with a tight invalidation point.")
    ];

    private static readonly string[] Symbols =
    [
        "AAPL",
        "MSFT",
        "NVDA",
        "TSLA",
        "META",
        "AMZN",
        "NFLX",
        "AMD",
        "SPY",
        "QQQ",
        "NIFTY",
        "BANKNIFTY"
    ];

    private static readonly string[] TradeTypes =
    [
        "Intraday",
        "Swing",
        "Options",
        "Futures"
    ];

    private static readonly string[] NoteThemes =
    [
        "Followed the opening plan and respected the stop.",
        "Waited for confirmation before entering the setup.",
        "Execution was disciplined and position size stayed within limits.",
        "Took the trade only after trend and volume aligned.",
        "Managed the exit based on structure instead of emotion."
    ];

    private static readonly string[] ReviewThemes =
    [
        "Exit was timely and matched the setup quality.",
        "Could hold winners longer when higher timeframe trend is intact.",
        "Good patience before entry; no impulsive scaling.",
        "Risk was controlled well and the trade stayed within plan.",
        "Review suggests keeping the same setup but tightening late entries."
    ];

    public static async Task<SeedSummary> SeedAsync(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        await EnsureReferenceDataAsync(dbContext, cancellationToken);

        var demoAccounts = await dbContext.Accounts
            .Where(x => DemoAccounts.Select(a => a.Name).Contains(x.Name))
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        var demoStrategies = await dbContext.Strategies
            .Where(x => DemoStrategies.Select(s => s.Name).Contains(x.Name))
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        var existingSeededTradeCount = await dbContext.Trades
            .CountAsync(x => x.Notes.StartsWith(SeedMarker), cancellationToken);

        var tradesToCreate = Math.Max(0, MinimumSeededTradeCount - existingSeededTradeCount);
        if (tradesToCreate > 0)
        {
            var trades = GenerateTrades(existingSeededTradeCount, tradesToCreate, demoAccounts, demoStrategies);
            var journals = trades.Select((trade, index) => BuildJournal(trade, existingSeededTradeCount + index)).ToList();

            await dbContext.Trades.AddRangeAsync(trades, cancellationToken);
            await dbContext.Journals.AddRangeAsync(journals, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return new SeedSummary(
            CreatedTradeCount: tradesToCreate,
            DemoTradeCount: await dbContext.Trades.CountAsync(x => x.Notes.StartsWith(SeedMarker), cancellationToken),
            TotalTradeCount: await dbContext.Trades.CountAsync(cancellationToken),
            AccountCount: await dbContext.Accounts.CountAsync(cancellationToken),
            StrategyCount: await dbContext.Strategies.CountAsync(cancellationToken),
            JournalCount: await dbContext.Journals.CountAsync(cancellationToken),
            RuleCount: await dbContext.TradingRules.CountAsync(cancellationToken));
    }

    private static async Task EnsureReferenceDataAsync(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var existingAccounts = await dbContext.Accounts
            .AsNoTracking()
            .Select(x => x.Name)
            .ToListAsync(cancellationToken);

        var missingAccounts = DemoAccounts
            .Where(x => !existingAccounts.Contains(x.Name, StringComparer.OrdinalIgnoreCase))
            .Select(x => new Account
            {
                Name = x.Name,
                Broker = x.Broker
            })
            .ToList();

        if (missingAccounts.Count > 0)
        {
            await dbContext.Accounts.AddRangeAsync(missingAccounts, cancellationToken);
        }

        var existingStrategies = await dbContext.Strategies
            .AsNoTracking()
            .Select(x => x.Name)
            .ToListAsync(cancellationToken);

        var missingStrategies = DemoStrategies
            .Where(x => !existingStrategies.Contains(x.Name, StringComparer.OrdinalIgnoreCase))
            .Select(x => new Strategy
            {
                Name = x.Name,
                Description = x.Description
            })
            .ToList();

        if (missingStrategies.Count > 0)
        {
            await dbContext.Strategies.AddRangeAsync(missingStrategies, cancellationToken);
        }

        var hasMaxTradesRule = await dbContext.TradingRules
            .AsNoTracking()
            .AnyAsync(x => x.RuleType == "MaxTradesPerDay", cancellationToken);

        if (!hasMaxTradesRule)
        {
            await dbContext.TradingRules.AddAsync(new TradingRule
            {
                Name = "Demo Max 4 Trades Per Day",
                RuleType = "MaxTradesPerDay",
                Description = "Sample rule included with the demo dataset.",
                IntValue = 4,
                IsActive = false
            }, cancellationToken);
        }

        var hasMandatoryStopLossRule = await dbContext.TradingRules
            .AsNoTracking()
            .AnyAsync(x => x.RuleType == "MandatoryStopLoss", cancellationToken);

        if (!hasMandatoryStopLossRule)
        {
            await dbContext.TradingRules.AddAsync(new TradingRule
            {
                Name = "Demo Mandatory Stop Loss",
                RuleType = "MandatoryStopLoss",
                Description = "Sample rule included with the demo dataset.",
                BoolValue = true,
                IsActive = false
            }, cancellationToken);
        }

        if (dbContext.ChangeTracker.HasChanges())
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static List<Trade> GenerateTrades(
        int existingSeededTradeCount,
        int tradesToCreate,
        IReadOnlyList<Account> accounts,
        IReadOnlyList<Strategy> strategies)
    {
        var random = new Random(20260417 + existingSeededTradeCount);
        var trades = new List<Trade>(tradesToCreate);
        var totalWindowDays = Math.Max(120, (existingSeededTradeCount + tradesToCreate) / 2 + 20);

        for (var index = 0; index < tradesToCreate; index++)
        {
            var ordinal = existingSeededTradeCount + index;
            var account = accounts[ordinal % accounts.Count];
            var strategy = strategies[ordinal % strategies.Count];
            var symbol = Symbols[ordinal % Symbols.Length];
            var tradeType = TradeTypes[ordinal % TradeTypes.Length];
            var direction = ordinal % 3 == 0 ? "Sell" : "Buy";
            var tradeDate = DateTime.Today.AddDays(-(totalWindowDays - ordinal / 2));
            var entryHour = ordinal % 2 == 0 ? 9 : 13;
            var entryTime = tradeDate.Date
                .AddHours(entryHour)
                .AddMinutes(15 + random.Next(0, 35));

            var holdMinutes = tradeType == "Swing"
                ? random.Next(1, 4) * 24 * 60 + random.Next(30, 180)
                : random.Next(25, 180);

            var entryPrice = GetEntryPrice(symbol, random);
            var exitPrice = GetExitPrice(entryPrice, random);
            var quantity = GetQuantity(tradeType, random);
            var stopLossDistancePercent = random.Next(8, 22) / 1000m;
            var stopLossPrice = direction == "Sell"
                ? decimal.Round(entryPrice * (1m + stopLossDistancePercent), 2)
                : decimal.Round(entryPrice * (1m - stopLossDistancePercent), 2);

            var trade = new Trade
            {
                Symbol = symbol,
                EntryPrice = entryPrice,
                ExitPrice = exitPrice,
                Quantity = quantity,
                TradeType = tradeType,
                Direction = direction,
                EntryTime = entryTime,
                ExitTime = entryTime.AddMinutes(holdMinutes),
                StopLossPrice = stopLossPrice,
                Brokerage = decimal.Round(3m + quantity * 0.08m + random.Next(0, 400) / 100m, 2),
                Taxes = decimal.Round(1.5m + Math.Abs(exitPrice - entryPrice) * quantity * 0.01m + random.Next(0, 250) / 100m, 2),
                StrategyId = strategy.Id,
                StrategyTag = strategy.Name,
                AccountId = account.Id,
                Notes = $"{SeedMarker} Trade #{ordinal + 1}. {NoteThemes[ordinal % NoteThemes.Length]}"
            };

            trades.Add(trade);
        }

        return trades;
    }

    private static Journal BuildJournal(Trade trade, int ordinal)
    {
        return new Journal
        {
            TradeId = trade.Id,
            PreTradeNotes = $"{SeedMarker} Planned {trade.Direction.ToLowerInvariant()} setup on {trade.Symbol} using '{trade.StrategyTag}'. {NoteThemes[ordinal % NoteThemes.Length]}",
            PostTradeReview = $"{SeedMarker} Review for trade #{ordinal + 1}. {ReviewThemes[ordinal % ReviewThemes.Length]}"
        };
    }

    private static decimal GetEntryPrice(string symbol, Random random)
    {
        var basePrice = symbol switch
        {
            "NVDA" => 870m,
            "TSLA" => 215m,
            "MSFT" => 415m,
            "META" => 490m,
            "AMZN" => 185m,
            "NFLX" => 620m,
            "AMD" => 170m,
            "SPY" => 520m,
            "QQQ" => 445m,
            "NIFTY" => 22450m,
            "BANKNIFTY" => 48200m,
            _ => 185m
        };

        var variancePercent = random.Next(-45, 46) / 1000m;
        return decimal.Round(basePrice * (1m + variancePercent), 2);
    }

    private static decimal GetExitPrice(decimal entryPrice, Random random)
    {
        var movePercent = random.Next(-35, 46) / 1000m;
        var exitPrice = decimal.Round(entryPrice * (1m + movePercent), 2);
        return exitPrice <= 0m ? decimal.Round(entryPrice * 0.98m, 2) : exitPrice;
    }

    private static decimal GetQuantity(string tradeType, Random random)
    {
        return tradeType switch
        {
            "Options" => random.Next(25, 201),
            "Futures" => random.Next(1, 9),
            "Swing" => random.Next(5, 75),
            _ => random.Next(10, 151)
        };
    }

    public sealed record SeedSummary(
        int CreatedTradeCount,
        int DemoTradeCount,
        int TotalTradeCount,
        int AccountCount,
        int StrategyCount,
        int JournalCount,
        int RuleCount)
    {
        public string ToConsoleText() =>
            $"Created demo trades: {CreatedTradeCount}{Environment.NewLine}" +
            $"Demo trades in database: {DemoTradeCount}{Environment.NewLine}" +
            $"Total trades in database: {TotalTradeCount}{Environment.NewLine}" +
            $"Accounts: {AccountCount}{Environment.NewLine}" +
            $"Strategies: {StrategyCount}{Environment.NewLine}" +
            $"Journals: {JournalCount}{Environment.NewLine}" +
            $"Rules: {RuleCount}";
    }
}
