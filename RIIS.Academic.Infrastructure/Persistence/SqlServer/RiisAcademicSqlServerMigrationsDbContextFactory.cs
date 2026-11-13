using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using RIIS.Academic.Infrastructure.Persistence;

namespace RIIS.Academic.Infrastructure.Persistence.SqlServer;

public sealed class RiisAcademicSqlServerMigrationsDbContextFactory
    : IDesignTimeDbContextFactory<RiisAcademicSqlServerMigrationsDbContext>
{
    public RiisAcademicSqlServerMigrationsDbContext CreateDbContext(string[] args)
    {
        var configuration = DesignTimeConfiguration.Build();
        var connectionString = ConnectionStringHelper.GetRiisConnectionString(
            configuration,
            RiisDatabaseProvider.SqlServer);

        var optionsBuilder = new DbContextOptionsBuilder<RiisAcademicSqlServerMigrationsDbContext>();
        optionsBuilder.UseRiisSqlServer(connectionString);

        return new RiisAcademicSqlServerMigrationsDbContext(optionsBuilder.Options);
    }
}
