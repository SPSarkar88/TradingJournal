using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using TradingJournal.Core.Domain;
using TradingJournal.Core.Models;
using TradingJournal.Core.Services;
using TradingJournal.Infrastructure.Persistence;
using TradingJournal.Infrastructure.Repositories;
using TradingJournal.Services;

namespace TradingJournal.ViewModels;

public sealed class StrategyViewModel : WorkspaceViewModelBase
{
    private readonly ActiveAccountService _activeAccountService;
    private readonly AnalyticsService _analyticsService = new();
    private StrategyPerformanceSummary? _selectedStrategy;
    private string _name = string.Empty;
    private string _description = string.Empty;
    private string _statusMessage = "Manage strategy definitions and review their results.";

    public StrategyViewModel(ActiveAccountService activeAccountService)
        : base(
            "Strategies",
            "Create, update, and evaluate the setups you trade repeatedly.",
            "Use strategies to categorize trades and compare what is actually working.",
            [])
    {
        _activeAccountService = activeAccountService;
        _activeAccountService.PropertyChanged += HandleActiveAccountChanged;
        Strategies = new ObservableCollection<StrategyPerformanceSummary>();
        SaveCommand = new AsyncRelayCommand(SaveAsync, CanSave);
        DeleteCommand = new AsyncRelayCommand(DeleteAsync, () => SelectedStrategy is not null);
        NewCommand = new RelayCommand(StartNew);
        RefreshCommand = new AsyncRelayCommand(LoadAsync);
        _ = LoadAsync();
    }

    public ObservableCollection<StrategyPerformanceSummary> Strategies { get; }

    public StrategyPerformanceSummary? SelectedStrategy
    {
        get => _selectedStrategy;
        set
        {
            if (!SetProperty(ref _selectedStrategy, value))
            {
                return;
            }

            Name = value?.Name ?? string.Empty;
            Description = value?.Description ?? string.Empty;
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

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string ActiveAccountDisplay => _activeAccountService.ActiveAccountDisplay;

    public ICommand SaveCommand { get; }

    public ICommand DeleteCommand { get; }

    public ICommand NewCommand { get; }

    public ICommand RefreshCommand { get; }

    private async Task LoadAsync()
    {
        using var dbContext = new AppDbContext(AppDbContextOptionsFactory.Create());
        var strategyRepository = new StrategyRepository(dbContext);
        var tradeRepository = new TradeRepository(dbContext);

        var strategies = await strategyRepository.GetAllAsync();
        var trades = await tradeRepository.GetFilteredAsync(new TradeQueryOptions
        {
            AccountId = _activeAccountService.ActiveAccountId,
            RequireAccountScope = true
        });
        var analysis = _analyticsService.AnalyzeByStrategy(trades)
            .ToDictionary(x => x.Label, StringComparer.OrdinalIgnoreCase);

        Strategies.Clear();

        foreach (var strategy in strategies.OrderBy(x => x.Name))
        {
            analysis.TryGetValue(strategy.Name, out var performance);
            Strategies.Add(new StrategyPerformanceSummary(
                strategy.Id,
                strategy.Name,
                strategy.Description,
                performance?.TradeCount ?? 0,
                performance?.TotalPnL ?? 0m,
                performance?.WinRate ?? 0m));
        }

        StatusMessage = _activeAccountService.HasActiveAccount
            ? $"{Strategies.Count} strategies loaded for {_activeAccountService.ActiveAccountDisplay}."
            : "Select an active account to review strategy performance.";
    }

    private bool CanSave() => !string.IsNullOrWhiteSpace(Name);

    private async Task SaveAsync()
    {
        using var dbContext = new AppDbContext(AppDbContextOptionsFactory.Create());
        var repository = new StrategyRepository(dbContext);

        var strategy = new Strategy
        {
            Id = SelectedStrategy?.Id ?? Guid.NewGuid(),
            Name = Name.Trim(),
            Description = Description.Trim()
        };

        if (SelectedStrategy is null)
        {
            await repository.AddAsync(strategy);
            StatusMessage = $"Added strategy '{strategy.Name}'.";
        }
        else
        {
            await repository.UpdateAsync(strategy);
            StatusMessage = $"Updated strategy '{strategy.Name}'.";
        }

        await LoadAsync();
        StartNew();
    }

    private async Task DeleteAsync()
    {
        if (SelectedStrategy is null)
        {
            return;
        }

        using var dbContext = new AppDbContext(AppDbContextOptionsFactory.Create());
        var repository = new StrategyRepository(dbContext);
        await repository.DeleteAsync(SelectedStrategy.Id);
        StatusMessage = $"Deleted strategy '{SelectedStrategy.Name}'.";
        await LoadAsync();
        StartNew();
    }

    private void StartNew()
    {
        SelectedStrategy = null;
        Name = string.Empty;
        Description = string.Empty;
    }

    private void HandleActiveAccountChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is not nameof(ActiveAccountService.ActiveAccountId)
            and not nameof(ActiveAccountService.ActiveAccountDisplay))
        {
            return;
        }

        OnPropertyChanged(nameof(ActiveAccountDisplay));
        _ = LoadAsync();
    }
}
