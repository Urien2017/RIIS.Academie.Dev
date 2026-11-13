using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace RIIS.Academic.Infrastructure.Persistence;

public class RiisAcademicDbContextFactory : IDesignTimeDbContextFactory<RiisAcademicDbContext>
{
    public RiisAcademicDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<RiisAcademicDbContext>();

        // Ce contexte conserve les anciennes migrations SQL Server.
        var configuration = DesignTimeConfiguration.Build();
        var connectionString = ConnectionStringHelper.GetRiisConnectionString(
            configuration,
            RiisDatabaseProvider.SqlServer);

        optionsBuilder.UseSqlServer(connectionString, options =>
        {
            options.MigrationsAssembly(typeof(RiisAcademicDbContext).Assembly.FullName);
        });

        return new RiisAcademicDbContext(optionsBuilder.Options);
    }
}
