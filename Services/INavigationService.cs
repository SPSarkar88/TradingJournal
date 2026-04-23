using System.ComponentModel;
using TradingJournal.ViewModels;

namespace TradingJournal.Services;

public interface INavigationService : INotifyPropertyChanged
{
    ViewModelBase? CurrentViewModel { get; }

    NavigationTarget CurrentTarget { get; }

    ActiveAccountService ActiveAccountService { get; }

    void NavigateTo(NavigationTarget target);
}
