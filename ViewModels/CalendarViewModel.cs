using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using TradingJournal.Core.Models;
using TradingJournal.Infrastructure.Persistence;
using TradingJournal.Services;

namespace TradingJournal.ViewModels;

public sealed class CalendarViewModel : WorkspaceViewModelBase
{
    private readonly ActiveAccountService _activeAccountService;
    private DateTime _displayMonth = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    private CalendarDaySummary? _selectedDay;

    public CalendarViewModel(ActiveAccountService activeAccountService)
        : base(
            "Calendar",
            "Review trading outcomes on a month-by-month heatmap.",
            "Select a day to see the trades that contributed to that session.",
            [])
    {
        _activeAccountService = activeAccountService;
        _activeAccountService.PropertyChanged += HandleActiveAccountChanged;
        Days = new ObservableCollection<CalendarDaySummary>();
        SelectedDayTrades = new ObservableCollection<TradeViewModel>();
        PreviousMonthCommand = new AsyncRelayCommand(PreviousMonthAsync);
        NextMonthCommand = new AsyncRelayCommand(NextMonthAsync);
        SelectDayCommand = new AsyncRelayCommand<CalendarDaySummary>(SelectDayAsync);
        _ = LoadMonthAsync();
    }

    public ObservableCollection<CalendarDaySummary> Days { get; }

    public ObservableCollection<TradeViewModel> SelectedDayTrades { get; }

    public string DisplayMonthLabel => _displayMonth.ToString("MMMM yyyy");

    public string ActiveAccountDisplay => _activeAccountService.ActiveAccountDisplay;

    public CalendarDaySummary? SelectedDay
    {
        get => _selectedDay;
        private set => SetProperty(ref _selectedDay, value);
    }

    public ICommand PreviousMonthCommand { get; }

    public ICommand NextMonthCommand { get; }

    public ICommand SelectDayCommand { get; }

    private async Task PreviousMonthAsync()
    {
        _displayMonth = _displayMonth.AddMonths(-1);
        OnPropertyChanged(nameof(DisplayMonthLabel));
        await LoadMonthAsync();
    }

    private async Task NextMonthAsync()
    {
        _displayMonth = _displayMonth.AddMonths(1);
        OnPropertyChanged(nameof(DisplayMonthLabel));
        await LoadMonthAsync();
    }

    private async Task LoadMonthAsync()
    {
        var firstVisibleDay = _displayMonth.AddDays(-(int)_displayMonth.DayOfWeek);
        var lastVisibleDay = firstVisibleDay.AddDays(41);

        List<TradingJournal.Core.Domain.Trade> trades = [];

        if (_activeAccountService.HasActiveAccount)
        {
            using var dbContext = new AppDbContext(AppDbContextOptionsFactory.Create());
            trades = await dbContext.Trades
                .AsNoTracking()
                .Where(x => x.AccountId == _activeAccountService.ActiveAccountId)
                .Where(x => x.EntryTime >= firstVisibleDay && x.EntryTime <= lastVisibleDay)
                .OrderBy(x => x.EntryTime)
                .ToListAsync();
        }

        Days.Clear();

        for (var day = firstVisibleDay; day <= lastVisibleDay; day = day.AddDays(1))
        {
            var dayTrades = trades.Where(x => x.EntryTime.Date == day.Date).ToList();
            Days.Add(new CalendarDaySummary
            {
                Date = DateOnly.FromDateTime(day),
                IsCurrentMonth = day.Month == _displayMonth.Month,
                TotalPnL = dayTrades.Sum(x => x.NetPnL),
                TradeCount = dayTrades.Count
            });
        }

        SelectedDay = null;
        SelectedDayTrades.Clear();
    }

    private async Task SelectDayAsync(CalendarDaySummary? day)
    {
        if (day is null || !_activeAccountService.HasActiveAccount)
        {
            SelectedDayTrades.Clear();
            return;
        }

        SelectedDay = day;

        using var dbContext = new AppDbContext(AppDbContextOptionsFactory.Create());
        var trades = await dbContext.Trades
            .AsNoTracking()
            .Where(x => x.AccountId == _activeAccountService.ActiveAccountId)
            .Where(x => x.EntryTime.Date == day.Date.ToDateTime(TimeOnly.MinValue).Date)
            .OrderByDescending(x => x.EntryTime)
            .ToListAsync();

        SelectedDayTrades.Clear();

        foreach (var trade in trades.Select(static trade => TradeViewModel.FromTrade(trade)))
        {
            SelectedDayTrades.Add(trade);
        }
    }

    private void HandleActiveAccountChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is not nameof(ActiveAccountService.ActiveAccountId)
            and not nameof(ActiveAccountService.ActiveAccountDisplay))
        {
            return;
        }

        OnPropertyChanged(nameof(ActiveAccountDisplay));
        _ = LoadMonthAsync();
    }
}
