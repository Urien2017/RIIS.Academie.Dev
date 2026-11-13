using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RIIS.Academic.Application.Cours.Services;
using RIIS.Academic.Infrastructure.Persistence.Seeders;

namespace RIIS.Academic.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    public static async Task InitializeRiisAcademicDatabaseAsync(
        this IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();

        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("RIIS.Academic.DatabaseInitializer");
        var dbContext = scope.ServiceProvider.GetRequiredService<RiisAcademicDbContext>();
        var provider = RiisDatabaseProvider.IsSqlite(configuration) ? "SQLite" : "SQL Server";

        logger.LogInformation(
            "Initialisation de la base RIIS Academic. Provider={Provider}, Migrations={ApplyMigrations}",
            provider,
            configuration["Database:ApplyMigrations"] ?? "true");

        var applyMigrations = !bool.TryParse(
            configuration["Database:ApplyMigrations"],
            out var configuredApplyMigrations)
            || configuredApplyMigrations;

        if (applyMigrations)
        {
            if (RiisDatabaseProvider.IsSqlite(configuration))
            {
                await ExecuteStepAsync(
                    logger,
                    "Application des migrations SQLite",
                    () => scope.ServiceProvider
                        .GetRequiredService<RiisAcademicSqliteMigrationsDbContext>()
                        .Database
                        .MigrateAsync(cancellationToken));
            }
            else
            {
                // Les migrations SQL Server existantes appartiennent encore
                // au contexte principal RiisAcademicDbContext.
                await ExecuteStepAsync(
                    logger,
                    "Application des migrations SQL Server",
                    () => dbContext.Database.MigrateAsync(cancellationToken));
            }
        }

        // L'admin et les permissions doivent exister apres la structure de la base.
        // Les parcours doivent exister avant la creation des tarifs.
        await ExecuteStepAsync(
            logger,
            "Seeder Authorization",
            () => AuthorizationSeeder.SeedAsync(dbContext, cancellationToken));
        await ExecuteStepAsync(
            logger,
            "Seeder FiliereSpecialite",
            () => FiliereSpecialiteSeeder.SeedAsync(dbContext, cancellationToken));
        await ExecuteStepAsync(
            logger,
            "Seeder NiveauDeuxParcours",
            () => NiveauDeuxParcoursSeeder.SeedAsync(dbContext, cancellationToken));
        await ExecuteStepAsync(
            logger,
            "Seeder BtsOfficialProgramme",
            () => BtsOfficialProgrammeSeeder.SeedAsync(dbContext, cancellationToken));
        await ExecuteStepAsync(
            logger,
            "Seeder PrepaOfficialProgramme",
            () => PrepaOfficialProgrammeSeeder.SeedAsync(dbContext, cancellationToken));
        await ExecuteStepAsync(
            logger,
            "Seeder ClassesPedagogiques",
            () => ClassesPedagogiquesSeeder.SeedAsync(dbContext, logger, cancellationToken));
        await ExecuteStepAsync(
            logger,
            "Validation des référentiels de cours",
            async () =>
            {
                var report = await scope.ServiceProvider
                    .GetRequiredService<IReferentielCoursValidationService>()
                    .ValidateAsync(cancellationToken);

                if (!report.EstValide)
                {
                    logger.LogWarning(
                        "Validation des référentiels terminée avec {ErrorCount} erreur(s) et {WarningCount} avertissement(s). Parcours exploitables : {UsableCount}/{AnalyzedCount}.",
                        report.Erreurs,
                        report.Avertissements,
                        report.ParcoursExploitables,
                        report.ParcoursAnalyses);
                }
                else
                {
                    logger.LogInformation(
                        "Validation des référentiels réussie. Parcours exploitables : {UsableCount}/{AnalyzedCount}. Avertissements : {WarningCount}.",
                        report.ParcoursExploitables,
                        report.ParcoursAnalyses,
                        report.Avertissements);
                }

                foreach (var issue in report.Issues)
                {
                    if (issue.Severity == RIIS.Academic.Application.Cours.Dtos.ReferentielValidationSeverity.Erreur)
                    {
                        logger.LogError(
                            "Référentiel invalide. Parcours={ParcoursId} Maquette={MaquetteId} Semestre={SemestreId} EC={ElementId} : {Message}",
                            issue.ParcoursAcademiqueId,
                            issue.MaquettePedagogiqueId,
                            issue.SemestrePedagogiqueId,
                            issue.ElementConstitutifId,
                            issue.Message);
                    }
                    else if (issue.Severity == RIIS.Academic.Application.Cours.Dtos.ReferentielValidationSeverity.Avertissement)
                    {
                        logger.LogWarning(
                            "Avertissement référentiel. Parcours={ParcoursId} Maquette={MaquetteId} Semestre={SemestreId} EC={ElementId} : {Message}",
                            issue.ParcoursAcademiqueId,
                            issue.MaquettePedagogiqueId,
                            issue.SemestrePedagogiqueId,
                            issue.ElementConstitutifId,
                            issue.Message);
                    }
                }
            });
        await ExecuteStepAsync(
            logger,
            "Seeder ReconstructionEtudiantsDossiers",
            () => ReconstructionEtudiantsDossiersSeeder.SeedAsync(dbContext, cancellationToken));
        await ExecuteStepAsync(
            logger,
            "Seeder Scolarite",
            () => ScolariteSeeder.SeedAsync(dbContext, cancellationToken));

        logger.LogInformation("Initialisation de la base RIIS Academic terminée.");
    }

    private static async Task ExecuteStepAsync(
        ILogger logger,
        string stepName,
        Func<Task> action)
    {
        var stopwatch = Stopwatch.StartNew();
        logger.LogInformation("Début : {StepName}", stepName);

        try
        {
            await action();
            stopwatch.Stop();
            logger.LogInformation(
                "Terminé : {StepName} en {ElapsedSeconds:0.00} s",
                stepName,
                stopwatch.Elapsed.TotalSeconds);
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            logger.LogError(
                exception,
                "Échec : {StepName} après {ElapsedSeconds:0.00} s",
                stepName,
                stopwatch.Elapsed.TotalSeconds);
            throw;
        }
    }
}
