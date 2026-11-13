using RIIS.Academic.Application.Abstractions.Persistence;
using RIIS.Academic.Application.Cours.Dtos;
using RIIS.Academic.Domain;

namespace RIIS.Academic.Application.Cours.Services;

/// <summary>
/// Controle le rattachement des donnees OCR au referentiel exact du parcours.
/// Une seance n'est persistée qu'après validation explicite.
/// </summary>
public sealed class CahierTexteValidationService(
    IRepository<AnneeAcademique> anneesAcademiques,
    IRepository<ParcoursAcademique> parcoursAcademiques,
    IRepository<ClassePedagogique> classesPedagogiques,
    IRepository<CahierTexte> cahiersTextes,
    IRepository<CahierTextePage> pages,
    IRepository<SeanceCours> seancesCours,
    IRepository<AffectationMaquette> affectationsMaquettes,
    IRepository<MaquettePedagogique> maquettesPedagogiques,
    IRepository<SemestrePedagogique> semestresPedagogiques,
    IRepository<MaquetteElementConstitutif> maquetteElementsConstitutifs,
    IRepository<ElementConstitutif> elementsConstitutifs,
    IRepository<UniteEnseignement> unitesEnseignement) : ICahierTexteValidationService
{
    public async Task<CahierTexteValidationReportDto> ValiderAsync(
        CahierTexteImportRequestDto request,
        long? cahierTexteId = null,
        CancellationToken cancellationToken = default)
    {
        var globalIssues = new List<CahierTexteValidationIssueDto>();
        var allSeances = request.Pages
            .SelectMany(page => page.Seances.Select((seance, index) => new Candidate(page, seance, index)))
            .ToList();

        var annee = await anneesAcademiques.GetByIdAsync(request.AnneeAcademiqueId, cancellationToken);
        var parcours = await parcoursAcademiques.GetByIdAsync(request.ParcoursAcademiqueId, cancellationToken);
        var classe = await classesPedagogiques.GetByIdAsync(request.ClassePedagogiqueId, cancellationToken);

        if (cahierTexteId is not null)
        {
            var cahier = await cahiersTextes.GetByIdAsync(cahierTexteId.Value, cancellationToken);
            if (cahier is null)
            {
                AddGlobalError(globalIssues, $"Le cahier de texte {cahierTexteId} est introuvable.");
            }
            else if (cahier.AnneeAcademiqueId != request.AnneeAcademiqueId
                || cahier.ParcoursAcademiqueId != request.ParcoursAcademiqueId
                || cahier.ClassePedagogiqueId != request.ClassePedagogiqueId)
            {
                AddGlobalError(globalIssues, "Le cahier de texte ne correspond pas aux référentiels sélectionnés.");
            }
        }

        if (annee is null)
        {
            AddGlobalError(globalIssues, "L'année académique est introuvable.");
        }

        if (parcours is null || !parcours.EstActive)
        {
            AddGlobalError(globalIssues, "Le parcours académique est introuvable ou inactif.");
        }

        if (classe is null)
        {
            AddGlobalError(globalIssues, "La classe pédagogique est introuvable.");
        }
        else if (classe.ParcoursAcademiqueId != request.ParcoursAcademiqueId)
        {
            AddGlobalError(globalIssues, "La classe pédagogique ne correspond pas au parcours académique.");
        }

        if (annee is not null && parcours is not null && parcours.AnneeAcademiqueId != annee.Id)
        {
            AddGlobalError(globalIssues, "Le parcours académique ne correspond pas à l'année sélectionnée.");
        }

        var affectations = await affectationsMaquettes.ListAsync(cancellationToken);
        var maquettes = await maquettesPedagogiques.ListAsync(cancellationToken);
        var semestres = await semestresPedagogiques.ListAsync(cancellationToken);
        var maquetteElements = await maquetteElementsConstitutifs.ListAsync(cancellationToken);
        var elements = await elementsConstitutifs.ListAsync(cancellationToken);
        var unites = await unitesEnseignement.ListAsync(cancellationToken);

        var maquetteIds = affectations
            .Where(x => x.ParcoursAcademiqueId == request.ParcoursAcademiqueId && x.EstActive)
            .Select(x => x.MaquettePedagogiqueId)
            .ToHashSet();

        if (maquetteIds.Count == 0)
        {
            maquetteIds = maquettes
                .Where(x => x.ParcoursAcademiqueId == request.ParcoursAcademiqueId)
                .Select(x => x.Id)
                .ToHashSet();
        }

        if (maquetteIds.Count == 0)
        {
            AddGlobalError(globalIssues, "Aucune maquette pédagogique n'est affectée au parcours académique.");
        }

        var semestreIds = semestres
            .Where(x => maquetteIds.Contains(x.MaquettePedagogiqueId))
            .Select(x => x.Id)
            .ToHashSet();
        var targetMaquetteElements = maquetteElements
            .Where(x => semestreIds.Contains(x.SemestrePedagogiqueId))
            .ToList();
        var elementById = elements.ToDictionary(x => x.Id);
        var uniteById = unites.ToDictionary(x => x.Id);
        var semestreById = semestres.ToDictionary(x => x.Id);
        var validationByCandidate = new List<SeanceCoursValidationDto>();
        var duplicateKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var candidate in allSeances)
        {
            var issues = new List<CahierTexteValidationIssueDto>();
            var matchingMecs = ResolveMaquetteElements(candidate.Seance, targetMaquetteElements, elementById);
            var selectedMec = matchingMecs.Count == 1 ? matchingMecs[0] : null;

            if (candidate.Page.NumeroPage <= 0)
            {
                AddSeanceError(issues, candidate, "Le numéro de page est obligatoire.");
            }

            if (candidate.Seance.DateCours is null)
            {
                AddSeanceError(issues, candidate, "La date du cours est obligatoire.");
            }

            if (candidate.Seance.HeureDebut is not null
                && candidate.Seance.HeureFin is not null
                && candidate.Seance.HeureFin < candidate.Seance.HeureDebut)
            {
                AddSeanceError(issues, candidate, "L'heure de fin ne peut pas précéder l'heure de début.");
            }

            if (candidate.Seance.ConfianceExtraction is < 0m or > 1m)
            {
                AddSeanceError(issues, candidate, "La confiance d'extraction doit être comprise entre 0 et 1.");
            }

            if (matchingMecs.Count == 0)
            {
                AddSeanceError(issues, candidate, "Aucun EC de la maquette active ne correspond aux informations extraites.");
            }
            else if (matchingMecs.Count > 1)
            {
                AddSeanceError(issues, candidate, "Plusieurs EC correspondent aux informations extraites ; sélectionnez l'EC manuellement.");
            }

            if (selectedMec is not null && !elementById.TryGetValue(selectedMec.ElementConstitutifId, out var element))
            {
                AddSeanceError(issues, candidate, "L'EC rattaché à la maquette est introuvable.");
            }
            else if (selectedMec is not null && elementById.TryGetValue(selectedMec.ElementConstitutifId, out element))
            {
                if (!uniteById.ContainsKey(element.UniteEnseignementId))
                {
                    AddSeanceError(issues, candidate, "L'UE de l'EC rattaché à la maquette est introuvable.");
                }

                if (candidate.Seance.MaquetteElementConstitutifId is null
                    && !string.IsNullOrWhiteSpace(candidate.Seance.ElementConstitutifCode)
                    && !string.Equals(
                        candidate.Seance.ElementConstitutifCode.Trim(),
                        element.Code,
                        StringComparison.OrdinalIgnoreCase))
                {
                    AddSeanceError(issues, candidate, "Le code EC extrait ne correspond pas à l'EC résolu.");
                }
            }

            if (selectedMec is not null && candidate.Seance.DateCours is not null)
            {
                var duplicateKey = $"{selectedMec.Id}|{candidate.Seance.DateCours:yyyyMMdd}|{candidate.Page.NumeroPage}";
                if (!duplicateKeys.Add(duplicateKey))
                {
                    AddSeanceError(issues, candidate, "La même séance apparaît plusieurs fois dans l'import.");
                }
            }

            if (HasNoCourseContent(candidate.Seance))
            {
                issues.Add(new CahierTexteValidationIssueDto
                {
                    Severity = CahierTexteValidationSeverity.Avertissement,
                    NumeroPage = candidate.Page.NumeroPage,
                    OrdreExtraction = candidate.Index,
                    Message = "La séance ne contient aucun contenu pédagogique extrait ; une correction manuelle est recommandée."
                });
            }

            validationByCandidate.Add(new SeanceCoursValidationDto
            {
                NumeroPage = candidate.Page.NumeroPage,
                OrdreExtraction = candidate.Index,
                EstValide = issues.All(x => x.Severity != CahierTexteValidationSeverity.Erreur),
                MaquetteElementConstitutifId = selectedMec?.Id,
                SemestrePedagogiqueId = selectedMec is null
                    ? null
                    : targetMaquetteElements.First(x => x.Id == selectedMec.Id).SemestrePedagogiqueId,
                UniteEnseignementId = selectedMec is not null && elementById.TryGetValue(selectedMec.ElementConstitutifId, out var resolvedElement)
                    ? resolvedElement.UniteEnseignementId
                    : null,
                ElementConstitutifId = selectedMec?.ElementConstitutifId,
                ElementConstitutifCode = selectedMec is not null && elementById.TryGetValue(selectedMec.ElementConstitutifId, out var resolvedCode)
                    ? resolvedCode.Code
                    : null,
                ElementConstitutifLibelle = selectedMec is not null && elementById.TryGetValue(selectedMec.ElementConstitutifId, out var resolvedLabel)
                    ? resolvedLabel.Libelle
                    : null,
                Issues = issues
            });
        }

        return new CahierTexteValidationReportDto
        {
            CahierTexteId = cahierTexteId,
            Issues = globalIssues,
            Seances = validationByCandidate
        };
    }

    public async Task<CahierTexteSeancesSaveResultDto> EnregistrerSeancesValideesAsync(
        long cahierTexteId,
        CahierTexteImportRequestDto request,
        CahierTexteValidationReportDto validation,
        CancellationToken cancellationToken = default)
    {
        var issues = validation.Issues.ToList();
        if (issues.Any(x => x.Severity == CahierTexteValidationSeverity.Erreur)
            || validation.Seances.SelectMany(x => x.Issues).Any(x => x.Severity == CahierTexteValidationSeverity.Erreur))
        {
            issues.Add(new CahierTexteValidationIssueDto
            {
                Severity = CahierTexteValidationSeverity.Erreur,
                Message = "Enregistrement impossible : corrigez les erreurs de validation avant de confirmer les séances."
            });
            return new CahierTexteSeancesSaveResultDto { Issues = issues };
        }

        var cahier = await cahiersTextes.GetByIdAsync(cahierTexteId, cancellationToken);
        if (cahier is null)
        {
            issues.Add(new CahierTexteValidationIssueDto
            {
                Severity = CahierTexteValidationSeverity.Erreur,
                Message = "Le cahier de texte est introuvable."
            });
            return new CahierTexteSeancesSaveResultDto { Issues = issues };
        }

        var pageEntities = (await pages.ListAsync(cancellationToken))
            .Where(x => x.CahierTexteId == cahierTexteId)
            .ToList();
        var existingSeances = (await seancesCours.ListAsync(cancellationToken))
            .Where(x => x.CahierTexteId == cahierTexteId)
            .ToList();
        var candidates = request.Pages
            .SelectMany(page => page.Seances.Select((seance, index) => new Candidate(page, seance, index)))
            .ToList();
        var saved = 0;
        var ignored = 0;

        foreach (var valid in validation.Seances.Where(x => x.EstValide))
        {
            var candidate = candidates.FirstOrDefault(x =>
                x.Page.NumeroPage == valid.NumeroPage && x.Index == valid.OrdreExtraction);
            if (candidate is null || valid.MaquetteElementConstitutifId is null || candidate.Seance.DateCours is null)
            {
                ignored++;
                continue;
            }

            var page = pageEntities.FirstOrDefault(x => x.NumeroPage == candidate.Page.NumeroPage);
            var existing = existingSeances.FirstOrDefault(x =>
                x.MaquetteElementConstitutifId == valid.MaquetteElementConstitutifId.Value
                && x.DateCours == candidate.Seance.DateCours.Value
                && x.CahierTextePageId == page?.Id
                && string.Equals(x.TitreLecon, candidate.Seance.TitreLecon, StringComparison.Ordinal));

            if (existing is null)
            {
                existing = new SeanceCours
                {
                    CahierTexteId = cahierTexteId,
                    MaquetteElementConstitutifId = valid.MaquetteElementConstitutifId.Value,
                    CahierTextePageId = page?.Id,
                    DateCours = candidate.Seance.DateCours.Value
                };
                await seancesCours.AddAsync(existing, cancellationToken);
                existingSeances.Add(existing);
            }

            ApplyCandidate(existing, candidate.Seance, page?.Id);
            existing.Statut = StatutSeanceCours.Validee;
            existing.ModifieLeUtc = DateTime.UtcNow;
            saved++;
        }

        foreach (var page in pageEntities)
        {
            var pageCandidates = candidates.Where(x => x.Page.NumeroPage == page.NumeroPage).ToList();
            if (pageCandidates.Count == 0)
            {
                continue;
            }

            var allPageCandidatesValid = pageCandidates.All(candidate => validation.Seances.Any(x =>
                x.NumeroPage == candidate.Page.NumeroPage
                && x.OrdreExtraction == candidate.Index
                && x.EstValide));
            page.Statut = allPageCandidatesValid
                ? StatutPageCahierTexte.Validee
                : StatutPageCahierTexte.AValider;
            page.TraiteeLeUtc = DateTime.UtcNow;
        }

        if (saved > 0 || pageEntities.Any(x => x.TraiteeLeUtc is not null))
        {
            await seancesCours.SaveChangesAsync(cancellationToken);
        }

        return new CahierTexteSeancesSaveResultDto
        {
            SeancesEnregistrees = saved,
            SeancesIgnorees = ignored,
            Issues = issues
        };
    }

    private static List<MaquetteElementConstitutif> ResolveMaquetteElements(
        SeanceCoursImportDto seance,
        IReadOnlyCollection<MaquetteElementConstitutif> maquetteElements,
        IReadOnlyDictionary<long, ElementConstitutif> elements)
    {
        if (seance.MaquetteElementConstitutifId is not null)
        {
            return maquetteElements
                .Where(x => x.Id == seance.MaquetteElementConstitutifId.Value)
                .ToList();
        }

        var withElements = maquetteElements
            .Where(x => elements.ContainsKey(x.ElementConstitutifId))
            .ToList();
        if (!string.IsNullOrWhiteSpace(seance.ElementConstitutifCode))
        {
            var code = seance.ElementConstitutifCode.Trim();
            var byCode = withElements
                .Where(x => string.Equals(elements[x.ElementConstitutifId].Code, code, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (byCode.Count > 0)
            {
                return byCode;
            }
        }

        if (!string.IsNullOrWhiteSpace(seance.ElementConstitutifLibelle))
        {
            var label = Normalize(seance.ElementConstitutifLibelle);
            return withElements
                .Where(x => Normalize(elements[x.ElementConstitutifId].Libelle) == label)
                .ToList();
        }

        return [];
    }

    private static void ApplyCandidate(SeanceCours target, SeanceCoursImportDto source, long? pageId)
    {
        target.CahierTextePageId = pageId;
        target.DateCours = source.DateCours!.Value;
        target.HeureDebut = source.HeureDebut;
        target.HeureFin = source.HeureFin;
        target.Nature = source.Nature;
        target.Module = source.Module;
        target.NumeroChapitre = source.NumeroChapitre;
        target.TitreLecon = source.TitreLecon;
        target.Paragraphe = source.Paragraphe;
        target.PointsEssentiels = source.PointsEssentiels;
        target.PointsPrincipaux = source.PointsPrincipaux;
        target.ExercicesApplications = source.ExercicesApplications;
        target.DevoirsMaison = source.DevoirsMaison;
        target.Evaluation = source.Evaluation;
        target.CompetencesObjectifs = source.CompetencesObjectifs;
        target.Observation = source.Observation;
        target.ConfianceExtraction = source.ConfianceExtraction;
    }

    private static bool HasNoCourseContent(SeanceCoursImportDto source)
        => string.IsNullOrWhiteSpace(source.TitreLecon)
            && string.IsNullOrWhiteSpace(source.Paragraphe)
            && string.IsNullOrWhiteSpace(source.PointsEssentiels)
            && string.IsNullOrWhiteSpace(source.PointsPrincipaux)
            && string.IsNullOrWhiteSpace(source.ExercicesApplications)
            && string.IsNullOrWhiteSpace(source.DevoirsMaison)
            && string.IsNullOrWhiteSpace(source.Evaluation)
            && string.IsNullOrWhiteSpace(source.CompetencesObjectifs);

    private static string Normalize(string value)
        => string.Join(' ', value.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            .ToUpperInvariant();

    private static void AddGlobalError(ICollection<CahierTexteValidationIssueDto> issues, string message)
        => issues.Add(new CahierTexteValidationIssueDto
        {
            Severity = CahierTexteValidationSeverity.Erreur,
            Message = message
        });

    private static void AddSeanceError(
        ICollection<CahierTexteValidationIssueDto> issues,
        Candidate candidate,
        string message)
        => issues.Add(new CahierTexteValidationIssueDto
        {
            Severity = CahierTexteValidationSeverity.Erreur,
            NumeroPage = candidate.Page.NumeroPage,
            OrdreExtraction = candidate.Index,
            Message = message
        });

    private sealed record Candidate(
        CahierTextePageImportDto Page,
        SeanceCoursImportDto Seance,
        int Index);
}
