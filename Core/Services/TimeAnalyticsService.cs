using TradingJournal.Core.Domain;
using TradingJournal.Core.Models;

namespace TradingJournal.Core.Services;

public sealed class TimeAnalyticsService
{
    private readonly PerformanceBreakdownBuilder _performanceBreakdownBuilder;

    public TimeAnalyticsService()
        : this(new TradeService())
    {
    }

    public TimeAnalyticsService(TradeService tradeService)
    {
        _performanceBreakdownBuilder = new PerformanceBreakdownBuilder(tradeService);
    }

    public IReadOnlyList<PerformanceBreakdown> AnalyzeByTimeOfDay(IEnumerable<Trade> trades)
    {
        return trades
            .GroupBy(x => x.EntryTime.Hour)
            .OrderBy(x => x.Key)
            .Select(x => _performanceBreakdownBuilder.Build($"{x.Key:00}:00", x))
            .ToList();
    }

    public IReadOnlyList<PerformanceBreakdown> AnalyzeByDayOfWeek(IEnumerable<Trade> trades)
    {
        return trades
            .GroupBy(x => x.EntryTime.DayOfWeek)
            .OrderBy(x => GetDayOfWeekOrder(x.Key))
            .Select(x => _performanceBreakdownBuilder.Build(x.Key.ToString(), x))
            .ToList();
    }

    public IReadOnlyList<PerformanceBreakdown> AnalyzeByExpiryDays(
        IEnumerable<Trade> trades,
        Func<Trade, int?> expiryDaysSelector)
    {
        return trades
            .Select(trade => new { Trade = trade, ExpiryDays = expiryDaysSelector(trade) })
            .Where(x => x.ExpiryDays.HasValue)
            .GroupBy(x => x.ExpiryDays!.Value)
            .OrderBy(x => x.Key)
            .Select(x => _performanceBreakdownBuilder.Build($"{x.Key} DTE", x.Select(y => y.Trade)))
            .ToList();
    }

    private static int GetDayOfWeekOrder(DayOfWeek dayOfWeek)
    {
        return dayOfWeek switch
        {
            DayOfWeek.Monday => 1,
            DayOfWeek.Tuesday => 2,
            DayOfWeek.Wednesday => 3,
            DayOfWeek.Thursday => 4,
            DayOfWeek.Friday => 5,
            DayOfWeek.Saturday => 6,
            DayOfWeek.Sunday => 7,
            _ => 8
        };
    }
}
