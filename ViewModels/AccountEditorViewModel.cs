using System.ComponentModel;
using System.Windows.Input;
using TradingJournal.Core.Domain;
using TradingJournal.Infrastructure.Persistence;
using TradingJournal.Services;

namespace TradingJournal.ViewModels;

public sealed class AccountEditorViewModel : ViewModelBase
{
    private readonly ActiveAccountService _activeAccountService;
    private Guid? _accountId;
    private string _name = string.Empty;
    private string _broker = string.Empty;
    private bool _isEditMode;
    private string _statusMessage = "Enter the account details and save to make it active.";

    public AccountEditorViewModel(ActiveAccountService activeAccountService, AccountListItemViewModel? account = null)
    {
        _activeAccountService = activeAccountService;
        _activeAccountService.PropertyChanged += HandleActiveAccountChanged;

        SaveCommand = new AsyncRelayCommand(SaveAsync, CanSave);
        NewCommand = new RelayCommand(StartNew);

        if (account is null)
        {
            StartNew();
        }
        else
        {
            LoadAccount(account);
        }
    }

    public Func<Guid, string, Task>? AccountSavedCallback { get; set; }

    public Action? CloseRequested { get; set; }

    public string FormTitle => IsEditMode ? "Edit Account" : "New Account";

    public string SaveButtonText => IsEditMode ? "Update Account" : "Save Account";

    public string ActiveAccountDisplay => _activeAccountService.ActiveAccountDisplay;

    public string Name
    {
        get => _name;
        set
        {
            if (SetProperty(ref _name, value))
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public string Broker
    {
        get => _broker;
        set
        {
            if (SetProperty(ref _broker, value))
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public bool IsEditMode
    {
        get => _isEditMode;
        private set
        {
            if (!SetProperty(ref _isEditMode, value))
            {
                return;
            }

            OnPropertyChanged(nameof(FormTitle));
            OnPropertyChanged(nameof(SaveButtonText));
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public ICommand SaveCommand { get; }

    public ICommand NewCommand { get; }

    private void LoadAccount(AccountListItemViewModel account)
    {
        _accountId = account.Id;
        Name = account.Name;
        Broker = account.Broker;
        IsEditMode = true;
        StatusMessage = $"Editing account '{account.Name}'.";
    }

    private void StartNew()
    {
        _accountId = null;
        Name = string.Empty;
        Broker = string.Empty;
        IsEditMode = false;
        StatusMessage = "Enter the account details and save to make it active.";
    }

    private bool CanSave() =>
        !string.IsNullOrWhiteSpace(Name) &&
        !string.IsNullOrWhiteSpace(Broker);

    private async Task SaveAsync()
    {
        using var dbContext = new AppDbContext(AppDbContextOptionsFactory.Create());
        var account = new Account
        {
            Id = _accountId ?? Guid.NewGuid(),
            Name = Name.Trim(),
            Broker = Broker.Trim()
        };

        string message;

        if (_accountId.HasValue)
        {
            dbContext.Accounts.Update(account);
            message = $"Updated account '{account.Name}'.";
        }
        else
        {
            await dbContext.Accounts.AddAsync(account);
            message = $"Added account '{account.Name}'.";
        }

        await dbContext.SaveChangesAsync();
        await _activeAccountService.RefreshAsync(account.Id);

        StatusMessage = message;

        if (AccountSavedCallback is not null)
        {
            await AccountSavedCallback(account.Id, message);
        }

        CloseRequested?.Invoke();
    }

    private void HandleActiveAccountChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is not nameof(ActiveAccountService.ActiveAccountId)
            and not nameof(ActiveAccountService.ActiveAccountDisplay))
        {
            return;
        }

        OnPropertyChanged(nameof(ActiveAccountDisplay));
    }
}
