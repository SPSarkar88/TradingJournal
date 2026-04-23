using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using TradingJournal.Infrastructure.Persistence;
using TradingJournal.Services;
using TradingJournal.Views;

namespace TradingJournal.ViewModels;

public sealed class AccountViewModel : WorkspaceViewModelBase
{
    private readonly ActiveAccountService _activeAccountService;
    private AccountListItemViewModel? _selectedAccount;
    private string _statusMessage = "Manage accounts from the list and open the editor in a separate window.";

    public AccountViewModel(ActiveAccountService activeAccountService)
        : base(
            "Accounts",
            "Manage brokerage accounts and control which one the workspace is scoped to.",
            "Open create and edit flows in a dedicated window while keeping the account list visible.",
            [])
    {
        _activeAccountService = activeAccountService;
        _activeAccountService.PropertyChanged += HandleActiveAccountChanged;

        Accounts = new ObservableCollection<AccountListItemViewModel>();
        NewCommand = new RelayCommand(() => OpenAccountEditor());
        EditCommand = new RelayCommand(() => OpenAccountEditor(SelectedAccount), () => SelectedAccount is not null);
        DeleteCommand = new AsyncRelayCommand(DeleteAsync, () => SelectedAccount is not null);
        SetActiveCommand = new AsyncRelayCommand(SetActiveAsync, () => SelectedAccount is not null);
        RefreshCommand = new AsyncRelayCommand(LoadAsync);
        _ = LoadAsync();
    }

    public ObservableCollection<AccountListItemViewModel> Accounts { get; }

    public AccountListItemViewModel? SelectedAccount
    {
        get => _selectedAccount;
        set
        {
            if (!SetProperty(ref _selectedAccount, value))
            {
                return;
            }

            CommandManager.InvalidateRequerySuggested();
        }
    }

    public string ActiveAccountDisplay => _activeAccountService.ActiveAccountDisplay;

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public ICommand NewCommand { get; }

    public ICommand EditCommand { get; }

    public ICommand DeleteCommand { get; }

    public ICommand SetActiveCommand { get; }

    public ICommand RefreshCommand { get; }

    private async Task LoadAsync()
    {
        await LoadAsync(SelectedAccount?.Id);
    }

    private async Task LoadAsync(Guid? preferredSelectionId)
    {
        using var dbContext = new AppDbContext(AppDbContextOptionsFactory.Create());
        var accounts = await dbContext.Accounts
            .AsNoTracking()
            .Include(x => x.Trades)
            .OrderBy(x => x.Name)
            .ToListAsync();

        Accounts.Clear();

        AccountListItemViewModel? restoredSelection = null;

        foreach (var account in accounts)
        {
            var item = new AccountListItemViewModel
            {
                Id = account.Id,
                Name = account.Name,
                Broker = account.Broker,
                TradeCount = account.Trades.Count,
                IsActive = _activeAccountService.ActiveAccountId == account.Id
            };

            Accounts.Add(item);

            if (preferredSelectionId.HasValue && item.Id == preferredSelectionId.Value)
            {
                restoredSelection = item;
            }
        }

        SelectedAccount = restoredSelection;

        StatusMessage = Accounts.Count == 0
            ? "No accounts found yet. Add one to start journaling by account."
            : $"{Accounts.Count} accounts loaded. Active account: {ActiveAccountDisplay}.";
    }

    private void OpenAccountEditor(AccountListItemViewModel? account = null)
    {
        var editorViewModel = new AccountEditorViewModel(_activeAccountService, account);
        var window = new AccountEditorWindow
        {
            DataContext = editorViewModel,
            Owner = GetOwnerWindow()
        };

        editorViewModel.AccountSavedCallback = async (savedAccountId, message) =>
        {
            await LoadAsync(savedAccountId);
            StatusMessage = message;
        };

        editorViewModel.CloseRequested = () => window.Close();
        window.ShowDialog();
    }

    private async Task DeleteAsync()
    {
        if (SelectedAccount is null)
        {
            return;
        }

        var deletedAccountName = SelectedAccount.Name;

        var confirmation = MessageBox.Show(
            $"Delete account '{deletedAccountName}'?",
            "Delete Account",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirmation != MessageBoxResult.Yes)
        {
            return;
        }

        using var dbContext = new AppDbContext(AppDbContextOptionsFactory.Create());
        var account = await dbContext.Accounts.FirstOrDefaultAsync(x => x.Id == SelectedAccount.Id);
        if (account is null)
        {
            return;
        }

        dbContext.Accounts.Remove(account);
        await dbContext.SaveChangesAsync();
        await _activeAccountService.RefreshAsync();
        await LoadAsync();
        StatusMessage = $"Deleted account '{deletedAccountName}'.";
    }

    private async Task SetActiveAsync()
    {
        if (SelectedAccount is null)
        {
            return;
        }

        await _activeAccountService.SetActiveAccountAsync(SelectedAccount.Id);
        await LoadAsync(SelectedAccount.Id);
        StatusMessage = $"Active account switched to {ActiveAccountDisplay}.";
    }

    private void HandleActiveAccountChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is not nameof(ActiveAccountService.ActiveAccountId)
            and not nameof(ActiveAccountService.ActiveAccountDisplay))
        {
            return;
        }

        OnPropertyChanged(nameof(ActiveAccountDisplay));
        _ = LoadAsync(SelectedAccount?.Id);
    }

    private static Window? GetOwnerWindow() =>
        Application.Current.Windows
            .OfType<Window>()
            .FirstOrDefault(window => window.IsActive)
        ?? Application.Current.MainWindow;
}
