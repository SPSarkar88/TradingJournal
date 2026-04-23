namespace TradingJournal.Core.Domain;

public sealed class Journal
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TradeId { get; set; }

    public Trade? Trade { get; set; }

    public string PreTradeNotes { get; set; } = string.Empty;

    public string PostTradeReview { get; set; } = string.Empty;
}
