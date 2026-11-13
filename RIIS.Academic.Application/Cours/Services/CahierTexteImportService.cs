using RIIS.Academic.Application.Abstractions.Persistence;
using RIIS.Academic.Application.Cours.Dtos;
using RIIS.Academic.Domain;

namespace RIIS.Academic.Application.Cours.Services;

/// <summary>
/// Enregistre la source importee et les pages OCR. Les seances ne sont pas
/// creees ici : elles doivent d'abord passer par
/// <see cref="ICahierTexteValidationService"/>.
/// </summary>
public sealed class CahierTexteImportService(
    IRepository<AnneeAcademique> anneesAcademiques,
    IRepository<ParcoursAcademique> parcoursAcademiques,
    IRepository<ClassePedagogique> classesPedagogiques,
    IRepository<CahierTexte> cahiersTextes,
    IRepository<CahierTextePage> pages) : ICahierTexteImportService
{
    public async Task<CahierTexteImportResultDto> ImporterAsync(
        CahierTexteImportRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var issues = new List<CahierTexteValidationIssueDto>();
        ValidateRequestShape(request, issues);

        var annee = await anneesAcademiques.GetByIdAsync(request.AnneeAcademiqueId, cancellationToken);
        var parcours = await parcoursAcademiques.GetByIdAsync(request.ParcoursAcademiqueId, cancellationToken);
        var classe = await classesPedagogiques.GetByIdAsync(request.ClassePedagogiqueId, cancellationToken);

        if (annee is null)
        {
            AddError(issues, $"L'année académique {request.AnneeAcademiqueId} est introuvable.");
        }

        if (parcours is null)
        {
            AddError(issues, $"Le parcours académique {request.ParcoursAcademiqueId} est introuvable.");
        }
        else if (!parcours.EstActive)
        {
            AddError(issues, $"Le parcours académique '{parcours.Libelle}' est inactif.");
        }

        if (classe is null)
        {
            AddError(issues, $"La classe pédagogique {request.ClassePedagogiqueId} est introuvable.");
        }

        if (annee is not null && parcours is not null
            && parcours.AnneeAcademiqueId != annee.Id)
        {
            AddError(issues, "Le parcours sélectionné n'appartient pas à l'année académique sélectionnée.");
        }

        if (classe is not null && classe.ParcoursAcademiqueId != request.ParcoursAcademiqueId)
        {
            AddError(issues, "La classe pédagogique ne correspond pas au parcours académique sélectionné.");
        }

        if (issues.Any(x => x.Severity == CahierTexteValidationSeverity.Erreur))
        {
            return new CahierTexteImportResultDto { Issues = issues };
        }

        var cahiers = await cahiersTextes.ListAsync(cancellationToken);
        var cahier = cahiers.FirstOrDefault(x =>
            x.AnneeAcademiqueId == request.AnneeAcademiqueId
            && x.ParcoursAcademiqueId == request.ParcoursAcademiqueId
            && x.ClassePedagogiqueId == request.ClassePedagogiqueId);

        if (cahier is null)
        {
            cahier = new CahierTexte
            {
                AnneeAcademiqueId = request.AnneeAcademiqueId,
                ParcoursAcademiqueId = request.ParcoursAcademiqueId,
                ClassePedagogiqueId = request.ClassePedagogiqueId,
                EnseignantUtilisateurId = request.EnseignantUtilisateurId,
                EnseignantNom = request.EnseignantNom,
                DateOuverture = request.DateOuverture ?? DateOnly.FromDateTime(DateTime.UtcNow)
            };

            await cahiersTextes.AddAsync(cahier, cancellationToken);
            await cahiersTextes.SaveChangesAsync(cancellationToken);
        }
        else
        {
            cahier.EnseignantUtilisateurId = request.EnseignantUtilisateurId ?? cahier.EnseignantUtilisateurId;
            cahier.EnseignantNom = string.IsNullOrWhiteSpace(request.EnseignantNom)
                ? cahier.EnseignantNom
                : request.EnseignantNom.Trim();
            cahier.DateOuverture ??= request.DateOuverture;
            cahier.ModifieLeUtc = DateTime.UtcNow;
            await cahiersTextes.SaveChangesAsync(cancellationToken);
        }

        var existingPages = (await pages.ListAsync(cancellationToken))
            .Where(x => x.CahierTexteId == cahier.Id)
            .ToList();
        var pagesImportees = 0;
        var pagesIgnorees = 0;

        foreach (var pageRequest in request.Pages)
        {
            var existing = existingPages.FirstOrDefault(x =>
                x.NumeroPage == pageRequest.NumeroPage
                || string.Equals(x.EmpreinteSha256, pageRequest.EmpreinteSha256, StringComparison.OrdinalIgnoreCase));

            if (existing is not null)
            {
                if (string.Equals(existing.EmpreinteSha256, pageRequest.EmpreinteSha256, StringComparison.OrdinalIgnoreCase))
                {
                    pagesIgnorees++;
                    continue;
                }

                existing.FichierOriginalUri = pageRequest.FichierOriginalUri.Trim();
                existing.EmpreinteSha256 = pageRequest.EmpreinteSha256.Trim();
                existing.TexteOcr = pageRequest.TexteOcr;
                existing.ConfianceOcr = pageRequest.ConfianceOcr;
                existing.Statut = StatutPageCahierTexte.Importee;
                existing.ImporteLeUtc = DateTime.UtcNow;
                existing.TraiteeLeUtc = null;
                issues.Add(new CahierTexteValidationIssueDto
                {
                    Severity = CahierTexteValidationSeverity.Avertissement,
                    NumeroPage = pageRequest.NumeroPage,
                    Message = "La page existait déjà avec une empreinte différente ; sa nouvelle version remplace l'ancienne source."
                });
                pagesImportees++;
                continue;
            }

            var page = new CahierTextePage
            {
                CahierTexteId = cahier.Id,
                NumeroPage = pageRequest.NumeroPage,
                FichierOriginalUri = pageRequest.FichierOriginalUri.Trim(),
                EmpreinteSha256 = pageRequest.EmpreinteSha256.Trim(),
                TexteOcr = pageRequest.TexteOcr,
                ConfianceOcr = pageRequest.ConfianceOcr,
                Statut = StatutPageCahierTexte.Importee
            };

            await pages.AddAsync(page, cancellationToken);
            existingPages.Add(page);
            pagesImportees++;
        }

        if (pagesImportees > 0)
        {
            await pages.SaveChangesAsync(cancellationToken);
        }

        return new CahierTexteImportResultDto
        {
            CahierTexteId = cahier.Id,
            PagesImportees = pagesImportees,
            PagesIgnorees = pagesIgnorees,
            Issues = issues
        };
    }

    private static void ValidateRequestShape(
        CahierTexteImportRequestDto request,
        ICollection<CahierTexteValidationIssueDto> issues)
    {
        if (request.AnneeAcademiqueId <= 0 || request.ParcoursAcademiqueId <= 0 || request.ClassePedagogiqueId <= 0)
        {
            AddError(issues, "L'année académique, le parcours et la classe sont obligatoires.");
        }

        if (request.Pages.Count == 0)
        {
            AddError(issues, "Aucune page de cahier n'a été fournie.");
            return;
        }

        foreach (var duplicate in request.Pages.GroupBy(x => x.NumeroPage).Where(x => x.Key <= 0 || x.Count() > 1))
        {
            AddError(issues, duplicate.Key <= 0
                ? "Chaque page doit avoir un numéro strictement positif."
                : $"La page {duplicate.Key} est présente plusieurs fois dans l'import.");
        }

        foreach (var page in request.Pages)
        {
            if (string.IsNullOrWhiteSpace(page.FichierOriginalUri))
            {
                AddError(issues, $"La page {page.NumeroPage} n'a aucune URI de fichier source.", page.NumeroPage);
            }

            if (string.IsNullOrWhiteSpace(page.EmpreinteSha256))
            {
                AddError(issues, $"La page {page.NumeroPage} n'a aucune empreinte SHA-256.", page.NumeroPage);
            }

            if (page.ConfianceOcr is < 0m or > 1m)
            {
                AddError(issues, $"La confiance OCR de la page {page.NumeroPage} doit être comprise entre 0 et 1.", page.NumeroPage);
            }
        }
    }

    private static void AddError(
        ICollection<CahierTexteValidationIssueDto> issues,
        string message,
        int? page = null)
        => issues.Add(new CahierTexteValidationIssueDto
        {
            Severity = CahierTexteValidationSeverity.Erreur,
            NumeroPage = page,
            Message = message
        });
}
