namespace TradingJournal.ViewModels;

public sealed class TradeDetailViewModel : ViewModelBase
{
    private TradeViewModel? _selectedTrade;

    public TradeViewModel? SelectedTrade
    {
        get => _selectedTrade;
        private set
        {
            if (!SetProperty(ref _selectedTrade, value))
            {
                return;
            }

            OnPropertyChanged(nameof(HasTrade));
            OnPropertyChanged(nameof(ImagePreviewPath));
        }
    }

    public bool HasTrade => SelectedTrade is not null;

    public string ImagePreviewPath => SelectedTrade?.ScreenshotPath ?? string.Empty;

    public void LoadTrade(TradeViewModel? trade)
    {
        SelectedTrade = trade;
    }
}
