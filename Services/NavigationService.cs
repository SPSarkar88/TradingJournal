using CommunityToolkit.Mvvm.ComponentModel;
using TradingJournal.ViewModels;

namespace TradingJournal.Services;

public sealed class NavigationService : ObservableObject, INavigationService
{
    private readonly IReadOnlyDictionary<NavigationTarget, Func<ViewModelBase>> _viewModelFactories;
    private readonly ActiveAccountService _activeAccountService;
    private NavigationTarget _currentTarget;
    private ViewModelBase? _currentViewModel;

    public NavigationService()
    {
        _activeAccountService = new ActiveAccountService();
        _viewModelFactories = new Dictionary<NavigationTarget, Func<ViewModelBase>>
        {
            [NavigationTarget.Accounts] = () => new AccountViewModel(_activeAccountService),
            [NavigationTarget.Trades] = () => new TradesViewModel(_activeAccountService),
            [NavigationTarget.Analytics] = () => new DashboardViewModel(_activeAccountService),
            [NavigationTarget.Strategies] = () => new StrategyViewModel(_activeAccountService),
            [NavigationTarget.Journal] = () => new JournalViewModel(_activeAccountService),
            [NavigationTarget.Calendar] = () => new CalendarViewModel(_activeAccountService),
            [NavigationTarget.Import] = () => new ImportViewModel(_activeAccountService),
            [NavigationTarget.Rules] = () => new RulesViewModel()
        };
    }

    public ActiveAccountService ActiveAccountService => _activeAccountService;

    public ViewModelBase? CurrentViewModel
    {
        get => _currentViewModel;
        private set => SetProperty(ref _currentViewModel, value);
    }

    public NavigationTarget CurrentTarget
    {
        get => _currentTarget;
        private set => SetProperty(ref _currentTarget, value);
    }

    public void NavigateTo(NavigationTarget target)
    {
        if (!_viewModelFactories.TryGetValue(target, out var factory))
        {
            throw new InvalidOperationException($"No view model is registered for target '{target}'.");
        }

        CurrentTarget = target;
        CurrentViewModel = factory();
    }
}
