using Microsoft.Extensions.Configuration;

namespace RIIS.Academic.Infrastructure.Persistence;

public static class RiisDatabaseProvider
{
    public const string SqlServer = "SqlServer";
    public const string Sqlite = "Sqlite";

    public static string Resolve(IConfiguration configuration)
    {
        var provider = configuration["DatabaseProvider"];

        if (string.IsNullOrWhiteSpace(provider))
        {
            return SqlServer;
        }

        if (string.Equals(provider, SqlServer, StringComparison.OrdinalIgnoreCase))
        {
            return SqlServer;
        }

        if (string.Equals(provider, Sqlite, StringComparison.OrdinalIgnoreCase))
        {
            return Sqlite;
        }

        throw new InvalidOperationException(
            $"DatabaseProvider '{provider}' est invalide. Valeurs supportees : {SqlServer}, {Sqlite}.");
    }

    public static bool IsSqlite(IConfiguration configuration)
        => string.Equals(Resolve(configuration), Sqlite, StringComparison.OrdinalIgnoreCase);
}
