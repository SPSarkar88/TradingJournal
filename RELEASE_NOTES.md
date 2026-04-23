# Release Title

TradingJournal v0.1.0 - Desktop Trading Journal Foundation

# Release Notes

## Summary

This release establishes the first complete desktop foundation for TradingJournal: a WPF-based trading journal with account-scoped workflows, trade capture, journaling, analytics, calendar review, CSV import, and rule-driven trade review backed by SQLite persistence.

## Highlights

### Desktop workspace shell

- Added a .NET 8 WPF application shell with sidebar navigation and dedicated workspaces for Accounts, Trades, Analytics, Strategies, Journal, Calendar, Import, and Rules.
- Added shared styling, control templates, converters, and MVVM infrastructure for a consistent desktop workflow.

### Account-scoped trading workflow

- Added account management with create, edit, delete, and active-account switching flows.
- Scoped the main workspace experience to the currently active account so trade review, journaling, analytics, calendar data, and imports stay tied to the selected trading account.

### Trade capture and review

- Added trade entry and edit flows with support for symbol, prices, quantity, direction, trade type, timestamps, stop-loss, brokerage, taxes, strategy tag, notes, and screenshot path.
- Added trade listing with filtering by symbol, strategy, date range, and profitability, plus sortable columns and delete support.
- Added trade detail viewing and automatic net P&L recalculation through the persistence layer.

### Analytics and performance review

- Added an analytics dashboard with total P&L, win rate, average profit, average loss, and average R-multiple.
- Added equity curve and win/loss chart visualizations.
- Added daily and weekly summary rollups for recent performance review.
- Added strategy-level performance analysis so traders can compare which setups are working.

### Journal and calendar review

- Added linked journal entries for each trade with pre-trade notes and post-trade review fields.
- Added a monthly calendar heatmap view with per-day P&L and trade count summaries.
- Added drill-down from calendar days into the trades that contributed to a selected session.

### Import and rule enforcement

- Added CSV validation, preview, mapping, and import flows for broker exports.
- Added trading rule support for maximum trades per day and mandatory stop-loss validation.
- Added rule violation prompts during trade save and surfaced violation summaries in the trade list.

### Data and persistence

- Added EF Core SQLite persistence, migrations, repository implementations, and domain models for trades, journals, strategies, accounts, and trading rules.
- Added startup database initialization and a demo data seeding path with sample accounts, strategies, rules, trades, and journal entries.
- Added a local SQLite database file under `Data/TradingJournal.db`.

## Technical Notes

- Target framework: `net8.0-windows`
- UI architecture: WPF with MVVM
- Persistence: EF Core with SQLite
- Key libraries: `CommunityToolkit.Mvvm`, `Dapper`, `LiveChartsCore.SkiaSharpView.WPF`

## Verification

- `dotnet build TradingJournal.sln -v minimal` completed successfully on April 23, 2026.

## Known Issues / Follow-up

- The build completes with `NU1701` compatibility warnings for charting-related packages restored from .NET Framework-targeted assets.
- No automated test project is present yet, so this release is validated primarily through build success and code inspection.
