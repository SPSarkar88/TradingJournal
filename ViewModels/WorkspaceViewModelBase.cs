namespace TradingJournal.ViewModels;

public abstract class WorkspaceViewModelBase : ViewModelBase
{
    protected WorkspaceViewModelBase(
        string title,
        string subtitle,
        string overview,
        IReadOnlyList<string> highlights)
    {
        Title = title;
        Subtitle = subtitle;
        Overview = overview;
        Highlights = highlights;
    }

    public string Title { get; }

    public string Subtitle { get; }

    public string Overview { get; }

    public IReadOnlyList<string> Highlights { get; }
}
