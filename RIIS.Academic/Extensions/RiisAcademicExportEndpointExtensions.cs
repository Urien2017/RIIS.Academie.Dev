using Microsoft.AspNetCore.Mvc;
using RIIS.Academic.Application.Dossiers.Services;
using RIIS.Academic.Application.Notes.Dtos;
using RIIS.Academic.Application.Notes.Services;
using RIIS.Academic.Application.ProcesVerbaux.Services;
using RIIS.Academic.Application.Releves.Services;
using RIIS.Academic.Domain;

namespace RIIS.Academic.Extensions;

public static class RiisAcademicExportEndpointExtensions
{
    public static IEndpointRouteBuilder MapRiisAcademicExportEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
            "/exports/dossiers-etudiants.xlsx",
            async (
                [FromQuery] long? parcoursAcademiqueId,
                [FromQuery] long? anneeAcademiqueId,
                [FromQuery] long? cycleFormationId,
                [FromQuery] long? niveauEtudeId,
                [FromQuery] long? etudiantId,
                [FromServices] IDossierEtudiantExcelExportService exportService,
                CancellationToken cancellationToken) =>
            {
                var export = await exportService.ExporterDossiersAsync(
                    parcoursAcademiqueId,
                    anneeAcademiqueId,
                    cycleFormationId,
                    niveauEtudeId,
                    etudiantId,
                    cancellationToken);

                return Results.File(export.Content, export.ContentType, export.FileName);
            });

        endpoints.MapGet(
            "/exports/pv/{procesVerbalId:long}.docx",
            async (
                [FromRoute] long procesVerbalId,
                [FromServices] IProcesVerbalWordExportService exportService,
                CancellationToken cancellationToken) =>
            {
                var export = await exportService.ExporterProcesVerbalAsync(
                    procesVerbalId,
                    cancellationToken);

                return export is null
                    ? Results.NotFound()
                    : Results.File(export.Content, export.ContentType, export.FileName);
            });

        endpoints.MapGet(
            "/exports/pv/{procesVerbalId:long}.xlsx",
            async (
                [FromRoute] long procesVerbalId,
                [FromServices] IProcesVerbalExcelExportService exportService,
                CancellationToken cancellationToken) =>
            {
                var export = await exportService.ExporterProcesVerbalAsync(
                    procesVerbalId,
                    cancellationToken);

                return export is null
                    ? Results.NotFound()
                    : Results.File(export.Content, export.ContentType, export.FileName);
            });

        endpoints.MapGet(
            "/exports/pv/{procesVerbalId:long}.pdf",
            async (
                [FromRoute] long procesVerbalId,
                [FromServices] IProcesVerbalPdfExportService exportService,
                CancellationToken cancellationToken) =>
            {
                var export = await exportService.ExporterProcesVerbalAsync(
                    procesVerbalId,
                    cancellationToken);

                return export is null
                    ? Results.NotFound()
                    : Results.File(export.Content, export.ContentType, export.FileName);
            });

        endpoints.MapGet(
            "/exports/notes/canevas/{anneeAcademiqueId:long}/{classePedagogiqueId:long}.xlsx",
            async (
                [FromRoute] long anneeAcademiqueId,
                [FromRoute] long classePedagogiqueId,
                [FromQuery] long semestrePedagogiqueId,
                [FromQuery] long? uniteEnseignementId,
                [FromQuery] long? elementConstitutifId,
                [FromQuery] TypeEvaluation? typeEvaluation,
                [FromServices] INotesCanevasExcelExportService exportService,
                [FromServices] ILoggerFactory loggerFactory,
                CancellationToken cancellationToken) =>
            {
                NotesCanevasExcelExportDto? export;
                try
                {
                    export = await exportService.ExporterCanevasAsync(
                        anneeAcademiqueId,
                        classePedagogiqueId,
                        semestrePedagogiqueId,
                        uniteEnseignementId,
                        elementConstitutifId,
                        typeEvaluation,
                        cancellationToken);
                }
                catch (NotesCanevasUnavailableException ex)
                {
                    return Results.NotFound(new { message = ex.Message });
                }

                if (export is null)
                {
                    loggerFactory.CreateLogger("NotesCanevasExport")
                        .LogWarning(
                            "Canevas introuvable pour AnneeAcademiqueId={AnneeAcademiqueId}, ClassePedagogiqueId={ClassePedagogiqueId}, SemestrePedagogiqueId={SemestrePedagogiqueId}, UniteEnseignementId={UniteEnseignementId}, ElementConstitutifId={ElementConstitutifId}, TypeEvaluation={TypeEvaluation}.",
                            anneeAcademiqueId,
                            classePedagogiqueId,
                            semestrePedagogiqueId,
                            uniteEnseignementId,
                            elementConstitutifId,
                            typeEvaluation);

                    return Results.NotFound(new
                    {
                        message = "Aucun canevas disponible pour cette année et cette classe. Vérifiez l'affectation d'une maquette, les semestres et les évaluations."
                    });
                }

                return Results.File(export.Content, export.ContentType, export.FileName);
            });

        endpoints.MapGet(
            "/exports/pv/{procesVerbalId:long}.modele.docx",
            async (
                [FromRoute] long procesVerbalId,
                [FromServices] IProcesVerbalTemplateWordExportService exportService,
                CancellationToken cancellationToken) =>
            {
                var export = await exportService.ExporterProcesVerbalAsync(
                    procesVerbalId,
                    cancellationToken);

                return export is null
                    ? Results.NotFound()
                    : Results.File(export.Content, export.ContentType, export.FileName);
            });

        endpoints.MapGet(
            "/exports/notes/options2/{anneeAcademiqueId:long}/{classePedagogiqueId:long}/{semestrePedagogiqueId:long}.docx",
            async (
                [FromRoute] long anneeAcademiqueId,
                [FromRoute] long classePedagogiqueId,
                [FromRoute] long semestrePedagogiqueId,
                [FromQuery] long? uniteEnseignementId,
                [FromQuery] long? elementConstitutifId,
                [FromQuery] TypeEvaluation? typeEvaluation,
                [FromServices] IProcesVerbalOptions2WordExportService exportService,
                CancellationToken cancellationToken) =>
            {
                var export = await exportService.ExporterFicheAsync(
                    anneeAcademiqueId,
                    classePedagogiqueId,
                    semestrePedagogiqueId,
                    uniteEnseignementId,
                    elementConstitutifId,
                    typeEvaluation,
                    cancellationToken);

                return export is null
                    ? Results.NotFound()
                    : Results.File(export.Content, export.ContentType, export.FileName);
            });

        endpoints.MapGet(
            "/exports/releves/{inscriptionId:long}.docx",
            async (
                [FromRoute] long inscriptionId,
                [FromServices] IReleveNoteWordExportService exportService,
                CancellationToken cancellationToken) =>
            {
                var export = await exportService.ExporterReleveAnnuelAsync(
                    inscriptionId,
                    cancellationToken);

                return export is null
                    ? Results.NotFound()
                    : Results.File(export.Content, export.ContentType, export.FileName);
            });

        endpoints.MapGet(
            "/exports/releves/{inscriptionId:long}.modele.docx",
            async (
                [FromRoute] long inscriptionId,
                [FromServices] IReleveNoteTemplateWordExportService exportService,
                CancellationToken cancellationToken) =>
            {
                var export = await exportService.ExporterReleveAnnuelAsync(
                    inscriptionId,
                    cancellationToken);

                return export is null
                    ? Results.NotFound()
                    : Results.File(export.Content, export.ContentType, export.FileName);
            });

        endpoints.MapGet(
            "/exports/releves/{dossierAcademiqueId:long}.pdf",
            async (
                [FromRoute] long dossierAcademiqueId,
                [FromServices] IReleveNotePdfExportService exportService,
                HttpResponse response,
                CancellationToken cancellationToken) =>
            {
                var export = await exportService.ExporterReleveAnnuelAsync(
                    dossierAcademiqueId,
                    // La génération PDF ouvre une nouvelle lecture SQL et peut
                    // dépasser le cycle d'une requête de téléchargement navigateur.
                    // Elle doit aller au bout pour produire le document.
                    CancellationToken.None);

                return export is null
                    ? Results.NotFound()
                    : Results.File(export.Content, export.ContentType, export.FileName);
            });

        endpoints.MapGet(
            "/previews/releves/{dossierAcademiqueId:long}.pdf",
            async (
                [FromRoute] long dossierAcademiqueId,
                [FromServices] IReleveNotePdfExportService exportService,
                HttpResponse response,
                CancellationToken cancellationToken) =>
            {
                var export = await exportService.ExporterReleveAnnuelAsync(
                    dossierAcademiqueId,
                    // Même comportement pour l'aperçu PDF chargé dans l'iframe.
                    CancellationToken.None);

                if (export is null)
                {
                    return Results.NotFound();
                }

                // Le PDF doit être rendu par le lecteur intégré de l'iframe.
                // L'endpoint /exports conserve, lui, le téléchargement avec Results.File.
                response.Headers.ContentDisposition = "inline";
                return Results.Stream(new MemoryStream(export.Content), export.ContentType);
            });

        return endpoints;
    }
}
