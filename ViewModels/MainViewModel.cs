using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using TradingJournal.Services;

namespace TradingJournal.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly ActiveAccountService _activeAccountService;
    private string _statusMessage = "Ready";

    public MainViewModel()
        : this(new NavigationService())
    {
    }

    public MainViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
        _activeAccountService = navigationService.ActiveAccountService;
        _navigationService.PropertyChanged += HandleNavigationChanged;
        _activeAccountService.PropertyChanged += HandleActiveAccountChanged;

        NavigationItems = new ObservableCollection<NavigationItemViewModel>(
        [
            new("Accounts", "Create accounts and switch the active workspace scope.", NavigationTarget.Accounts),
            new("Trades", "Log entries, exits, and execution notes.", NavigationTarget.Trades),
            new("Analytics", "Review performance, drawdown, and trends.", NavigationTarget.Analytics),
            new("Strategies", "Manage setups and compare strategy-level results.", NavigationTarget.Strategies),
            new("Journal", "Capture pre-trade plans and post-trade reviews.", NavigationTarget.Journal),
            new("Calendar", "Inspect daily performance in a monthly heatmap.", NavigationTarget.Calendar),
            new("Import", "Preview and import CSV exports into the journal.", NavigationTarget.Import),
            new("Rules", "Define trading guardrails and review violations.", NavigationTarget.Rules)
        ]);

        NavigateCommand = new RelayCommand<NavigationTarget>(target => Navigate(target));
        RefreshCommand = new RelayCommand(() => UpdateStatus($"Workspace refreshed at {DateTime.Now:t}."));

        Navigate(_activeAccountService.HasActiveAccount ? NavigationTarget.Trades : NavigationTarget.Accounts);
    }

    public string Title => "Trading Journal";

    public string ShellTitle => "Trading Journal";

    public string ShellSubtitle => "";

    public string ActiveAccountDisplay => _activeAccountService.ActiveAccountDisplay;

    public ObservableCollection<NavigationItemViewModel> NavigationItems { get; }

    public WorkspaceViewModelBase? CurrentWorkspace => _navigationService.CurrentViewModel as WorkspaceViewModelBase;

    public ICommand NavigateCommand { get; }

    public ICommand RefreshCommand { get; }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    private void Navigate(NavigationTarget target)
    {
        _navigationService.NavigateTo(target);
        UpdateSelection(target);

        if (CurrentWorkspace is not null)
        {
            UpdateStatus($"{CurrentWorkspace.Title} workspace loaded.");
        }
    }

    private void HandleNavigationChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(INavigationService.CurrentViewModel))
        {
            return;
        }

        OnPropertyChanged(nameof(CurrentWorkspace));
    }

    private void HandleActiveAccountChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is not nameof(ActiveAccountService.ActiveAccountDisplay)
            and not nameof(ActiveAccountService.ActiveAccountId))
        {
            return;
        }

        OnPropertyChanged(nameof(ActiveAccountDisplay));
    }

    private void UpdateSelection(NavigationTarget activeTarget)
    {
        foreach (var item in NavigationItems)
        {
            item.IsSelected = item.Target == activeTarget;
        }
    }

    private void UpdateStatus(string message)
    {
        StatusMessage = message;
    }
}
