using Microsoft.Extensions.Configuration;

namespace RIIS.Academic.Infrastructure.Persistence;

public static class ConnectionStringHelper
{
    private const string ProductionEnvironment = "prod";

    public static string GetRiisConnectionString(IConfiguration configuration)
        => GetRiisConnectionString(configuration, RiisDatabaseProvider.Resolve(configuration));

    public static string GetRiisConnectionString(IConfiguration configuration, string provider)
    {
        var connectionName = ResolveConnectionName(configuration, provider);

        return configuration.GetConnectionString(connectionName)
            ?? throw new InvalidOperationException(
                $"La chaine de connexion '{connectionName}' est introuvable pour le provider '{provider}'.");
    }

    private static string ResolveConnectionName(IConfiguration configuration, string provider)
    {
        if (string.Equals(provider, RiisDatabaseProvider.Sqlite, StringComparison.OrdinalIgnoreCase)) 
        {
            return "RiisSqlite";
        }

        return string.Equals(
            configuration["envval"],
            ProductionEnvironment,
            StringComparison.OrdinalIgnoreCase)
            ? "RiisOnlineConnection"
            : "RiisSqlServer";
    }
}
