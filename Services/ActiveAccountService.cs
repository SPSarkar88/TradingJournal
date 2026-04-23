using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.EntityFrameworkCore;
using TradingJournal.Core.Domain;
using TradingJournal.Infrastructure.Persistence;

namespace TradingJournal.Services;

public sealed class ActiveAccountService : ObservableObject
{
    private Guid? _activeAccountId;
    private string _activeAccountName = string.Empty;
    private string _activeAccountBroker = string.Empty;

    public ActiveAccountService()
    {
        LoadInitialAccount();
    }

    public Guid? ActiveAccountId
    {
        get => _activeAccountId;
        private set
        {
            if (!SetProperty(ref _activeAccountId, value))
            {
                return;
            }

            OnPropertyChanged(nameof(HasActiveAccount));
            OnPropertyChanged(nameof(ActiveAccountDisplay));
        }
    }

    public bool HasActiveAccount => ActiveAccountId.HasValue;

    public string ActiveAccountDisplay =>
        HasActiveAccount
            ? $"{_activeAccountName} ({_activeAccountBroker})"
            : "No active account selected";

    public async Task RefreshAsync(Guid? preferredAccountId = null, CancellationToken cancellationToken = default)
    {
        using var dbContext = new AppDbContext(AppDbContextOptionsFactory.Create());
        var accounts = await dbContext.Accounts
            .AsNoTracking()
            .Include(x => x.Trades)
            .ToListAsync(cancellationToken);

        var selectedAccount = preferredAccountId.HasValue
            ? accounts.FirstOrDefault(x => x.Id == preferredAccountId.Value)
            : null;

        selectedAccount ??= ActiveAccountId.HasValue
            ? accounts.FirstOrDefault(x => x.Id == ActiveAccountId.Value)
            : null;

        selectedAccount ??= SelectDefaultAccount(accounts);
        Apply(selectedAccount);
    }

    public async Task SetActiveAccountAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        using var dbContext = new AppDbContext(AppDbContextOptionsFactory.Create());
        var account = await dbContext.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == accountId, cancellationToken);

        Apply(account);
    }

    private void LoadInitialAccount()
    {
        using var dbContext = new AppDbContext(AppDbContextOptionsFactory.Create());
        var accounts = dbContext.Accounts
            .AsNoTracking()
            .Include(x => x.Trades)
            .ToList();

        Apply(SelectDefaultAccount(accounts));
    }

    private static Account? SelectDefaultAccount(IEnumerable<Account> accounts)
    {
        return accounts
            .OrderByDescending(x => x.Trades.Count)
            .ThenByDescending(x => x.Trades.Count == 0 ? DateTime.MinValue : x.Trades.Max(t => t.EntryTime))
            .ThenBy(x => x.Name)
            .FirstOrDefault();
    }

    private void Apply(Account? account)
    {
        _activeAccountName = account?.Name ?? string.Empty;
        _activeAccountBroker = account?.Broker ?? string.Empty;
        ActiveAccountId = account?.Id;
        OnPropertyChanged(nameof(ActiveAccountDisplay));
    }
}
