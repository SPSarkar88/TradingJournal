using TradingJournal.Core.Domain;
using TradingJournal.Core.Models;

namespace TradingJournal.Core.Services;

public sealed class AnalyticsService
{
    private readonly PerformanceBreakdownBuilder _performanceBreakdownBuilder;
    private readonly TradeService _tradeService;

    public AnalyticsService()
        : this(new TradeService())
    {
    }

    public AnalyticsService(TradeService tradeService)
    {
        _tradeService = tradeService;
        _performanceBreakdownBuilder = new PerformanceBreakdownBuilder(tradeService);
    }

    public decimal CalculateTotalPnL(IEnumerable<Trade> trades)
    {
        return GetClosedTradePnL(trades).Sum();
    }

    public decimal CalculateWinRate(IEnumerable<Trade> trades)
    {
        var pnlValues = GetClosedTradePnL(trades).ToList();
        if (pnlValues.Count == 0)
        {
            return 0m;
        }

        var winningTrades = pnlValues.Count(x => x > 0m);
        return winningTrades * 100m / pnlValues.Count;
    }

    public AverageProfitLossAnalysis CalculateAverageProfitVsLoss(IEnumerable<Trade> trades)
    {
        var pnlValues = GetClosedTradePnL(trades).ToList();
        var profits = pnlValues.Where(x => x > 0m).ToList();
        var losses = pnlValues.Where(x => x < 0m).Select(Math.Abs).ToList();

        var averageProfit = profits.Count == 0 ? 0m : profits.Average();
        var averageLoss = losses.Count == 0 ? 0m : losses.Average();
        var ratio = averageLoss <= 0m ? 0m : averageProfit / averageLoss;

        return new AverageProfitLossAnalysis(averageProfit, averageLoss, ratio);
    }

    public DrawdownAnalysis CalculateMaxDrawdown(IEnumerable<Trade> trades)
    {
        var orderedTrades = trades
            .Where(x => x.ExitPrice.HasValue)
            .OrderBy(x => x.EntryTime)
            .ToList();

        decimal equity = 0m;
        decimal peakEquity = 0m;
        decimal maxDrawdown = 0m;
        decimal maxDrawdownPercentage = 0m;
        DateTime? peakTime = null;
        DateTime? troughTime = null;
        DateTime? currentPeakTime = null;

        foreach (var trade in orderedTrades)
        {
            equity += _tradeService.CalculateNetPnL(trade);

            if (equity >= peakEquity)
            {
                peakEquity = equity;
                currentPeakTime = trade.ExitTime ?? trade.EntryTime;
            }

            var drawdown = peakEquity - equity;
            if (drawdown <= maxDrawdown)
            {
                continue;
            }

            maxDrawdown = drawdown;
            peakTime = currentPeakTime;
            troughTime = trade.ExitTime ?? trade.EntryTime;
            maxDrawdownPercentage = peakEquity <= 0m ? 0m : drawdown / peakEquity * 100m;
        }

        return new DrawdownAnalysis(maxDrawdown, maxDrawdownPercentage, peakTime, troughTime);
    }

    public IReadOnlyList<DailyTradeCount> CalculateTradesPerDay(IEnumerable<Trade> trades)
    {
        return trades
            .GroupBy(x => DateOnly.FromDateTime(x.EntryTime.Date))
            .OrderBy(x => x.Key)
            .Select(x => new DailyTradeCount(x.Key, x.Count()))
            .ToList();
    }

    public IReadOnlyList<PerformanceBreakdown> AnalyzeByStrategy(IEnumerable<Trade> trades)
    {
        return trades
            .GroupBy(x => string.IsNullOrWhiteSpace(x.StrategyTag) ? "Unassigned" : x.StrategyTag)
            .OrderBy(x => x.Key)
            .Select(x => _performanceBreakdownBuilder.Build(x.Key, x))
            .ToList();
    }

    public decimal CalculateAverageRMultiple(IEnumerable<Trade> trades)
    {
        var rMultiples = trades
            .Where(x => x.ExitPrice.HasValue && x.StopLossPrice.HasValue)
            .Select(trade => _tradeService.CalculateRMultiple(trade))
            .Where(x => x != 0m)
            .ToList();

        return rMultiples.Count == 0 ? 0m : rMultiples.Average();
    }

    private IEnumerable<decimal> GetClosedTradePnL(IEnumerable<Trade> trades)
    {
        return trades
            .Where(x => x.ExitPrice.HasValue)
            .Select(_tradeService.CalculateNetPnL);
    }
}
