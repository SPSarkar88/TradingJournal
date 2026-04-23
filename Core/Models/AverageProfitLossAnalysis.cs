namespace TradingJournal.Core.Models;

public sealed record AverageProfitLossAnalysis(
    decimal AverageProfit,
    decimal AverageLoss,
    decimal ProfitLossRatio);
