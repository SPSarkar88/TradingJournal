namespace TradingJournal.Core.Domain;

public sealed class TradingRule
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string RuleType { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int? IntValue { get; set; }

    public bool? BoolValue { get; set; }

    public bool IsActive { get; set; } = true;
}
