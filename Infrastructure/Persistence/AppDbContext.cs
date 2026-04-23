using Microsoft.EntityFrameworkCore;
using TradingJournal.Core.Domain;

namespace TradingJournal.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Trade> Trades => Set<Trade>();

    public DbSet<Strategy> Strategies => Set<Strategy>();

    public DbSet<Account> Accounts => Set<Account>();

    public DbSet<Journal> Journals => Set<Journal>();

    public DbSet<TradingRule> TradingRules => Set<TradingRule>();

    public override int SaveChanges()
    {
        ApplyDerivedValues();
        return base.SaveChanges();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyDerivedValues();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyDerivedValues();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        ApplyDerivedValues();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureTrade(modelBuilder);
        ConfigureStrategy(modelBuilder);
        ConfigureAccount(modelBuilder);
        ConfigureJournal(modelBuilder);
        ConfigureTradingRule(modelBuilder);
    }

    private void ApplyDerivedValues()
    {
        foreach (var entry in ChangeTracker.Entries<Trade>()
                     .Where(x => x.State is EntityState.Added or EntityState.Modified))
        {
            entry.Entity.RecalculateNetPnL();
        }
    }

    private static void ConfigureTrade(ModelBuilder modelBuilder)
    {
        var trade = modelBuilder.Entity<Trade>();

        trade.ToTable("Trades");
        trade.HasKey(x => x.Id);

        trade.Property(x => x.Symbol)
            .HasMaxLength(32)
            .IsRequired();

        trade.Property(x => x.EntryPrice)
            .HasPrecision(18, 4);

        trade.Property(x => x.ExitPrice)
            .HasPrecision(18, 4);

        trade.Property(x => x.Quantity)
            .HasPrecision(18, 4);

        trade.Property(x => x.Brokerage)
            .HasPrecision(18, 2);

        trade.Property(x => x.Taxes)
            .HasPrecision(18, 2);

        trade.Property(x => x.StopLossPrice)
            .HasPrecision(18, 4);

        trade.Property(x => x.NetPnL)
            .HasPrecision(18, 2);

        trade.Property(x => x.TradeType)
            .HasMaxLength(24)
            .IsRequired();

        trade.Property(x => x.Direction)
            .HasMaxLength(8)
            .IsRequired();

        trade.Property(x => x.StrategyTag)
            .HasMaxLength(64);

        trade.Property(x => x.Notes)
            .HasMaxLength(4000);

        trade.Property(x => x.ScreenshotPath)
            .HasMaxLength(512);

        trade.HasIndex(x => x.EntryTime);

        trade.HasOne(x => x.Strategy)
            .WithMany(x => x.Trades)
            .HasForeignKey(x => x.StrategyId)
            .OnDelete(DeleteBehavior.SetNull);

        trade.HasOne(x => x.Account)
            .WithMany(x => x.Trades)
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.SetNull);
    }

    private static void ConfigureStrategy(ModelBuilder modelBuilder)
    {
        var strategy = modelBuilder.Entity<Strategy>();

        strategy.ToTable("Strategies");
        strategy.HasKey(x => x.Id);

        strategy.Property(x => x.Name)
            .HasMaxLength(64)
            .IsRequired();

        strategy.Property(x => x.Description)
            .HasMaxLength(512);

        strategy.HasIndex(x => x.Name)
            .IsUnique();
    }

    private static void ConfigureAccount(ModelBuilder modelBuilder)
    {
        var account = modelBuilder.Entity<Account>();

        account.ToTable("Accounts");
        account.HasKey(x => x.Id);

        account.Property(x => x.Name)
            .HasMaxLength(64)
            .IsRequired();

        account.Property(x => x.Broker)
            .HasMaxLength(64)
            .IsRequired();

        account.HasIndex(x => new { x.Name, x.Broker })
            .IsUnique();
    }

    private static void ConfigureJournal(ModelBuilder modelBuilder)
    {
        var journal = modelBuilder.Entity<Journal>();

        journal.ToTable("Journals");
        journal.HasKey(x => x.Id);

        journal.Property(x => x.PreTradeNotes)
            .HasMaxLength(4000);

        journal.Property(x => x.PostTradeReview)
            .HasMaxLength(4000);

        journal.HasIndex(x => x.TradeId)
            .IsUnique();

        journal.HasOne(x => x.Trade)
            .WithOne(x => x.Journal)
            .HasForeignKey<Journal>(x => x.TradeId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureTradingRule(ModelBuilder modelBuilder)
    {
        var rule = modelBuilder.Entity<TradingRule>();

        rule.ToTable("TradingRules");
        rule.HasKey(x => x.Id);

        rule.Property(x => x.Name)
            .HasMaxLength(80)
            .IsRequired();

        rule.Property(x => x.RuleType)
            .HasMaxLength(40)
            .IsRequired();

        rule.Property(x => x.Description)
            .HasMaxLength(512);

        rule.HasIndex(x => x.Name);
    }
}
