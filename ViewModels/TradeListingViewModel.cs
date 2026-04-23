using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using TradingJournal.Core.Domain;
using TradingJournal.Core.Models;
using TradingJournal.Core.Services;
using TradingJournal.Infrastructure.Persistence;
using TradingJournal.Infrastructure.Repositories;
using TradingJournal.Services;

namespace TradingJournal.ViewModels;

public sealed class TradeListingViewModel : ViewModelBase
{
    private readonly ActiveAccountService _activeAccountService;
    private string _searchSymbol = string.Empty;
    private string _strategyFilter = string.Empty;
    private string _selectedProfitLossFilter = "All";
    private DateTime? _fromDate;
    private DateTime? _toDate;
    private string _currentSortMember = nameof(TradeViewModel.EntryTime);
    private bool _sortDescending = true;
    private string _statusMessage = "Load trades to begin reviewing the journal.";

    public TradeListingViewModel(ActiveAccountService activeAccountService)
    {
        _activeAccountService = activeAccountService;
        _activeAccountService.PropertyChanged += HandleActiveAccountChanged;

        ProfitLossFilters =
        [
            "All",
            "Profitable",
            "Losing"
        ];

        Trades = new ObservableCollection<TradeViewModel>();
        NewTradeCommand = new RelayCommand(() => NewTradeRequestedCallback?.Invoke());
        LoadTradesCommand = new AsyncRelayCommand(LoadTradesAsync);
        ApplyFiltersCommand = new AsyncRelayCommand(LoadTradesAsync);
        ClearFiltersCommand = new AsyncRelayCommand(ClearFiltersAsync);
        SortTradesCommand = new RelayCommand<string>(ApplySort);
        EditTradeCommand = new RelayCommand<TradeViewModel>(trade =>
        {
            if (trade is not null)
            {
                EditRequestedCallback?.Invoke(trade);
            }
        });
        DeleteTradeCommand = new AsyncRelayCommand<TradeViewModel>(DeleteTradeAsync);
    }

    public Action<TradeViewModel>? EditRequestedCallback { get; set; }

    public Action<TradeViewModel?>? TradeSelectedCallback { get; set; }

    public Action? NewTradeRequestedCallback { get; set; }

    public IReadOnlyList<string> ProfitLossFilters { get; }

    public ObservableCollection<TradeViewModel> Trades { get; }

    public string ActiveAccountDisplay => _activeAccountService.ActiveAccountDisplay;

    public string SearchSymbol
    {
        get => _searchSymbol;
        set => SetProperty(ref _searchSymbol, value);
    }

    public string StrategyFilter
    {
        get => _strategyFilter;
        set => SetProperty(ref _strategyFilter, value);
    }

    public string SelectedProfitLossFilter
    {
        get => _selectedProfitLossFilter;
        set => SetProperty(ref _selectedProfitLossFilter, value);
    }

    public DateTime? FromDate
    {
        get => _fromDate;
        set => SetProperty(ref _fromDate, value);
    }

    public DateTime? ToDate
    {
        get => _toDate;
        set => SetProperty(ref _toDate, value);
    }

    public string CurrentSortMember
    {
        get => _currentSortMember;
        private set => SetProperty(ref _currentSortMember, value);
    }

    public bool SortDescending
    {
        get => _sortDescending;
        private set => SetProperty(ref _sortDescending, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public ICommand NewTradeCommand { get; }

    public ICommand LoadTradesCommand { get; }

    public ICommand ApplyFiltersCommand { get; }

    public ICommand ClearFiltersCommand { get; }

    public ICommand SortTradesCommand { get; }

    public ICommand EditTradeCommand { get; }

    public ICommand DeleteTradeCommand { get; }

    public async Task LoadTradesAsync()
    {
        if (!_activeAccountService.HasActiveAccount)
        {
            Trades.Clear();
            TradeSelectedCallback?.Invoke(null);
            StatusMessage = "Select an active account to view trades.";
            return;
        }

        using var dbContext = new AppDbContext(AppDbContextOptionsFactory.Create());
        var repository = new TradeRepository(dbContext);
        var trades = await repository.GetFilteredAsync(BuildQueryOptions());
        var rulesRepository = new TradingRuleRepository(dbContext);
        var activeRules = await rulesRepository.GetActiveAsync();
        var enrichedTrades = ApplyRuleViolations(trades, activeRules);

        ReplaceTrades(ApplySortInternal(enrichedTrades));
        StatusMessage = $"{Trades.Count} trades loaded.";
    }

    private async Task ClearFiltersAsync()
    {
        SearchSymbol = string.Empty;
        StrategyFilter = string.Empty;
        SelectedProfitLossFilter = "All";
        FromDate = null;
        ToDate = null;
        CurrentSortMember = nameof(TradeViewModel.EntryTime);
        SortDescending = true;
        await LoadTradesAsync();
    }

    private async Task DeleteTradeAsync(TradeViewModel? trade)
    {
        if (trade is null)
        {
            return;
        }

        var result = MessageBox.Show(
            $"Delete trade '{trade.Symbol}' from {trade.DateDisplay}?",
            "Delete Trade",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        using var dbContext = new AppDbContext(AppDbContextOptionsFactory.Create());
        var repository = new TradeRepository(dbContext);
        await repository.DeleteAsync(trade.Id);
        await LoadTradesAsync();
        StatusMessage = $"Deleted trade '{trade.Symbol}'.";
    }

    private void ApplySort(string? sortMember)
    {
        if (string.IsNullOrWhiteSpace(sortMember))
        {
            return;
        }

        SortDescending = CurrentSortMember == sortMember ? !SortDescending : false;
        CurrentSortMember = sortMember;
        ReplaceTrades(ApplySortInternal(Trades));
    }

    private TradeQueryOptions BuildQueryOptions()
    {
        return new TradeQueryOptions
        {
            AccountId = _activeAccountService.ActiveAccountId,
            RequireAccountScope = true,
            Symbol = string.IsNullOrWhiteSpace(SearchSymbol) ? null : SearchSymbol.Trim(),
            StrategyTag = string.IsNullOrWhiteSpace(StrategyFilter) ? null : StrategyFilter.Trim(),
            FromEntryTime = FromDate,
            ToEntryTime = ToDate?.Date.AddDays(1).AddTicks(-1),
            IsWinningTrade = SelectedProfitLossFilter switch
            {
                "Profitable" => true,
                "Losing" => false,
                _ => null
            }
        };
    }

    private IEnumerable<TradeViewModel> ApplySortInternal(IEnumerable<TradeViewModel> trades)
    {
        Func<TradeViewModel, object?> selector = CurrentSortMember switch
        {
            nameof(TradeViewModel.Symbol) => trade => trade.Symbol,
            nameof(TradeViewModel.EntryPrice) => trade => trade.EntryPrice,
            nameof(TradeViewModel.ExitPrice) => trade => trade.ExitPrice,
            nameof(TradeViewModel.NetPnL) => trade => trade.NetPnL,
            nameof(TradeViewModel.DateDisplay) => trade => trade.EntryTime,
            nameof(TradeViewModel.StrategyTag) => trade => trade.StrategyTag,
            nameof(TradeViewModel.TradeType) => trade => trade.TradeType,
            _ => trade => trade.EntryTime
        };

        return SortDescending
            ? trades.OrderByDescending(selector).ToList()
            : trades.OrderBy(selector).ToList();
    }

    private void ReplaceTrades(IEnumerable<TradeViewModel> trades)
    {
        Trades.Clear();

        foreach (var trade in trades)
        {
            Trades.Add(trade);
        }

        TradeSelectedCallback?.Invoke(Trades.FirstOrDefault());
    }

    private static IReadOnlyList<TradeViewModel> ApplyRuleViolations(
        IReadOnlyList<Trade> trades,
        IReadOnlyList<TradingRule> activeRules)
    {
        var options = new TradingRuleOptions
        {
            MaxTradesPerDay = activeRules.FirstOrDefault(x => x.RuleType == "MaxTradesPerDay")?.IntValue,
            RequireStopLoss = activeRules.FirstOrDefault(x => x.RuleType == "MandatoryStopLoss")?.BoolValue ?? false
        };

        var violations = new RuleService()
            .FlagViolations(trades, options)
            .Where(x => x.TradeId.HasValue)
            .GroupBy(x => x.TradeId!.Value)
            .ToDictionary(
                x => x.Key,
                x => string.Join("; ", x.Select(v => v.Message)));

        return trades
            .Select(trade => TradeViewModel.FromTrade(
                trade,
                violations.TryGetValue(trade.Id, out var summary) ? summary : string.Empty))
            .ToList();
    }

    private void HandleActiveAccountChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is not nameof(ActiveAccountService.ActiveAccountId)
            and not nameof(ActiveAccountService.ActiveAccountDisplay))
        {
            return;
        }

        OnPropertyChanged(nameof(ActiveAccountDisplay));
        _ = LoadTradesAsync();
    }
}
