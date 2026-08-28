using Microsoft.Extensions.Configuration;

namespace RIIS.Academic.Infrastructure.Persistence;

public static class ConnectionStringHelper
{
    public static string GetRiisConnectionString(IConfiguration configuration)
    {
        var env = configuration["envval"];

        var connectionName = string.Equals(env, "dev", StringComparison.OrdinalIgnoreCase)
            ? "RiisSqlServer"
            : "OtherConnection";

        return configuration.GetConnectionString(connectionName)
            ?? throw new InvalidOperationException($"Connection string '{connectionName}' not found.");
    }
}
