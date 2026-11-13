using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RIIS.Academic.Application.Abstractions.Persistence;
using RIIS.Academic.Application.Abstractions.Services;
using RIIS.Academic.Application.Authorization.Services;
using RIIS.Academic.Application.ClassesPedagogiques.Services;
using RIIS.Academic.Application.Cours.Services;
using RIIS.Academic.Application.Dashboard.Services;
using RIIS.Academic.Application.Dossiers.Services;
using RIIS.Academic.Application.Etudiants.Services;
using RIIS.Academic.Application.Evaluations.Services;
using RIIS.Academic.Application.Notes.Services;
using RIIS.Academic.Application.ProcesVerbaux.Services;
using RIIS.Academic.Application.Programmes.Services;
using RIIS.Academic.Application.Referentiels.Services;
using RIIS.Academic.Application.Releves.Services;
using RIIS.Academic.Application.Scolarite.Services;
using RIIS.Academic.Infrastructure.Documents;
using RIIS.Academic.Infrastructure.Persistence.Repositories;
using RIIS.Academic.Infrastructure.Persistence.Services;
using RIIS.Academic.Infrastructure.Persistence.SqlServer;
using RIIS.Academic.Infrastructure.Persistence.Sqlite;
using RIIS.Academic.Infrastructure.Persistence.Interceptors;

namespace RIIS.Academic.Infrastructure.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddRiisAcademicInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var provider = RiisDatabaseProvider.Resolve(configuration);
        var connectionString = ConnectionStringHelper.GetRiisConnectionString(configuration, provider);

        services.AddScoped<LoadingDbCommandInterceptor>();

        services.AddDbContext<RiisAcademicDbContext>(
            (serviceProvider, options) =>
            {
                options.AddInterceptors(
                    serviceProvider.GetRequiredService<LoadingDbCommandInterceptor>());

                if (string.Equals(provider, RiisDatabaseProvider.Sqlite, StringComparison.OrdinalIgnoreCase))
                {
                    options.UseRiisSqlite(connectionString, AppContext.BaseDirectory);
                }
                else
                {
                    options.UseRiisSqlServer(connectionString);
                }
            },
            contextLifetime: ServiceLifetime.Transient,
            optionsLifetime: ServiceLifetime.Transient);

        if (string.Equals(provider, RiisDatabaseProvider.Sqlite, StringComparison.OrdinalIgnoreCase))
        {
            services.AddDbContext<RiisAcademicSqliteMigrationsDbContext>(
                (serviceProvider, options) =>
                {
                    options.AddInterceptors(
                        serviceProvider.GetRequiredService<LoadingDbCommandInterceptor>());
                    options.UseRiisSqlite(connectionString, AppContext.BaseDirectory);
                },
                contextLifetime: ServiceLifetime.Transient,
                optionsLifetime: ServiceLifetime.Transient);
        }
        else
        {
            services.AddDbContext<RiisAcademicSqlServerMigrationsDbContext>(
                (serviceProvider, options) =>
                {
                    options.AddInterceptors(
                        serviceProvider.GetRequiredService<LoadingDbCommandInterceptor>());
                    options.UseRiisSqlServer(connectionString);
                },
                contextLifetime: ServiceLifetime.Transient,
                optionsLifetime: ServiceLifetime.Transient);
        }

        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
        services.AddScoped<IReferentielsService, ReferentielsService>();
        services.AddScoped<IReferentielCoursValidationService, ReferentielCoursValidationService>();
        services.AddScoped<ICahierTexteImportService, CahierTexteImportService>();
        services.AddScoped<ICahierTexteValidationService, CahierTexteValidationService>();
        services.AddScoped<IRolesService, RolesService>();
        services.AddScoped<IUtilisateursService, UtilisateursService>();
        services.AddScoped<IMatriculeGenerator, MatriculeGenerator>();
        services.AddScoped<IEtudiantsService, EtudiantsService>();
        services.AddScoped<IDashboardAcademiqueService, DashboardAcademiqueService>();
        services.AddScoped<IDashboardScolariteService, DashboardScolariteService>();
        //Dossiers
        services.AddScoped<IDossierAdministratifService, DossierAdministratifService>();
        services.AddScoped<IDossierAcademiqueService, DossierAcademiqueService>();
        services.AddScoped<IDossierFinancierService, DossierFinancierService>();
        services.AddScoped<IDossierEtudiantService, EfDossierEtudiantService>();
        //Notes
        services.AddScoped<IClassesPedagogiquesService, ClassesPedagogiquesService>();
        services.AddScoped<IProgrammePedagogiqueService, ProgrammePedagogiqueService>();
        services.AddScoped<IEvaluationsService, EvaluationsService>();
        services.AddScoped<ICalculNotesService, CalculNotesService>();
        services.AddScoped<ISaisieNotesService, SaisieNotesService>();
        services.AddScoped<INotesCanevasService, NotesCanevasService>();
        services.AddScoped<INotesCanevasExcelImportService, NotesCanevasExcelImportService>();
        services.AddScoped<INotesDocumentImportService, NotesDocumentImportService>();
        services.AddScoped<IProcesVerbauxService, ProcesVerbauxService>();
        services.AddScoped<IRelevesNotesService, RelevesNotesService>();
        //Scolarité
        services.AddScoped<ITypesElementsScolariteService, TypesElementsScolariteService>();
        services.AddScoped<IModesPaiementScolariteService, ModesPaiementScolariteService>();
        services.AddScoped<ITarifsScolariteService, TarifsScolariteService>();
        services.AddScoped<IDossiersScolariteService, DossiersScolariteService>();
        services.AddScoped<IFinancesScolariteService, FinancesScolariteService>();
        //ExportService
        services.AddScoped<IProcesVerbalExcelExportService, ProcesVerbalExcelExportService>();
        services.AddScoped<IProcesVerbalPdfExportService, ProcesVerbalPdfExportService>();
        services.AddScoped<INotesCanevasExcelExportService, NotesCanevasExcelExportService>();
        services.AddScoped<IProcesVerbalTemplateWordExportService, ProcesVerbalTemplateWordExportService>();
        services.AddScoped<IProcesVerbalWordExportService, ProcesVerbalWordExportService>();
        services.AddScoped<IProcesVerbalOptions2WordExportService, ProcesVerbalOptions2WordExportService>();
        services.AddScoped<IReleveNoteTemplateWordExportService, ReleveNoteTemplateWordExportService>();
        services.AddScoped<IReleveNoteWordExportService, ReleveNoteWordExportService>();
        services.AddScoped<IReleveNotePdfExportService, ReleveNotePdfExportService>();
        services.AddScoped<IDossierEtudiantExcelExportService, DossierEtudiantExcelExportService>();

        return services;
    }

    
}
