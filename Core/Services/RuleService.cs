using TradingJournal.Core.Domain;
using TradingJournal.Core.Models;

namespace TradingJournal.Core.Services;

public sealed class RuleService
{
    public IReadOnlyList<RuleViolation> CheckMaxTradesPerDay(IEnumerable<Trade> trades, int maxTradesPerDay)
    {
        if (maxTradesPerDay <= 0)
        {
            return [];
        }

        return trades
            .GroupBy(x => x.EntryTime.Date)
            .Where(x => x.Count() > maxTradesPerDay)
            .OrderBy(x => x.Key)
            .Select(x => new RuleViolation(
                "MAX_TRADES_PER_DAY",
                $"Exceeded the daily limit of {maxTradesPerDay} trades with {x.Count()} trades on {x.Key:yyyy-MM-dd}.",
                x.Key))
            .ToList();
    }

    public bool CheckMandatoryStopLoss(Trade trade, decimal? stopLossPrice)
    {
        if (!stopLossPrice.HasValue)
        {
            return false;
        }

        return trade.Direction.Equals("Sell", StringComparison.OrdinalIgnoreCase)
            ? stopLossPrice.Value > trade.EntryPrice
            : stopLossPrice.Value < trade.EntryPrice;
    }

    public bool CheckMandatoryStopLoss(Trade trade) => CheckMandatoryStopLoss(trade, trade.StopLossPrice);

    public IReadOnlyList<RuleViolation> FlagViolations(
        IEnumerable<Trade> trades,
        TradingRuleOptions options,
        IReadOnlyDictionary<Guid, decimal?>? stopLossByTradeId = null)
    {
        var violations = new List<RuleViolation>();
        var tradeList = trades.ToList();

        if (options.MaxTradesPerDay.HasValue)
        {
            violations.AddRange(CheckMaxTradesPerDay(tradeList, options.MaxTradesPerDay.Value));
        }

        if (!options.RequireStopLoss)
        {
            return violations;
        }

        foreach (var trade in tradeList)
        {
            decimal? stopLossPrice = null;
            stopLossByTradeId?.TryGetValue(trade.Id, out stopLossPrice);

            var effectiveStopLossPrice = stopLossPrice ?? trade.StopLossPrice;

            if (CheckMandatoryStopLoss(trade, effectiveStopLossPrice))
            {
                continue;
            }

            violations.Add(new RuleViolation(
                "MANDATORY_STOP_LOSS",
                $"Trade '{trade.Symbol}' does not have a valid stop-loss value.",
                trade.EntryTime,
                trade.Id));
        }

        return violations;
    }
}
