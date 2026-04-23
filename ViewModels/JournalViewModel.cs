using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using TradingJournal.Core.Domain;
using TradingJournal.Core.Models;
using TradingJournal.Infrastructure.Persistence;
using TradingJournal.Services;

namespace TradingJournal.ViewModels;

public sealed class JournalViewModel : WorkspaceViewModelBase
{
    private readonly ActiveAccountService _activeAccountService;
    private Guid? _selectedTradeId;
    private string _preTradeNotes = string.Empty;
    private string _postTradeReview = string.Empty;
    private string _statusMessage = "Select a trade to load or save journal notes.";

    public JournalViewModel(ActiveAccountService activeAccountService)
        : base(
            "Journal",
            "Capture structured pre-trade and post-trade thinking linked to each execution.",
            "Notes are stored per trade so the execution record and journal stay connected.",
            [])
    {
        _activeAccountService = activeAccountService;
        _activeAccountService.PropertyChanged += HandleActiveAccountChanged;
        Trades = new ObservableCollection<TradeSelectionItem>();
        SaveCommand = new AsyncRelayCommand(SaveAsync, CanSave);
        RefreshCommand = new AsyncRelayCommand(LoadAsync);
        LoadTradeCommand = new AsyncRelayCommand(LoadJournalAsync, () => SelectedTradeId.HasValue);
        _ = LoadAsync();
    }

    public ObservableCollection<TradeSelectionItem> Trades { get; }

    public Guid? SelectedTradeId
    {
        get => _selectedTradeId;
        set
        {
            if (SetProperty(ref _selectedTradeId, value))
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public string PreTradeNotes
    {
        get => _preTradeNotes;
        set => SetProperty(ref _preTradeNotes, value);
    }

    public string PostTradeReview
    {
        get => _postTradeReview;
        set => SetProperty(ref _postTradeReview, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string ActiveAccountDisplay => _activeAccountService.ActiveAccountDisplay;

    public ICommand SaveCommand { get; }

    public ICommand RefreshCommand { get; }

    public ICommand LoadTradeCommand { get; }

    public async Task LoadAsync()
    {
        if (!_activeAccountService.HasActiveAccount)
        {
            Trades.Clear();
            SelectedTradeId = null;
            PreTradeNotes = string.Empty;
            PostTradeReview = string.Empty;
            StatusMessage = "Select an active account to view journal entries.";
            return;
        }

        using var dbContext = new AppDbContext(AppDbContextOptionsFactory.Create());
        var trades = await dbContext.Trades
            .AsNoTracking()
            .Where(x => x.AccountId == _activeAccountService.ActiveAccountId)
            .OrderByDescending(x => x.EntryTime)
            .Select(x => new TradeSelectionItem(x.Id, $"{x.Symbol} - {x.EntryTime:dd MMM yyyy HH:mm}"))
            .ToListAsync();

        Trades.Clear();

        foreach (var trade in trades)
        {
            Trades.Add(trade);
        }

        StatusMessage = $"{Trades.Count} trades available for journaling in {_activeAccountService.ActiveAccountDisplay}.";
    }

    public async Task LoadJournalAsync()
    {
        if (!SelectedTradeId.HasValue)
        {
            return;
        }

        using var dbContext = new AppDbContext(AppDbContextOptionsFactory.Create());
        var journal = await dbContext.Journals
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TradeId == SelectedTradeId.Value);

        PreTradeNotes = journal?.PreTradeNotes ?? string.Empty;
        PostTradeReview = journal?.PostTradeReview ?? string.Empty;
        StatusMessage = "Journal loaded.";
    }

    private bool CanSave() => SelectedTradeId.HasValue;

    private async Task SaveAsync()
    {
        if (!SelectedTradeId.HasValue)
        {
            return;
        }

        using var dbContext = new AppDbContext(AppDbContextOptionsFactory.Create());
        var journal = await dbContext.Journals.FirstOrDefaultAsync(x => x.TradeId == SelectedTradeId.Value);

        if (journal is null)
        {
            journal = new Journal
            {
                TradeId = SelectedTradeId.Value
            };

            await dbContext.Journals.AddAsync(journal);
        }

        journal.PreTradeNotes = PreTradeNotes.Trim();
        journal.PostTradeReview = PostTradeReview.Trim();
        await dbContext.SaveChangesAsync();
        StatusMessage = "Journal saved.";
    }

    private void HandleActiveAccountChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
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
