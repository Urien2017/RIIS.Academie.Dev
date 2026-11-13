using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using RIIS.Academic.Infrastructure.Persistence;

namespace RIIS.Academic.Infrastructure.Persistence.Sqlite;

public sealed class RiisAcademicSqliteMigrationsDbContextFactory
    : IDesignTimeDbContextFactory<RiisAcademicSqliteMigrationsDbContext>
{
    public RiisAcademicSqliteMigrationsDbContext CreateDbContext(string[] args)
    {
        var configuration = DesignTimeConfiguration.Build();
        var connectionString = ConnectionStringHelper.GetRiisConnectionString(
            configuration,
            RiisDatabaseProvider.Sqlite);

        var optionsBuilder = new DbContextOptionsBuilder<RiisAcademicSqliteMigrationsDbContext>();
        optionsBuilder.UseRiisSqlite(connectionString);

        return new RiisAcademicSqliteMigrationsDbContext(optionsBuilder.Options);
    }
}
