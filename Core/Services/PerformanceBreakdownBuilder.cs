using TradingJournal.Core.Domain;
using TradingJournal.Core.Models;

namespace TradingJournal.Core.Services;

internal sealed class PerformanceBreakdownBuilder(TradeService tradeService)
{
    public PerformanceBreakdown Build(string label, IEnumerable<Trade> trades)
    {
        var tradeList = trades.Where(x => x.ExitPrice.HasValue).ToList();
        if (tradeList.Count == 0)
        {
            return new PerformanceBreakdown(label, 0, 0, 0m, 0m, 0m);
        }

        var pnlValues = tradeList
            .Select(tradeService.CalculateNetPnL)
            .ToList();

        var totalPnL = pnlValues.Sum();
        var winningTrades = pnlValues.Count(x => x > 0m);
        var averagePnL = totalPnL / tradeList.Count;
        var winRate = winningTrades * 100m / tradeList.Count;

        return new PerformanceBreakdown(label, tradeList.Count, winningTrades, totalPnL, averagePnL, winRate);
    }
}
