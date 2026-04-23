using System.Collections.ObjectModel;
using System.Windows.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using TradingJournal.Core.Domain;
using TradingJournal.Core.Models;
using TradingJournal.Core.Services;
using TradingJournal.Infrastructure.Persistence;
using TradingJournal.Infrastructure.Repositories;
using TradingJournal.Services;

namespace TradingJournal.ViewModels;

public sealed class DashboardViewModel : WorkspaceViewModelBase
{
    private readonly ActiveAccountService _activeAccountService;
    private readonly AnalyticsService _analyticsService = new();
    private decimal _totalPnL;
    private decimal _winRate;
    private decimal _averageProfit;
    private decimal _averageLoss;
    private decimal _averageRMultiple;
    private string _rMultipleLabel = "Awaiting stop-loss data";
    private IEnumerable<ISeries> _equityCurveSeries = [];
    private IEnumerable<ISeries> _winLossSeries = [];

    public DashboardViewModel(ActiveAccountService activeAccountService)
        : base(
            "Analytics Dashboard",
            "Track performance at a glance with equity, distribution, and summary views.",
            "The dashboard uses live repository data to populate the MVP trading metrics.",
            [])
    {
        _activeAccountService = activeAccountService;
        _activeAccountService.PropertyChanged += HandleActiveAccountChanged;
        DailySummaries = new ObservableCollection<PeriodPnlSummary>();
        WeeklySummaries = new ObservableCollection<PeriodPnlSummary>();
        RefreshCommand = new AsyncRelayCommand(LoadAsync);
        _ = LoadAsync();
    }

    public decimal TotalPnL
    {
        get => _totalPnL;
        private set => SetProperty(ref _totalPnL, value);
    }

    public decimal WinRate
    {
        get => _winRate;
        private set
        {
            if (!SetProperty(ref _winRate, value))
            {
                return;
            }

            OnPropertyChanged(nameof(WinRateDisplay));
        }
    }

    public string WinRateDisplay => $"{WinRate:N1}%";

    public decimal AverageProfit
    {
        get => _averageProfit;
        private set => SetProperty(ref _averageProfit, value);
    }

    public decimal AverageLoss
    {
        get => _averageLoss;
        private set => SetProperty(ref _averageLoss, value);
    }

    public decimal AverageRMultiple
    {
        get => _averageRMultiple;
        private set => SetProperty(ref _averageRMultiple, value);
    }

    public string RMultipleLabel
    {
        get => _rMultipleLabel;
        private set => SetProperty(ref _rMultipleLabel, value);
    }

    public ObservableCollection<PeriodPnlSummary> DailySummaries { get; }

    public ObservableCollection<PeriodPnlSummary> WeeklySummaries { get; }

    public IEnumerable<ISeries> EquityCurveSeries
    {
        get => _equityCurveSeries;
        private set => SetProperty(ref _equityCurveSeries, value);
    }

    public IEnumerable<ISeries> WinLossSeries
    {
        get => _winLossSeries;
        private set => SetProperty(ref _winLossSeries, value);
    }

    public ICommand RefreshCommand { get; }

    private async Task LoadAsync()
    {
        if (!_activeAccountService.HasActiveAccount)
        {
            ClearDashboard();
            return;
        }

        using var dbContext = new AppDbContext(AppDbContextOptionsFactory.Create());
        var repository = new TradeRepository(dbContext);
        var trades = await repository.GetFilteredAsync(new TradeQueryOptions
        {
            AccountId = _activeAccountService.ActiveAccountId,
            RequireAccountScope = true
        });

        var orderedClosedTrades = trades
            .Where(x => x.ExitPrice.HasValue)
            .OrderBy(x => x.ExitTime ?? x.EntryTime)
            .ToList();

        var avgProfitVsLoss = _analyticsService.CalculateAverageProfitVsLoss(orderedClosedTrades);
        TotalPnL = _analyticsService.CalculateTotalPnL(orderedClosedTrades);
        WinRate = _analyticsService.CalculateWinRate(orderedClosedTrades);
        AverageProfit = avgProfitVsLoss.AverageProfit;
        AverageLoss = avgProfitVsLoss.AverageLoss;
        AverageRMultiple = _analyticsService.CalculateAverageRMultiple(orderedClosedTrades);
        RMultipleLabel = orderedClosedTrades.Any(x => x.StopLossPrice.HasValue)
            ? $"{AverageRMultiple:N2}R average"
            : "Awaiting stop-loss data";

        ReplaceSummaries(DailySummaries, BuildDailySummaries(orderedClosedTrades));
        ReplaceSummaries(WeeklySummaries, BuildWeeklySummaries(orderedClosedTrades));
        EquityCurveSeries = BuildEquityCurveSeries(orderedClosedTrades);
        WinLossSeries = BuildWinLossSeries(orderedClosedTrades);
    }

    private void ClearDashboard()
    {
        TotalPnL = 0m;
        WinRate = 0m;
        AverageProfit = 0m;
        AverageLoss = 0m;
        AverageRMultiple = 0m;
        RMultipleLabel = "Select an active account";
        ReplaceSummaries(DailySummaries, Array.Empty<PeriodPnlSummary>());
        ReplaceSummaries(WeeklySummaries, Array.Empty<PeriodPnlSummary>());
        EquityCurveSeries = [];
        WinLossSeries = [];
    }

    private static IEnumerable<ISeries> BuildEquityCurveSeries(IReadOnlyList<Trade> trades)
    {
        var runningPnl = 0m;
        var equityValues = new List<decimal>();
        var service = new TradeService();

        foreach (var trade in trades)
        {
            runningPnl += service.CalculateNetPnL(trade);
            equityValues.Add(runningPnl);
        }

        return
        [
            new LineSeries<decimal>
            {
                Values = equityValues,
                Fill = null,
                GeometrySize = 8
            }
        ];
    }

    private static IEnumerable<ISeries> BuildWinLossSeries(IReadOnlyList<Trade> trades)
    {
        var wins = trades.Count(x => x.NetPnL > 0m);
        var losses = trades.Count(x => x.NetPnL <= 0m);

        return
        [
            new PieSeries<int> { Values = [wins], Name = "Wins" },
            new PieSeries<int> { Values = [losses], Name = "Losses" }
        ];
    }

    private static IReadOnlyList<PeriodPnlSummary> BuildDailySummaries(IEnumerable<Trade> trades)
    {
        return trades
            .GroupBy(x => x.EntryTime.Date)
            .OrderByDescending(x => x.Key)
            .Take(7)
            .Select(x => new PeriodPnlSummary(
                x.Key.ToString("dd MMM"),
                x.Sum(t => t.NetPnL),
                x.Count()))
            .ToList();
    }

    private static IReadOnlyList<PeriodPnlSummary> BuildWeeklySummaries(IEnumerable<Trade> trades)
    {
        return trades
            .GroupBy(x => StartOfWeek(x.EntryTime))
            .OrderByDescending(x => x.Key)
            .Take(6)
            .Select(x => new PeriodPnlSummary(
                $"{x.Key:dd MMM}",
                x.Sum(t => t.NetPnL),
                x.Count()))
            .ToList();
    }

    private static DateTime StartOfWeek(DateTime value)
    {
        var diff = (7 + (value.DayOfWeek - DayOfWeek.Monday)) % 7;
        return value.Date.AddDays(-1 * diff);
    }

    private static void ReplaceSummaries(
        ObservableCollection<PeriodPnlSummary> target,
        IReadOnlyList<PeriodPnlSummary> source)
    {
        target.Clear();

        foreach (var item in source)
        {
            target.Add(item);
        }
    }

    private void HandleActiveAccountChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ActiveAccountService.ActiveAccountId))
        {
            _ = LoadAsync();
        }
    }
}
