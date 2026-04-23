using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using Microsoft.Win32;
using TradingJournal.Core.Models;
using TradingJournal.Core.Services;
using TradingJournal.Infrastructure.Persistence;
using TradingJournal.Infrastructure.Repositories;
using TradingJournal.Services;

namespace TradingJournal.ViewModels;

public sealed class ImportViewModel : WorkspaceViewModelBase
{
    private readonly ActiveAccountService _activeAccountService;
    private readonly ImportService _importService;
    private string _selectedFilePath = string.Empty;
    private string _symbolColumn = "Symbol";
    private string _entryPriceColumn = "EntryPrice";
    private string _exitPriceColumn = "ExitPrice";
    private string _quantityColumn = "Quantity";
    private string _tradeTypeColumn = "TradeType";
    private string _directionColumn = "Direction";
    private string _entryTimeColumn = "EntryTime";
    private string _statusMessage = "Select a CSV file to preview and import.";

    public ImportViewModel(ActiveAccountService activeAccountService)
        : base(
            "Import",
            "Validate broker exports before adding them to the journal.",
            "Preview rows, adjust column names if needed, and confirm the import into SQLite.",
            [])
    {
        _activeAccountService = activeAccountService;
        _activeAccountService.PropertyChanged += HandleActiveAccountChanged;
        PreviewRows = new ObservableCollection<ImportPreviewRow>();
        _importService = new ImportService();
        SelectFileCommand = new RelayCommand(SelectFile);
        PreviewCommand = new AsyncRelayCommand(LoadPreviewAsync, () => !string.IsNullOrWhiteSpace(SelectedFilePath));
        ImportCommand = new AsyncRelayCommand(ImportAsync, () => !string.IsNullOrWhiteSpace(SelectedFilePath) && _activeAccountService.HasActiveAccount);
    }

    public ObservableCollection<ImportPreviewRow> PreviewRows { get; }

    public string SelectedFilePath
    {
        get => _selectedFilePath;
        set
        {
            if (SetProperty(ref _selectedFilePath, value))
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public string SymbolColumn
    {
        get => _symbolColumn;
        set => SetProperty(ref _symbolColumn, value);
    }

    public string EntryPriceColumn
    {
        get => _entryPriceColumn;
        set => SetProperty(ref _entryPriceColumn, value);
    }

    public string ExitPriceColumn
    {
        get => _exitPriceColumn;
        set => SetProperty(ref _exitPriceColumn, value);
    }

    public string QuantityColumn
    {
        get => _quantityColumn;
        set => SetProperty(ref _quantityColumn, value);
    }

    public string TradeTypeColumn
    {
        get => _tradeTypeColumn;
        set => SetProperty(ref _tradeTypeColumn, value);
    }

    public string DirectionColumn
    {
        get => _directionColumn;
        set => SetProperty(ref _directionColumn, value);
    }

    public string EntryTimeColumn
    {
        get => _entryTimeColumn;
        set => SetProperty(ref _entryTimeColumn, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string ActiveAccountDisplay => _activeAccountService.ActiveAccountDisplay;

    public ICommand SelectFileCommand { get; }

    public ICommand PreviewCommand { get; }

    public ICommand ImportCommand { get; }

    private void SelectFile()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "CSV Files|*.csv",
            Title = "Select CSV File"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        SelectedFilePath = dialog.FileName;
        StatusMessage = "CSV selected. Preview it before importing.";
    }

    private async Task LoadPreviewAsync()
    {
        PreviewRows.Clear();

        if (!File.Exists(SelectedFilePath))
        {
            StatusMessage = "Selected file could not be found.";
            return;
        }

        var lines = await File.ReadAllLinesAsync(SelectedFilePath);
        if (lines.Length == 0)
        {
            StatusMessage = "CSV file is empty.";
            return;
        }

        var headers = lines[0]
            .Split(',', StringSplitOptions.TrimEntries)
            .ToList();
        var validation = _importService.ValidateCSV(headers, BuildMapping());
        if (!validation.IsValid)
        {
            StatusMessage = string.Join(" ", validation.Errors);
            return;
        }

        for (var index = 1; index < Math.Min(lines.Length, 6); index++)
        {
            var values = lines[index]
                .Split(',')
                .ToList();
            PreviewRows.Add(new ImportPreviewRow
            {
                RowNumber = index,
                Symbol = ValueFor(headers, values, SymbolColumn),
                EntryPrice = ValueFor(headers, values, EntryPriceColumn),
                ExitPrice = ValueFor(headers, values, ExitPriceColumn),
                Quantity = ValueFor(headers, values, QuantityColumn),
                TradeType = ValueFor(headers, values, TradeTypeColumn),
                Direction = ValueFor(headers, values, DirectionColumn),
                EntryTime = ValueFor(headers, values, EntryTimeColumn)
            });
        }

        StatusMessage = $"{PreviewRows.Count} preview rows loaded.";
    }

    private async Task ImportAsync()
    {
        if (!_activeAccountService.HasActiveAccount)
        {
            StatusMessage = "Select an active account before importing trades.";
            return;
        }

        using var dbContext = new AppDbContext(AppDbContextOptionsFactory.Create());
        var result = await new ImportService(new TradeRepository(dbContext))
            .ImportTradesFromCSV(SelectedFilePath, BuildMapping(), _activeAccountService.ActiveAccountId);
        StatusMessage = $"Imported {result.RowsImported} of {result.RowsProcessed} rows.";

        if (result.Errors.Count > 0)
        {
            StatusMessage += $" {result.Errors.Count} rows had issues.";
        }
    }

    private CsvColumnMapping BuildMapping()
    {
        return new CsvColumnMapping
        {
            Symbol = SymbolColumn,
            EntryPrice = EntryPriceColumn,
            ExitPrice = ExitPriceColumn,
            Quantity = QuantityColumn,
            TradeType = TradeTypeColumn,
            Direction = DirectionColumn,
            EntryTime = EntryTimeColumn
        };
    }

    private static string ValueFor(IReadOnlyList<string> headers, IReadOnlyList<string> values, string columnName)
    {
        var index = headers
            .Select((header, position) => new { header, position })
            .FirstOrDefault(x => x.header.Equals(columnName, StringComparison.OrdinalIgnoreCase))
            ?.position;

        return index.HasValue && index.Value < values.Count ? values[index.Value] : string.Empty;
    }

    private void HandleActiveAccountChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is not nameof(ActiveAccountService.ActiveAccountId)
            and not nameof(ActiveAccountService.ActiveAccountDisplay))
        {
            return;
        }

        OnPropertyChanged(nameof(ActiveAccountDisplay));
        CommandManager.InvalidateRequerySuggested();
    }
}
