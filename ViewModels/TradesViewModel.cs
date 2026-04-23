using System.Linq;
using System.Windows;
using TradingJournal.Services;
using TradingJournal.Views;

namespace TradingJournal.ViewModels;

public sealed class TradesViewModel : WorkspaceViewModelBase
{
    private readonly ActiveAccountService _activeAccountService;

    public TradesViewModel(ActiveAccountService activeAccountService)
        : base(
            "Trades",
            "Capture, review, edit, and organize executions from one workspace.",
            "The trade workspace keeps the journal list visible and opens entry in a dedicated window.",
            [])
    {
        _activeAccountService = activeAccountService;
        ListingViewModel = new TradeListingViewModel(activeAccountService);
        DetailViewModel = new TradeDetailViewModel();

        ListingViewModel.NewTradeRequestedCallback = () => OpenTradeEntryWindow();
        ListingViewModel.EditRequestedCallback = trade => OpenTradeEntryWindow(trade);
        ListingViewModel.TradeSelectedCallback = trade =>
        {
            DetailViewModel.LoadTrade(trade);
        };

        _ = ListingViewModel.LoadTradesAsync();
    }

    public TradeListingViewModel ListingViewModel { get; }

    public TradeDetailViewModel DetailViewModel { get; }

    private void OpenTradeEntryWindow(TradeViewModel? trade = null)
    {
        var entryViewModel = new TradeEntryViewModel(_activeAccountService)
        {
            CloseAfterSave = true
        };

        entryViewModel.TradeSavedCallback = async savedTrade =>
        {
            await ListingViewModel.LoadTradesAsync();
            ListingViewModel.StatusMessage = $"Saved trade '{savedTrade.Symbol}'.";
            DetailViewModel.LoadTrade(savedTrade);
        };

        entryViewModel.TradeLoadedCallback = loadedTrade =>
        {
            if (loadedTrade is not null)
            {
                DetailViewModel.LoadTrade(loadedTrade);
            }
        };

        if (trade is not null)
        {
            entryViewModel.LoadTrade(trade);
        }

        var window = new TradeEntryWindow
        {
            DataContext = entryViewModel,
            Owner = GetOwnerWindow()
        };

        entryViewModel.CloseRequested = () => window.Close();
        window.ShowDialog();
    }

    private static Window? GetOwnerWindow() =>
        Application.Current.Windows
            .OfType<Window>()
            .FirstOrDefault(window => window.IsActive)
        ?? Application.Current.MainWindow;
}
