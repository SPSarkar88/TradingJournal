using TradingJournal.Core.Domain;

namespace TradingJournal.Core.Services;

public sealed class TradeService
{
    public decimal CalculateGrossPnL(Trade trade)
    {
        if (!trade.ExitPrice.HasValue)
        {
            return 0m;
        }

        var priceDelta = trade.Direction.Equals("Sell", StringComparison.OrdinalIgnoreCase)
            ? trade.EntryPrice - trade.ExitPrice.Value
            : trade.ExitPrice.Value - trade.EntryPrice;

        return priceDelta * trade.Quantity;
    }

    public decimal CalculateNetPnL(Trade trade)
    {
        return CalculateGrossPnL(trade) - trade.Brokerage - trade.Taxes;
    }

    public decimal CalculateRMultiple(Trade trade, decimal stopLossPrice)
    {
        var riskAmount = CalculateRiskAmount(trade, stopLossPrice);
        if (riskAmount <= 0m)
        {
            return 0m;
        }

        return CalculateNetPnL(trade) / riskAmount;
    }

    public decimal CalculateRMultiple(Trade trade)
    {
        return trade.StopLossPrice.HasValue
            ? CalculateRMultiple(trade, trade.StopLossPrice.Value)
            : 0m;
    }

    public decimal CalculateRiskPercentage(Trade trade, decimal capital, decimal stopLossPrice)
    {
        if (capital <= 0m)
        {
            return 0m;
        }

        var riskAmount = CalculateRiskAmount(trade, stopLossPrice);
        return riskAmount <= 0m ? 0m : riskAmount / capital * 100m;
    }

    public decimal CalculateRiskPercentage(Trade trade, decimal capital)
    {
        return trade.StopLossPrice.HasValue
            ? CalculateRiskPercentage(trade, capital, trade.StopLossPrice.Value)
            : 0m;
    }

    private static decimal CalculateRiskAmount(Trade trade, decimal stopLossPrice)
    {
        return Math.Abs(trade.EntryPrice - stopLossPrice) * trade.Quantity;
    }
}
