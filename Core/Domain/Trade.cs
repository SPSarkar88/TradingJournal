namespace TradingJournal.Core.Domain;

public sealed class Trade
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Symbol { get; set; } = string.Empty;

    public decimal EntryPrice { get; set; }

    public decimal? ExitPrice { get; set; }

    public decimal Quantity { get; set; }

    public string TradeType { get; set; } = "Intraday";

    public string Direction { get; set; } = "Buy";

    public DateTime EntryTime { get; set; } = DateTime.UtcNow;

    public DateTime? ExitTime { get; set; }

    public decimal? StopLossPrice { get; set; }

    public decimal Brokerage { get; set; }

    public decimal Taxes { get; set; }

    public decimal NetPnL { get; private set; }

    public string StrategyTag { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;

    public string ScreenshotPath { get; set; } = string.Empty;

    public Guid? StrategyId { get; set; }

    public Strategy? Strategy { get; set; }

    public Guid? AccountId { get; set; }

    public Account? Account { get; set; }

    public Journal? Journal { get; set; }

    public void RecalculateNetPnL()
    {
        if (!ExitPrice.HasValue)
        {
            NetPnL = 0m;
            return;
        }

        var directionMultiplier = Direction.Equals("Sell", StringComparison.OrdinalIgnoreCase) ? -1m : 1m;
        var grossPnL = (ExitPrice.Value - EntryPrice) * Quantity * directionMultiplier;

        NetPnL = grossPnL - Brokerage - Taxes;
    }
}
