using Microsoft.EntityFrameworkCore;
using TradingJournal.Core.Domain;
using TradingJournal.Core.Interfaces;
using TradingJournal.Core.Models;
using TradingJournal.Infrastructure.Persistence;

namespace TradingJournal.Infrastructure.Repositories;

public sealed class TradeRepository(AppDbContext dbContext)
    : GenericRepository<Trade>(dbContext), ITradeRepository
{
    public async Task<IReadOnlyList<Trade>> GetRecentAsync(int limit = 25, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Include(x => x.Account)
            .Include(x => x.Strategy)
            .OrderByDescending(x => x.EntryTime)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Trade>> GetFilteredAsync(
        TradeQueryOptions queryOptions,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .AsNoTracking()
            .Include(x => x.Account)
            .Include(x => x.Strategy)
            .AsQueryable();

        if (queryOptions.RequireAccountScope)
        {
            query = queryOptions.AccountId.HasValue
                ? query.Where(x => x.AccountId == queryOptions.AccountId.Value)
                : query.Where(_ => false);
        }
        else if (queryOptions.AccountId.HasValue)
        {
            query = query.Where(x => x.AccountId == queryOptions.AccountId.Value);
        }

        if (!string.IsNullOrWhiteSpace(queryOptions.Symbol))
        {
            query = query.Where(x => x.Symbol.Contains(queryOptions.Symbol));
        }

        if (!string.IsNullOrWhiteSpace(queryOptions.StrategyTag))
        {
            query = query.Where(x => x.StrategyTag == queryOptions.StrategyTag);
        }

        if (!string.IsNullOrWhiteSpace(queryOptions.Direction))
        {
            query = query.Where(x => x.Direction == queryOptions.Direction);
        }

        if (!string.IsNullOrWhiteSpace(queryOptions.TradeType))
        {
            query = query.Where(x => x.TradeType == queryOptions.TradeType);
        }

        if (queryOptions.FromEntryTime.HasValue)
        {
            query = query.Where(x => x.EntryTime >= queryOptions.FromEntryTime.Value);
        }

        if (queryOptions.ToEntryTime.HasValue)
        {
            query = query.Where(x => x.EntryTime <= queryOptions.ToEntryTime.Value);
        }

        if (queryOptions.IsWinningTrade.HasValue)
        {
            query = queryOptions.IsWinningTrade.Value
                ? query.Where(x => x.NetPnL > 0)
                : query.Where(x => x.NetPnL <= 0);
        }

        query = ApplySorting(query, queryOptions);

        return await query.ToListAsync(cancellationToken);
    }

    private static IQueryable<Trade> ApplySorting(IQueryable<Trade> query, TradeQueryOptions queryOptions)
    {
        return (queryOptions.SortBy, queryOptions.Descending) switch
        {
            (TradeSortField.Symbol, true) => query.OrderByDescending(x => x.Symbol),
            (TradeSortField.Symbol, false) => query.OrderBy(x => x.Symbol),
            (TradeSortField.ExitTime, true) => query.OrderByDescending(x => x.ExitTime),
            (TradeSortField.ExitTime, false) => query.OrderBy(x => x.ExitTime),
            (TradeSortField.Quantity, true) => query.OrderByDescending(x => x.Quantity),
            (TradeSortField.Quantity, false) => query.OrderBy(x => x.Quantity),
            (TradeSortField.NetPnL, true) => query.OrderByDescending(x => x.NetPnL),
            (TradeSortField.NetPnL, false) => query.OrderBy(x => x.NetPnL),
            (TradeSortField.EntryTime, false) => query.OrderBy(x => x.EntryTime),
            _ => query.OrderByDescending(x => x.EntryTime)
        };
    }
}
