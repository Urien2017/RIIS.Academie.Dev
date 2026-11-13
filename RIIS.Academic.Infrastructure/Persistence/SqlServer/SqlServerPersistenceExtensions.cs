using Microsoft.EntityFrameworkCore;
using RIIS.Academic.Infrastructure.Persistence;

namespace RIIS.Academic.Infrastructure.Persistence.SqlServer;

public static class SqlServerPersistenceExtensions
{
    public static DbContextOptionsBuilder UseRiisSqlServer(
        this DbContextOptionsBuilder optionsBuilder,
        string connectionString)
    {
        return optionsBuilder.UseSqlServer(
            connectionString,
            sql => sql.MigrationsAssembly(typeof(RiisAcademicDbContext).Assembly.FullName));
    }
}
