using TradingJournal.Services;

namespace TradingJournal.ViewModels;

public sealed class NavigationItemViewModel : ViewModelBase
{
    private bool _isSelected;

    public NavigationItemViewModel(string title, string description, NavigationTarget target)
    {
        Title = title;
        Description = description;
        Target = target;
    }

    public string Title { get; }

    public string Description { get; }

    public NavigationTarget Target { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}
