using System.IO;

namespace TradingJournal.Infrastructure.Persistence;

public static class DatabasePaths
{
    private static readonly string ProjectRoot = ResolveProjectRoot();

    public static string DataDirectory => Path.Combine(ProjectRoot, "Data");

    public static string DatabaseFilePath => Path.Combine(DataDirectory, "TradingJournal.db");

    public static string ConnectionString => $"Data Source={DatabaseFilePath}";

    public static void EnsureDataDirectoryExists()
    {
        Directory.CreateDirectory(DataDirectory);
    }

    private static string ResolveProjectRoot()
    {
        var candidate = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
        return File.Exists(Path.Combine(candidate, "TradingJournal.csproj"))
            ? candidate
            : AppContext.BaseDirectory;
    }
}
