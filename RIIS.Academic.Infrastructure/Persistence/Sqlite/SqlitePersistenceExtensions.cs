using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RIIS.Academic.Infrastructure.Persistence;

namespace RIIS.Academic.Infrastructure.Persistence.Sqlite;

public static class SqlitePersistenceExtensions
{
    public static DbContextOptionsBuilder UseRiisSqlite(
        this DbContextOptionsBuilder optionsBuilder,
        string connectionString,
        string? runtimeBaseDirectory = null)
    {
        var normalizedConnectionString = NormalizeConnectionString(
            connectionString,
            runtimeBaseDirectory);
        EnsureDatabaseDirectory(normalizedConnectionString);

        return optionsBuilder.UseSqlite(
            normalizedConnectionString,
            sqlite => sqlite.MigrationsAssembly(typeof(RiisAcademicDbContext).Assembly.FullName));
    }

    private static string NormalizeConnectionString(
        string connectionString,
        string? runtimeBaseDirectory)
    {
        var builder = new SqliteConnectionStringBuilder(connectionString);
        var dataSource = builder.DataSource;

        if (string.IsNullOrWhiteSpace(runtimeBaseDirectory)
            || string.IsNullOrWhiteSpace(dataSource)
            || string.Equals(dataSource, ":memory:", StringComparison.OrdinalIgnoreCase)
            || Path.IsPathRooted(dataSource))
        {
            return connectionString;
        }

        builder.DataSource = Path.Combine(runtimeBaseDirectory, dataSource);
        return builder.ToString();
    }

    private static void EnsureDatabaseDirectory(string connectionString)
    {
        var dataSource = new SqliteConnectionStringBuilder(connectionString).DataSource;
        if (string.IsNullOrWhiteSpace(dataSource) || string.Equals(dataSource, ":memory:", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var directory = Path.GetDirectoryName(Path.GetFullPath(dataSource));
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
}
