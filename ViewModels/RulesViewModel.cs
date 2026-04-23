using System.Collections.ObjectModel;
using System.Windows.Input;
using TradingJournal.Core.Domain;
using TradingJournal.Infrastructure.Persistence;
using TradingJournal.Infrastructure.Repositories;

namespace TradingJournal.ViewModels;

public sealed class RulesViewModel : WorkspaceViewModelBase
{
    private TradingRule? _selectedRule;
    private string _name = string.Empty;
    private string _description = string.Empty;
    private string _selectedRuleType = "MaxTradesPerDay";
    private string _intValueText = string.Empty;
    private bool _boolValue = true;
    private bool _isActive = true;
    private string _statusMessage = "Configure active rules to surface violations during trade review.";

    public RulesViewModel()
        : base(
            "Rules",
            "Define the guardrails that keep your execution honest.",
            "Rules are evaluated during trade entry and surfaced in the trade listing.",
            [])
    {
        RuleTypes =
        [
            "MaxTradesPerDay",
            "MandatoryStopLoss"
        ];

        Rules = new ObservableCollection<TradingRule>();
        SaveCommand = new AsyncRelayCommand(SaveAsync, CanSave);
        DeleteCommand = new AsyncRelayCommand(DeleteAsync, () => SelectedRule is not null);
        NewCommand = new RelayCommand(StartNew);
        RefreshCommand = new AsyncRelayCommand(LoadAsync);
        _ = LoadAsync();
    }

    public IReadOnlyList<string> RuleTypes { get; }

    public ObservableCollection<TradingRule> Rules { get; }

    public TradingRule? SelectedRule
    {
        get => _selectedRule;
        set
        {
            if (!SetProperty(ref _selectedRule, value))
            {
                return;
            }

            Name = value?.Name ?? string.Empty;
            Description = value?.Description ?? string.Empty;
            SelectedRuleType = value?.RuleType ?? "MaxTradesPerDay";
            IntValueText = value?.IntValue?.ToString() ?? string.Empty;
            BoolValue = value?.BoolValue ?? true;
            IsActive = value?.IsActive ?? true;
        }
    }

    public string Name
    {
        get => _name;
        set
        {
            if (SetProperty(ref _name, value))
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    public string SelectedRuleType
    {
        get => _selectedRuleType;
        set => SetProperty(ref _selectedRuleType, value);
    }

    public string IntValueText
    {
        get => _intValueText;
        set => SetProperty(ref _intValueText, value);
    }

    public bool BoolValue
    {
        get => _boolValue;
        set => SetProperty(ref _boolValue, value);
    }

    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public ICommand SaveCommand { get; }

    public ICommand DeleteCommand { get; }

    public ICommand NewCommand { get; }

    public ICommand RefreshCommand { get; }

    private async Task LoadAsync()
    {
        using var dbContext = new AppDbContext(AppDbContextOptionsFactory.Create());
        var repository = new TradingRuleRepository(dbContext);
        var rules = await repository.GetAllAsync();

        Rules.Clear();
        foreach (var rule in rules.OrderBy(x => x.Name))
        {
            Rules.Add(rule);
        }

        StatusMessage = $"{Rules.Count} rules loaded.";
    }

    private bool CanSave() => !string.IsNullOrWhiteSpace(Name);

    private async Task SaveAsync()
    {
        using var dbContext = new AppDbContext(AppDbContextOptionsFactory.Create());
        var repository = new TradingRuleRepository(dbContext);

        var rule = new TradingRule
        {
            Id = SelectedRule?.Id ?? Guid.NewGuid(),
            Name = Name.Trim(),
            Description = Description.Trim(),
            RuleType = SelectedRuleType,
            IntValue = int.TryParse(IntValueText, out var intValue) ? intValue : null,
            BoolValue = SelectedRuleType == "MandatoryStopLoss" ? BoolValue : null,
            IsActive = IsActive
        };

        if (SelectedRule is null)
        {
            await repository.AddAsync(rule);
            StatusMessage = $"Added rule '{rule.Name}'.";
        }
        else
        {
            await repository.UpdateAsync(rule);
            StatusMessage = $"Updated rule '{rule.Name}'.";
        }

        await LoadAsync();
        StartNew();
    }

    private async Task DeleteAsync()
    {
        if (SelectedRule is null)
        {
            return;
        }

        using var dbContext = new AppDbContext(AppDbContextOptionsFactory.Create());
        var repository = new TradingRuleRepository(dbContext);
        await repository.DeleteAsync(SelectedRule.Id);
        StatusMessage = $"Deleted rule '{SelectedRule.Name}'.";
        await LoadAsync();
        StartNew();
    }

    private void StartNew()
    {
        SelectedRule = null;
        Name = string.Empty;
        Description = string.Empty;
        SelectedRuleType = "MaxTradesPerDay";
        IntValueText = string.Empty;
        BoolValue = true;
        IsActive = true;
    }
}
