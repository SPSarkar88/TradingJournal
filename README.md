# TradingJournal

TradingJournal is a desktop trading journal built with WPF and .NET 8. It helps track trades by account, review execution quality, capture pre-trade and post-trade notes, inspect performance trends, import broker CSV exports, and flag rule violations before trades are saved.

## What it does

TradingJournal currently includes:

- Account management with active-account switching
- Trade entry, editing, deletion, and detail review
- Filters for symbol, strategy, date range, and profit/loss outcome
- Performance analytics with total P&L, win rate, average profit/loss, average R-multiple, equity curve, and win/loss distribution
- Strategy-level performance summaries
- Per-trade journal notes
- Monthly trading calendar heatmap with day drill-down
- CSV preview and import workflow
- Rule checks for:
  - maximum trades per day
  - mandatory stop-loss validation
- SQLite persistence through EF Core

## Tech stack

- .NET 8
- WPF
- MVVM
- Entity Framework Core with SQLite
- CommunityToolkit.Mvvm
- LiveChartsCore
- Dapper

## Project structure

```text
TradingJournal/
|- Core/              Domain models, interfaces, business services, view models used by features
|- Infrastructure/    EF Core persistence, repositories, database factories, migrations
|- Services/          Navigation and active-account coordination
|- ViewModels/        Workspace and screen view models
|- Views/             WPF views and windows
|- Resources/         Shared styles, templates, and theme resources
|- Converters/        WPF value converters
|- Modules/           Workspace/module definitions
|- Data/              Local SQLite database file
```

## Requirements

- Windows
- .NET 8 SDK

This is a WPF application, so it is intended to run on Windows desktop environments.

## Getting started

1. Restore and build:

```powershell
dotnet restore
dotnet build TradingJournal.sln -v minimal
```

2. Run the app:

```powershell
dotnet run --project TradingJournal.csproj
```

On startup, the app initializes the local SQLite database in the repository's `Data` folder.

## Demo data

The app supports demo data seeding in Debug builds.

Run:

```powershell
dotnet run --project TradingJournal.csproj -- --seed-demo-data
```

That command:

- applies migrations in Debug
- seeds sample accounts, strategies, rules, trades, and journals
- writes a seed report to `Data/seed-report.txt`
- exits after seeding completes

## Data storage

TradingJournal stores data locally in:

```text
Data/TradingJournal.db
```

The app creates the `Data` directory automatically if it does not exist.

## Main workflows

### Accounts

- Create, edit, and delete brokerage accounts
- Switch the active account for the rest of the workspace
- The app automatically selects a default account based on existing trade activity

### Trades

- Capture symbol, entry/exit prices, quantity, direction, timestamps, stop-loss, fees, notes, and screenshot path
- Recalculate net P&L automatically
- Review trades in a sortable and filterable list
- Open trade entry in a dedicated window for new and existing trades

### Analytics

- Review headline performance metrics
- Inspect equity curve and win/loss distribution
- Compare recent daily and weekly results
- Evaluate strategy-level performance breakdowns

### Journal

- Attach pre-trade notes and post-trade review notes to individual trades
- Keep execution records and review notes tied together

### Calendar

- Review a monthly heatmap of trading outcomes
- Drill into trades for a selected day

### Import

- Select a CSV file
- Preview mapped rows before import
- Adjust expected column names in the UI if broker headers differ
- Import trades into the currently active account

### Rules

- Configure active trading guardrails
- Surface violations in the trade list
- Prompt before saving trades that violate active rules

## CSV import expectations

The default CSV mapping expects these required columns:

- `Symbol`
- `EntryPrice`
- `Quantity`
- `TradeType`
- `Direction`
- `EntryTime`

Optional supported columns include:

- `ExitPrice`
- `ExitTime`
- `StopLossPrice`
- `Brokerage`
- `Taxes`
- `StrategyTag`
- `Notes`
- `ScreenshotPath`

## Development notes

- The app uses EF Core migrations stored in `Infrastructure/Persistence/Migrations`.
- `Trade.NetPnL` is recalculated automatically before database saves.
- In Debug builds, startup runs database migrations automatically.
- In non-Debug builds, the app uses `EnsureCreated()`.

## Current limitations

- There is no automated test project in the repository yet.
- Build output currently shows `NU1701` warnings for some charting-related packages restored from older .NET Framework-targeted assets.
- The application currently stores data in a repo-local SQLite database rather than a user-profile app data location.

## Release notes

See [RELEASE_NOTES.md](./RELEASE_NOTES.md) for the current release summary and release title.
