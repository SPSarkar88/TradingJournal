using Microsoft.EntityFrameworkCore;

namespace TradingJournal.Infrastructure.Persistence;

public static class AppDbContextOptionsFactory
{
    public static DbContextOptions<AppDbContext> Create()
    {
        DatabasePaths.EnsureDataDirectoryExists();

        return new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(DatabasePaths.ConnectionString)
            .Options;
    }
}
