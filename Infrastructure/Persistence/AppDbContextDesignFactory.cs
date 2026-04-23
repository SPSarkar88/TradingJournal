using Microsoft.EntityFrameworkCore.Design;

namespace TradingJournal.Infrastructure.Persistence;

public sealed class AppDbContextDesignFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        return new AppDbContext(AppDbContextOptionsFactory.Create());
    }
}
