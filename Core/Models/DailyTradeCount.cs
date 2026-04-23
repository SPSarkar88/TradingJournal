namespace TradingJournal.Core.Models;

public sealed record DailyTradeCount(DateOnly TradeDate, int TradeCount);
