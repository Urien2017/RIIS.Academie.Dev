using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace RIIS.Academic.Infrastructure.Persistence;

public class RiisAcademicDbContextFactory : IDesignTimeDbContextFactory<RiisAcademicDbContext>
{
    public RiisAcademicDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<RiisAcademicDbContext>();

        // Construire la configuration
        var environmentName = Environment.GetEnvironmentVariable("envval") ?? "dev";

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = ConnectionStringHelper.GetRiisConnectionString(configuration);

        optionsBuilder.UseSqlServer(connectionString, options =>
        {
            options.MigrationsAssembly(typeof(RiisAcademicDbContext).Assembly.FullName);
        });

        return new RiisAcademicDbContext(optionsBuilder.Options);
    }
}