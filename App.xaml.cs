using System.IO;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using TradingJournal.Infrastructure.Persistence;

namespace TradingJournal;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        using var dbContext = new AppDbContext(AppDbContextOptionsFactory.Create());
#if DEBUG
        dbContext.Database.Migrate();
        if (e.Args.Any(arg => arg.Equals("--seed-demo-data", StringComparison.OrdinalIgnoreCase)))
        {
            var summary = AppDataSeeder.SeedAsync(dbContext).GetAwaiter().GetResult();
            var reportPath = Path.Combine(DatabasePaths.DataDirectory, "seed-report.txt");
            File.WriteAllText(reportPath, summary.ToConsoleText());
            Console.WriteLine(summary.ToConsoleText());
            Shutdown();
            return;
        }
#else
        dbContext.Database.EnsureCreated();
#endif
        

        base.OnStartup(e);
    }
}
