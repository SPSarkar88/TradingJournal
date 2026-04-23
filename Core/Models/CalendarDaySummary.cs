namespace TradingJournal.Core.Models;

public sealed class CalendarDaySummary
{
    public DateOnly Date { get; init; }

    public bool IsCurrentMonth { get; init; }

    public decimal TotalPnL { get; init; }

    public int TradeCount { get; init; }

    public string HeatLevel
    {
        get
        {
            if (TradeCount == 0)
            {
                return "Neutral";
            }

            return TotalPnL > 0m ? "Profit" : TotalPnL < 0m ? "Loss" : "Neutral";
        }
    }
}
