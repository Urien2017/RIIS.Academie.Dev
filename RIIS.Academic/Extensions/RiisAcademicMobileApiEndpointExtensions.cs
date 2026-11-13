using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using RIIS.Academic.Application.ClassesPedagogiques.Services;
using RIIS.Academic.Application.Cours.Dtos;
using RIIS.Academic.Application.Cours.Services;
using RIIS.Academic.Application.Common.Dtos;
using RIIS.Academic.Application.Notes.Dtos;
using RIIS.Academic.Application.Notes.Services;
using RIIS.Academic.Application.Referentiels.Dtos;
using RIIS.Academic.Application.Referentiels.Services;
using RIIS.Academic.Domain;

namespace RIIS.Academic.Extensions;

public static class RiisAcademicMobileApiEndpointExtensions
{
    public static IEndpointRouteBuilder MapRiisAcademicMobileApiEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/mobile")
            .WithTags("RIIS Mobile");

        group.MapGet("/health", (
            HttpRequest request,
            IConfiguration configuration,
            IHostEnvironment environment) =>
        {
            if (!RiisAcademicMobileApiSecurity.IsAuthorized(request, configuration, environment))
            {
                return Results.Unauthorized();
            }

            return Results.Ok(new
            {
                service = "RIIS.Academic Mobile API",
                status = "ok",
                utc = DateTime.UtcNow
            });
        });

        group.MapGet("/notes/lookups", async (
            [FromQuery] long? anneeAcademiqueId,
            [FromQuery] long? classePedagogiqueId,
            [FromQuery] long? uniteEnseignementId,
            [FromQuery] long? elementConstitutifId,
            [FromQuery] TypeEvaluation? typeEvaluation,
            HttpRequest request,
            IConfiguration configuration,
            IHostEnvironment environment,
            [FromServices] ISaisieNotesService saisieNotesService,
            [FromServices] IReferentielsService referentielsService,
            CancellationToken cancellationToken) =>
        {
            if (!RiisAcademicMobileApiSecurity.IsAuthorized(request, configuration, environment))
            {
                return Results.Unauthorized();
            }

            var annees = await saisieNotesService
                .GetAnneesAcademiquesLookupAsync(cancellationToken);
            var classes = await saisieNotesService
                .GetClassesPedagogiquesLookupAsync(anneeAcademiqueId, cancellationToken);
            var unites = await saisieNotesService
                .GetUnitesEnseignementLookupAsync(cancellationToken: cancellationToken);
            var elements = await saisieNotesService
                .GetElementsConstitutifsLookupAsync(
                    uniteEnseignementId,
                    cancellationToken: cancellationToken);
            var evaluations = await saisieNotesService
                .GetEvaluationsLookupAsync(
                    anneeAcademiqueId,
                    uniteEnseignementId,
                    elementConstitutifId,
                    typeEvaluation,
                    cancellationToken: cancellationToken);
            var parcours = await referentielsService
                .GetParcoursAcademiquesAsync(
                    anneeAcademiqueId: anneeAcademiqueId,
                    inclureInactifs: false,
                    cancellationToken: cancellationToken);

            var semestres = anneeAcademiqueId is not null && classePedagogiqueId is not null
                ? await saisieNotesService.GetSemestresForClasseLookupAsync(
                    anneeAcademiqueId.Value,
                    classePedagogiqueId.Value,
                    cancellationToken)
                : [];

            return Results.Ok(new MobileNotesLookupsDto
            {
                AnneesAcademiques = annees,
                ParcoursAcademiques = parcours,
                ClassesPedagogiques = classes,
                SemestresPedagogiques = semestres,
                UnitesEnseignement = unites,
                ElementsConstitutifs = elements,
                Evaluations = evaluations
            });
        });

        group.MapGet("/cahiers-texte/lookups", async (
            [FromQuery] long? anneeAcademiqueId,
            HttpRequest request,
            IConfiguration configuration,
            IHostEnvironment environment,
            [FromServices] ISaisieNotesService saisieNotesService,
            [FromServices] IReferentielsService referentielsService,
            [FromServices] IClassesPedagogiquesService classesService,
            CancellationToken cancellationToken) =>
        {
            if (!RiisAcademicMobileApiSecurity.IsAuthorized(request, configuration, environment))
            {
                return Results.Unauthorized();
            }

            var annees = await saisieNotesService.GetAnneesAcademiquesLookupAsync(cancellationToken);
            var parcours = await referentielsService.GetParcoursAcademiquesAsync(
                anneeAcademiqueId,
                inclureInactifs: false,
                cancellationToken: cancellationToken);
            var classes = await classesService.GetClassesPedagogiquesAsync(cancellationToken);
            var parcoursById = parcours.ToDictionary(x => x.Id);

            var items = classes
                .Where(x => parcoursById.ContainsKey(x.ParcoursAcademiqueId))
                .Select(x =>
                {
                    var parcoursItem = parcoursById[x.ParcoursAcademiqueId];
                    return new MobileCahierTexteClasseLookupDto
                    {
                        AnneeAcademiqueId = parcoursItem.AnneeAcademiqueId,
                        ParcoursAcademiqueId = parcoursItem.Id,
                        ClassePedagogiqueId = x.Id,
                        Libelle = parcoursItem.Libelle,
                        CycleLibelle = parcoursItem.CycleLibelle,
                        NiveauLibelle = parcoursItem.NiveauLibelle,
                        FiliereLibelle = parcoursItem.FiliereLibelle,
                        SpecialiteLibelle = parcoursItem.SpecialiteLibelle
                    };
                })
                .OrderBy(x => x.Libelle)
                .ToList();

            return Results.Ok(new MobileCahierTexteLookupsDto
            {
                AnneesAcademiques = annees,
                Classes = items
            });
        });

        group.MapGet("/notes/canevas", async (
            [FromQuery] long anneeAcademiqueId,
            [FromQuery] long classePedagogiqueId,
            [FromQuery] long semestrePedagogiqueId,
            [FromQuery] long? uniteEnseignementId,
            [FromQuery] long? elementConstitutifId,
            [FromQuery] TypeEvaluation? typeEvaluation,
            HttpRequest request,
            IConfiguration configuration,
            IHostEnvironment environment,
            [FromServices] INotesCanevasService canevasService,
            CancellationToken cancellationToken) =>
        {
            if (!RiisAcademicMobileApiSecurity.IsAuthorized(request, configuration, environment))
            {
                return Results.Unauthorized();
            }

            try
            {
                var canevas = await canevasService.GetCanevasAsync(
                    anneeAcademiqueId,
                    classePedagogiqueId,
                    semestrePedagogiqueId,
                    uniteEnseignementId,
                    elementConstitutifId,
                    typeEvaluation,
                    cancellationToken);

                return canevas is null
                    ? Results.NotFound(new { message = "Aucun canevas de notes trouvé." })
                    : Results.Ok(canevas);
            }
            catch (NotesCanevasUnavailableException exception)
            {
                return Results.NotFound(new { message = exception.Message });
            }
        });

        group.MapPost("/notes/sync", async (
            SaisieNotesGrilleDto grille,
            [FromHeader(Name = "X-RIIS-Saisie-Par")] string? saisiePar,
            HttpRequest request,
            IConfiguration configuration,
            IHostEnvironment environment,
            [FromServices] ISaisieNotesService saisieNotesService,
            CancellationToken cancellationToken) =>
        {
            if (!RiisAcademicMobileApiSecurity.IsAuthorized(request, configuration, environment))
            {
                return Results.Unauthorized();
            }

            if (grille is null || grille.Lignes.Count == 0)
            {
                return Results.BadRequest(new
                {
                    message = "La grille doit contenir au moins une ligne de note."
                });
            }

            try
            {
                await saisieNotesService.SaveNotesAsync(
                    grille,
                    NormalizeSaisiePar(saisiePar),
                    cancellationToken);

                return Results.Ok(new MobileNotesSyncResponseDto
                {
                    EvaluationAcademiqueId = grille.EvaluationAcademiqueId,
                    ClassePedagogiqueId = grille.ClassePedagogiqueId,
                    LignesTraitees = grille.Lignes.Count,
                    SynchronizedAtUtc = DateTime.UtcNow,
                    Message = "Les notes ont été synchronisées avec succès."
                });
            }
            catch (InvalidOperationException exception)
            {
                return Results.UnprocessableEntity(new
                {
                    message = exception.Message
                });
            }
        })
        .Accepts<SaisieNotesGrilleDto>("application/json")
        .DisableAntiforgery();

        MapCahierTexteEndpoints(group);

        return endpoints;
    }

    private static void MapCahierTexteEndpoints(RouteGroupBuilder group)
    {
        group.MapPost("/cahiers-texte/pages/upload", async (
            HttpRequest request,
            IConfiguration configuration,
            IHostEnvironment environment,
            CancellationToken cancellationToken) =>
        {
            if (!RiisAcademicMobileApiSecurity.IsAuthorized(request, configuration, environment))
            {
                return Results.Unauthorized();
            }

            if (!request.HasFormContentType)
            {
                return Results.BadRequest(new
                {
                    message = "L'upload doit utiliser le format multipart/form-data."
                });
            }

            var form = await request.ReadFormAsync(cancellationToken);
            var file = form.Files.GetFile("file");
            if (file is null || file.Length == 0)
            {
                return Results.BadRequest(new { message = "Aucun fichier image n'a été fourni." });
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".pdf" };
            if (!allowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            {
                return Results.BadRequest(new
                {
                    message = "Format non pris en charge. Utilisez JPG, PNG, WEBP ou PDF."
                });
            }

            const long maxFileSize = 15 * 1024 * 1024;
            if (file.Length > maxFileSize)
            {
                return Results.BadRequest(new
                {
                    message = "Le fichier ne doit pas dépasser 15 Mo."
                });
            }

            var configuredStoragePath = configuration["MobileApi:CaptureStoragePath"];
            var storagePath = string.IsNullOrWhiteSpace(configuredStoragePath)
                ? Path.Combine(AppContext.BaseDirectory, "App_Data", "CahierTexteUploads")
                : configuredStoragePath;
            Directory.CreateDirectory(storagePath);

            var storageId = Guid.NewGuid().ToString("N");
            var storageFileName = storageId + extension;
            var fullPath = Path.Combine(storagePath, storageFileName);
            await using (var input = file.OpenReadStream())
            await using (var output = File.Create(fullPath))
            {
                await input.CopyToAsync(output, cancellationToken);
            }

            await using var hashStream = File.OpenRead(fullPath);
            var hash = await SHA256.HashDataAsync(hashStream, cancellationToken);

            return Results.Ok(new MobileCaptureUploadResultDto
            {
                StorageKey = $"mobile-capture://{storageFileName}",
                OriginalFileName = Path.GetFileName(file.FileName),
                ContentType = file.ContentType ?? "application/octet-stream",
                SizeBytes = file.Length,
                Sha256 = Convert.ToHexString(hash)
            });
        })
        .Accepts<IFormFile>("multipart/form-data")
        .Produces<MobileCaptureUploadResultDto>()
        .DisableAntiforgery();

        group.MapPost("/cahiers-texte/import", async (
            CahierTexteImportRequestDto requestDto,
            HttpRequest request,
            IConfiguration configuration,
            IHostEnvironment environment,
            [FromServices] ICahierTexteImportService importService,
            CancellationToken cancellationToken) =>
        {
            if (!RiisAcademicMobileApiSecurity.IsAuthorized(request, configuration, environment))
            {
                return Results.Unauthorized();
            }

            if (requestDto is null)
            {
                return Results.BadRequest(new { message = "Le contenu de l'import est obligatoire." });
            }

            var result = await importService.ImporterAsync(requestDto, cancellationToken);
            return result.EstValide
                ? Results.Ok(result)
                : Results.UnprocessableEntity(result);
        })
        .Accepts<CahierTexteImportRequestDto>("application/json")
        .Produces<CahierTexteImportResultDto>()
        .DisableAntiforgery();

        group.MapPost("/cahiers-texte/validate", async (
            CahierTexteImportRequestDto requestDto,
            [FromQuery] long? cahierTexteId,
            HttpRequest request,
            IConfiguration configuration,
            IHostEnvironment environment,
            [FromServices] ICahierTexteValidationService validationService,
            CancellationToken cancellationToken) =>
        {
            if (!RiisAcademicMobileApiSecurity.IsAuthorized(request, configuration, environment))
            {
                return Results.Unauthorized();
            }

            if (requestDto is null)
            {
                return Results.BadRequest(new { message = "Le contenu à valider est obligatoire." });
            }

            var report = await validationService.ValiderAsync(
                requestDto,
                cahierTexteId,
                cancellationToken);

            // La validation est une réponse métier de prévisualisation : le
            // mobile doit recevoir le détail des erreurs sans perdre le rapport.
            return Results.Ok(report);
        })
        .Accepts<CahierTexteImportRequestDto>("application/json")
        .Produces<CahierTexteValidationReportDto>()
        .DisableAntiforgery();

        group.MapPost("/cahiers-texte/{cahierTexteId:long}/seances/confirm", async (
            long cahierTexteId,
            CahierTexteImportRequestDto requestDto,
            HttpRequest request,
            IConfiguration configuration,
            IHostEnvironment environment,
            [FromServices] ICahierTexteValidationService validationService,
            CancellationToken cancellationToken) =>
        {
            if (!RiisAcademicMobileApiSecurity.IsAuthorized(request, configuration, environment))
            {
                return Results.Unauthorized();
            }

            if (requestDto is null)
            {
                return Results.BadRequest(new { message = "Le contenu à confirmer est obligatoire." });
            }

            // La validation est recalculée côté serveur pour empêcher qu'un
            // client modifie l'EC ou la maquette après la prévisualisation.
            var validation = await validationService.ValiderAsync(
                requestDto,
                cahierTexteId,
                cancellationToken);

            if (!validation.EstValide)
            {
                return Results.UnprocessableEntity(new
                {
                    message = "Les séances ne peuvent pas être confirmées avant correction des erreurs.",
                    validation
                });
            }

            var result = await validationService.EnregistrerSeancesValideesAsync(
                cahierTexteId,
                requestDto,
                validation,
                cancellationToken);

            return result.EstValide
                ? Results.Ok(result)
                : Results.UnprocessableEntity(result);
        })
        .Accepts<CahierTexteImportRequestDto>("application/json")
        .Produces<CahierTexteSeancesSaveResultDto>()
        .DisableAntiforgery();
    }

    private static string NormalizeSaisiePar(string? saisiePar)
        => string.IsNullOrWhiteSpace(saisiePar)
            ? "RIIS.MobApp"
            : saisiePar.Trim();

}

public sealed class MobileNotesLookupsDto
{
    public List<LookupDto> AnneesAcademiques { get; set; } = [];
    public List<ParcoursAcademiqueDto> ParcoursAcademiques { get; set; } = [];
    public List<LookupDto> ClassesPedagogiques { get; set; } = [];
    public List<LookupDto> SemestresPedagogiques { get; set; } = [];
    public List<LookupDto> UnitesEnseignement { get; set; } = [];
    public List<LookupDto> ElementsConstitutifs { get; set; } = [];
    public List<LookupDto> Evaluations { get; set; } = [];
}

public sealed class MobileNotesSyncResponseDto
{
    public long EvaluationAcademiqueId { get; set; }
    public long ClassePedagogiqueId { get; set; }
    public int LignesTraitees { get; set; }
    public DateTime SynchronizedAtUtc { get; set; }
    public string Message { get; set; } = string.Empty;
}

public sealed class MobileCaptureUploadResultDto
{
    public string StorageKey { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string Sha256 { get; set; } = string.Empty;
}

public sealed class MobileCahierTexteLookupsDto
{
    public List<LookupDto> AnneesAcademiques { get; set; } = [];
    public List<MobileCahierTexteClasseLookupDto> Classes { get; set; } = [];
}

public sealed class MobileCahierTexteClasseLookupDto
{
    public long AnneeAcademiqueId { get; set; }
    public long ParcoursAcademiqueId { get; set; }
    public long ClassePedagogiqueId { get; set; }
    public string Libelle { get; set; } = string.Empty;
    public string CycleLibelle { get; set; } = string.Empty;
    public string NiveauLibelle { get; set; } = string.Empty;
    public string FiliereLibelle { get; set; } = string.Empty;
    public string SpecialiteLibelle { get; set; } = string.Empty;
}
