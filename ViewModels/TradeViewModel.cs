using TradingJournal.Core.Domain;

namespace TradingJournal.ViewModels;

public sealed class TradeViewModel : ViewModelBase
{
    public Guid Id { get; init; }

    public string Symbol { get; init; } = string.Empty;

    public decimal EntryPrice { get; init; }

    public decimal? ExitPrice { get; init; }

    public decimal Quantity { get; init; }

    public string TradeType { get; init; } = string.Empty;

    public string Direction { get; init; } = string.Empty;

    public DateTime EntryTime { get; init; }

    public DateTime? ExitTime { get; init; }

    public decimal? StopLossPrice { get; init; }

    public decimal Brokerage { get; init; }

    public decimal Taxes { get; init; }

    public decimal NetPnL { get; init; }

    public string StrategyTag { get; init; } = string.Empty;

    public string Notes { get; init; } = string.Empty;

    public string ScreenshotPath { get; init; } = string.Empty;

    public Guid? AccountId { get; init; }

    public string AccountDisplay { get; init; } = string.Empty;

    public string ViolationSummary { get; init; } = string.Empty;

    public string DateDisplay => EntryTime.ToString("dd MMM yyyy");

    public static TradeViewModel FromTrade(Trade trade, string? violationSummary = null)
    {
        return new TradeViewModel
        {
            Id = trade.Id,
            Symbol = trade.Symbol,
            EntryPrice = trade.EntryPrice,
            ExitPrice = trade.ExitPrice,
            Quantity = trade.Quantity,
            TradeType = trade.TradeType,
            Direction = trade.Direction,
            EntryTime = trade.EntryTime,
            ExitTime = trade.ExitTime,
            StopLossPrice = trade.StopLossPrice,
            Brokerage = trade.Brokerage,
            Taxes = trade.Taxes,
            NetPnL = trade.NetPnL,
            StrategyTag = trade.StrategyTag,
            Notes = trade.Notes,
            ScreenshotPath = trade.ScreenshotPath,
            AccountId = trade.AccountId,
            AccountDisplay = trade.Account is null ? string.Empty : $"{trade.Account.Name} ({trade.Account.Broker})",
            ViolationSummary = violationSummary ?? string.Empty
        };
    }

    public Trade ToTrade()
    {
        return new Trade
        {
            Id = Id,
            Symbol = Symbol,
            EntryPrice = EntryPrice,
            ExitPrice = ExitPrice,
            Quantity = Quantity,
            TradeType = TradeType,
            Direction = Direction,
            EntryTime = EntryTime,
            ExitTime = ExitTime,
            StopLossPrice = StopLossPrice,
            Brokerage = Brokerage,
            Taxes = Taxes,
            StrategyTag = StrategyTag,
            Notes = Notes,
            ScreenshotPath = ScreenshotPath,
            AccountId = AccountId
        };
    }
}
