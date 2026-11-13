using Microsoft.EntityFrameworkCore;

namespace RIIS.Academic.Infrastructure.Persistence;

// Ces contextes partagent le modele metier, mais possedent chacun leur historique EF.
public sealed class RiisAcademicSqlServerMigrationsDbContext(
    DbContextOptions<RiisAcademicSqlServerMigrationsDbContext> options)
    : RiisAcademicModelDbContext(options);

public sealed class RiisAcademicSqliteMigrationsDbContext(
    DbContextOptions<RiisAcademicSqliteMigrationsDbContext> options)
    : RiisAcademicModelDbContext(options);
