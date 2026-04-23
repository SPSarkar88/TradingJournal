using System.Globalization;
using System.Windows.Input;
using Microsoft.Win32;
using TradingJournal.Core.Domain;
using TradingJournal.Core.Models;
using TradingJournal.Core.Services;
using TradingJournal.Infrastructure.Persistence;
using TradingJournal.Infrastructure.Repositories;
using TradingJournal.Services;

namespace TradingJournal.ViewModels;

public sealed class TradeEntryViewModel : ViewModelBase
{
    private readonly ActiveAccountService _activeAccountService;
    private readonly TradeService _tradeService = new();
    private Guid _tradeId;
    private Guid? _accountId;
    private string _symbol = string.Empty;
    private decimal _entryPrice;
    private decimal? _exitPrice;
    private decimal _quantity = 1m;
    private string _selectedTradeType = "Intraday";
    private string _direction = "Buy";
    private DateTime? _entryDate = DateTime.Today;
    private string _entryTimeText = DateTime.Now.ToString("HH:mm");
    private DateTime? _exitDate = DateTime.Today;
    private string _exitTimeText = DateTime.Now.ToString("HH:mm");
    private decimal? _stopLossPrice;
    private decimal _brokerage;
    private decimal _taxes;
    private string _strategyTag = string.Empty;
    private string _notes = string.Empty;
    private string _screenshotPath = string.Empty;
    private decimal _grossPnL;
    private decimal _netPnL;
    private bool _isEditMode;
    private string _statusMessage = "Enter a trade and calculate the result before saving.";
    private string _ruleAlertMessage = string.Empty;

    public TradeEntryViewModel(ActiveAccountService activeAccountService)
    {
        _activeAccountService = activeAccountService;
        _accountId = activeAccountService.ActiveAccountId;
        _activeAccountService.PropertyChanged += HandleActiveAccountChanged;

        TradeTypes =
        [
            "Intraday",
            "Swing",
            "Options",
            "Futures"
        ];

        CalculateCommand = new RelayCommand(CalculatePnL);
        SaveCommand = new AsyncRelayCommand(SaveAsync, CanSave);
        NewTradeCommand = new RelayCommand(StartNewTrade);
        SelectScreenshotCommand = new RelayCommand(SelectScreenshot);
        StartNewTrade();
    }

    public Func<TradeViewModel, Task>? TradeSavedCallback { get; set; }

    public Action<TradeViewModel?>? TradeLoadedCallback { get; set; }

    public Action? CloseRequested { get; set; }

    public bool CloseAfterSave { get; set; }

    public IReadOnlyList<string> TradeTypes { get; }

    public string ActiveAccountDisplay => _activeAccountService.ActiveAccountDisplay;

    public string FormTitle => IsEditMode ? "Edit Trade" : "New Trade";

    public string SaveButtonText => IsEditMode ? "Update Trade" : "Save Trade";

    public string Symbol
    {
        get => _symbol;
        set => SetProperty(ref _symbol, value);
    }

    public decimal EntryPrice
    {
        get => _entryPrice;
        set => SetProperty(ref _entryPrice, value);
    }

    public decimal? ExitPrice
    {
        get => _exitPrice;
        set => SetProperty(ref _exitPrice, value);
    }

    public decimal Quantity
    {
        get => _quantity;
        set => SetProperty(ref _quantity, value);
    }

    public string SelectedTradeType
    {
        get => _selectedTradeType;
        set => SetProperty(ref _selectedTradeType, value);
    }

    public bool IsBuy
    {
        get => _direction == "Buy";
        set
        {
            if (value)
            {
                Direction = "Buy";
            }
        }
    }

    public bool IsSell
    {
        get => _direction == "Sell";
        set
        {
            if (value)
            {
                Direction = "Sell";
            }
        }
    }

    public DateTime? EntryDate
    {
        get => _entryDate;
        set => SetProperty(ref _entryDate, value);
    }

    public string EntryTimeText
    {
        get => _entryTimeText;
        set => SetProperty(ref _entryTimeText, value);
    }

    public DateTime? ExitDate
    {
        get => _exitDate;
        set => SetProperty(ref _exitDate, value);
    }

    public string ExitTimeText
    {
        get => _exitTimeText;
        set => SetProperty(ref _exitTimeText, value);
    }

    public decimal? StopLossPrice
    {
        get => _stopLossPrice;
        set => SetProperty(ref _stopLossPrice, value);
    }

    public decimal Brokerage
    {
        get => _brokerage;
        set => SetProperty(ref _brokerage, value);
    }

    public decimal Taxes
    {
        get => _taxes;
        set => SetProperty(ref _taxes, value);
    }

    public string StrategyTag
    {
        get => _strategyTag;
        set => SetProperty(ref _strategyTag, value);
    }

    public string Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
    }

    public string ScreenshotPath
    {
        get => _screenshotPath;
        set => SetProperty(ref _screenshotPath, value);
    }

    public decimal GrossPnL
    {
        get => _grossPnL;
        private set => SetProperty(ref _grossPnL, value);
    }

    public decimal NetPnL
    {
        get => _netPnL;
        private set => SetProperty(ref _netPnL, value);
    }

    public bool IsEditMode
    {
        get => _isEditMode;
        private set
        {
            if (!SetProperty(ref _isEditMode, value))
            {
                return;
            }

            OnPropertyChanged(nameof(FormTitle));
            OnPropertyChanged(nameof(SaveButtonText));
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string RuleAlertMessage
    {
        get => _ruleAlertMessage;
        private set => SetProperty(ref _ruleAlertMessage, value);
    }

    public ICommand CalculateCommand { get; }

    public ICommand SaveCommand { get; }

    public ICommand NewTradeCommand { get; }

    public ICommand SelectScreenshotCommand { get; }

    public void LoadTrade(TradeViewModel trade)
    {
        _tradeId = trade.Id;
        _accountId = trade.AccountId;
        Symbol = trade.Symbol;
        EntryPrice = trade.EntryPrice;
        ExitPrice = trade.ExitPrice;
        Quantity = trade.Quantity;
        SelectedTradeType = trade.TradeType;
        Direction = trade.Direction;
        EntryDate = trade.EntryTime.Date;
        EntryTimeText = trade.EntryTime.ToString("HH:mm");
        ExitDate = trade.ExitTime?.Date;
        ExitTimeText = trade.ExitTime?.ToString("HH:mm") ?? DateTime.Now.ToString("HH:mm");
        StopLossPrice = trade.StopLossPrice;
        Brokerage = trade.Brokerage;
        Taxes = trade.Taxes;
        StrategyTag = trade.StrategyTag;
        Notes = trade.Notes;
        ScreenshotPath = trade.ScreenshotPath;
        IsEditMode = true;
        CalculatePnL();
        StatusMessage = $"Editing trade '{trade.Symbol}'.";
        TradeLoadedCallback?.Invoke(TradeViewModel.FromTrade(BuildTrade()));
    }

    public void StartNewTrade()
    {
        _tradeId = Guid.Empty;
        _accountId = _activeAccountService.ActiveAccountId;
        Symbol = string.Empty;
        EntryPrice = 0m;
        ExitPrice = null;
        Quantity = 1m;
        SelectedTradeType = "Intraday";
        Direction = "Buy";
        EntryDate = DateTime.Today;
        EntryTimeText = DateTime.Now.ToString("HH:mm");
        ExitDate = DateTime.Today;
        ExitTimeText = DateTime.Now.ToString("HH:mm");
        StopLossPrice = null;
        Brokerage = 0m;
        Taxes = 0m;
        StrategyTag = string.Empty;
        Notes = string.Empty;
        ScreenshotPath = string.Empty;
        GrossPnL = 0m;
        NetPnL = 0m;
        IsEditMode = false;
        StatusMessage = _activeAccountService.HasActiveAccount
            ? $"Ready for a new trade in {_activeAccountService.ActiveAccountDisplay}."
            : "Create or select an active account before saving trades.";
        RuleAlertMessage = string.Empty;
        TradeLoadedCallback?.Invoke(null);
    }

    private string Direction
    {
        get => _direction;
        set
        {
            if (!SetProperty(ref _direction, value))
            {
                return;
            }

            OnPropertyChanged(nameof(IsBuy));
            OnPropertyChanged(nameof(IsSell));
        }
    }

    private void CalculatePnL()
    {
        var trade = BuildTrade();
        GrossPnL = _tradeService.CalculateGrossPnL(trade);
        NetPnL = _tradeService.CalculateNetPnL(trade);
        StatusMessage = "Calculated gross and net P&L.";
    }

    private bool CanSave()
    {
        return !string.IsNullOrWhiteSpace(Symbol)
               && Quantity > 0m
               && EntryDate.HasValue
               && _activeAccountService.HasActiveAccount
               && !string.IsNullOrWhiteSpace(SelectedTradeType);
    }

    private async Task SaveAsync()
    {
        var trade = BuildTrade();
        trade.RecalculateNetPnL();
        GrossPnL = _tradeService.CalculateGrossPnL(trade);
        NetPnL = trade.NetPnL;

        var violations = await GetRuleViolationsAsync(trade);
        RuleAlertMessage = violations.Count == 0
            ? string.Empty
            : string.Join(Environment.NewLine, violations.Select(x => x.Message));

        if (violations.Count > 0)
        {
            var proceed = System.Windows.MessageBox.Show(
                $"This trade violates one or more active rules:{Environment.NewLine}{Environment.NewLine}{RuleAlertMessage}{Environment.NewLine}{Environment.NewLine}Save anyway?",
                "Rule Violations",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (proceed != System.Windows.MessageBoxResult.Yes)
            {
                StatusMessage = "Save cancelled due to rule violations.";
                return;
            }
        }

        using var dbContext = new AppDbContext(AppDbContextOptionsFactory.Create());
        var repository = new TradeRepository(dbContext);
        var successMessage = string.Empty;

        if (IsEditMode)
        {
            await repository.UpdateAsync(trade);
            successMessage = $"Updated trade '{trade.Symbol}'.";
        }
        else
        {
            await repository.AddAsync(trade);
            successMessage = $"Saved trade '{trade.Symbol}'.";
        }

        StatusMessage = successMessage;
        var savedTradeViewModel = TradeViewModel.FromTrade(trade);

        if (TradeSavedCallback is not null)
        {
            await TradeSavedCallback(savedTradeViewModel);
        }

        if (CloseAfterSave)
        {
            CloseRequested?.Invoke();
            return;
        }

        StartNewTrade();
    }

    private Trade BuildTrade()
    {
        var entryTimestamp = CombineDateAndTime(EntryDate ?? DateTime.Today, EntryTimeText);
        var exitTimestamp = ExitPrice.HasValue && ExitDate.HasValue
            ? (DateTime?)CombineDateAndTime(ExitDate.Value, ExitTimeText)
            : null;

        return new Trade
        {
            Id = _tradeId == Guid.Empty ? Guid.NewGuid() : _tradeId,
            Symbol = Symbol.Trim().ToUpperInvariant(),
            EntryPrice = EntryPrice,
            ExitPrice = ExitPrice,
            Quantity = Quantity,
            TradeType = SelectedTradeType,
            Direction = Direction,
            EntryTime = entryTimestamp,
            ExitTime = exitTimestamp,
            StopLossPrice = StopLossPrice,
            Brokerage = Brokerage,
            Taxes = Taxes,
            StrategyTag = StrategyTag.Trim(),
            Notes = Notes.Trim(),
            ScreenshotPath = ScreenshotPath.Trim(),
            AccountId = _accountId ?? _activeAccountService.ActiveAccountId
        };
    }

    private async Task<IReadOnlyList<RuleViolation>> GetRuleViolationsAsync(Trade trade)
    {
        using var dbContext = new AppDbContext(AppDbContextOptionsFactory.Create());
        var rulesRepository = new TradingRuleRepository(dbContext);
        var tradeRepository = new TradeRepository(dbContext);
        var rules = await rulesRepository.GetActiveAsync();
        var existingTrades = await tradeRepository.GetFilteredAsync(new TradeQueryOptions
        {
            AccountId = trade.AccountId,
            RequireAccountScope = true
        });

        var options = ToTradingRuleOptions(rules);
        var service = new RuleService();

        var tradesToEvaluate = existingTrades.ToList();
        if (IsEditMode)
        {
            tradesToEvaluate.RemoveAll(x => x.Id == trade.Id);
        }

        tradesToEvaluate.Add(trade);

        return service.FlagViolations(tradesToEvaluate, options);
    }

    private static TradingRuleOptions ToTradingRuleOptions(IReadOnlyList<TradingRule> rules)
    {
        return new TradingRuleOptions
        {
            MaxTradesPerDay = rules.FirstOrDefault(x => x.RuleType == "MaxTradesPerDay")?.IntValue,
            RequireStopLoss = rules.FirstOrDefault(x => x.RuleType == "MandatoryStopLoss")?.BoolValue ?? false
        };
    }

    private void SelectScreenshot()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.gif",
            Title = "Select Screenshot"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        ScreenshotPath = dialog.FileName;
        StatusMessage = "Screenshot selected.";
    }

    private static DateTime CombineDateAndTime(DateTime date, string timeText)
    {
        var parsedTime = TimeOnly.TryParseExact(timeText, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var time)
            ? time
            : new TimeOnly(9, 15);

        return date.Date.Add(parsedTime.ToTimeSpan());
    }

    private void HandleActiveAccountChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is not nameof(ActiveAccountService.ActiveAccountId)
            and not nameof(ActiveAccountService.ActiveAccountDisplay))
        {
            return;
        }

        OnPropertyChanged(nameof(ActiveAccountDisplay));
        CommandManager.InvalidateRequerySuggested();

        if (IsEditMode)
        {
            return;
        }

        _accountId = _activeAccountService.ActiveAccountId;
        StatusMessage = _activeAccountService.HasActiveAccount
            ? $"Ready for a new trade in {_activeAccountService.ActiveAccountDisplay}."
            : "Create or select an active account before saving trades.";
    }
}
