using RIIS.Academic.Application.Abstractions.Persistence;
using RIIS.Academic.Application.Cours.Dtos;
using RIIS.Academic.Domain;

namespace RIIS.Academic.Application.Cours.Services;

public sealed class ReferentielCoursValidationService(
    IRepository<ParcoursAcademique> parcoursAcademiques,
    IRepository<ClassePedagogique> classesPedagogiques,
    IRepository<AffectationMaquette> affectationsMaquettes,
    IRepository<MaquettePedagogique> maquettesPedagogiques,
    IRepository<SemestrePedagogique> semestresPedagogiques,
    IRepository<NiveauEtude> niveauxEtude,
    IRepository<MaquetteElementConstitutif> maquetteElementsConstitutifs,
    IRepository<ElementConstitutif> elementsConstitutifs,
    IRepository<UniteEnseignement> unitesEnseignement) : IReferentielCoursValidationService
{
    public async Task<ReferentielCoursValidationReportDto> ValidateAsync(
        CancellationToken cancellationToken = default)
    {
        var parcours = (await parcoursAcademiques.ListAsync(cancellationToken))
            .Where(x => x.EstActive)
            .OrderBy(x => x.Libelle)
            .ToList();
        var classes = await classesPedagogiques.ListAsync(cancellationToken);
        var affectations = await affectationsMaquettes.ListAsync(cancellationToken);
        var maquettes = await maquettesPedagogiques.ListAsync(cancellationToken);
        var semestres = await semestresPedagogiques.ListAsync(cancellationToken);
        var niveaux = await niveauxEtude.ListAsync(cancellationToken);
        var maquetteElements = await maquetteElementsConstitutifs.ListAsync(cancellationToken);
        var elements = await elementsConstitutifs.ListAsync(cancellationToken);
        var unites = await unitesEnseignement.ListAsync(cancellationToken);

        var issues = new List<ReferentielCoursValidationIssueDto>();
        var parcoursExploitables = 0;
        var niveauById = niveaux.ToDictionary(x => x.Id);
        var maquetteById = maquettes.ToDictionary(x => x.Id);
        var semestreById = semestres.ToDictionary(x => x.Id);
        var elementById = elements.ToDictionary(x => x.Id);
        var uniteById = unites.ToDictionary(x => x.Id);

        foreach (var parcoursItem in parcours)
        {
            var parcoursIssuesBefore = issues.Count;
            var parcoursClasses = classes
                .Where(x => x.ParcoursAcademiqueId == parcoursItem.Id)
                .ToList();

            if (parcoursClasses.Count == 0)
            {
                AddIssue(
                    issues,
                    ReferentielValidationSeverity.Erreur,
                    parcoursItem,
                    "Aucune classe pédagogique n'est rattachée à ce parcours.");
            }
            else if (parcoursClasses.Count > 1)
            {
                AddIssue(
                    issues,
                    ReferentielValidationSeverity.Avertissement,
                    parcoursItem,
                    $"{parcoursClasses.Count} classes pédagogiques sont rattachées à ce parcours ; le workflow actuel en attend une seule.");
            }

            var activeAffectations = affectations
                .Where(x => x.ParcoursAcademiqueId == parcoursItem.Id && x.EstActive)
                .ToList();
            var maquetteIds = activeAffectations
                .Select(x => x.MaquettePedagogiqueId)
                .ToHashSet();

            if (maquetteIds.Count == 0)
            {
                var legacyMaquetteIds = maquettes
                    .Where(x => x.ParcoursAcademiqueId == parcoursItem.Id)
                    .Select(x => x.Id)
                    .ToHashSet();

                if (legacyMaquetteIds.Count > 0)
                {
                    maquetteIds = legacyMaquetteIds;
                    AddIssue(
                        issues,
                        ReferentielValidationSeverity.Avertissement,
                        parcoursItem,
                        "Le parcours utilise encore une ancienne liaison directe vers la maquette ; une AffectationMaquette active est recommandée.");
                }
                else
                {
                    AddIssue(
                        issues,
                        ReferentielValidationSeverity.Erreur,
                        parcoursItem,
                        "Aucune maquette pédagogique active n'est affectée à ce parcours.");
                }
            }

            foreach (var maquetteId in maquetteIds)
            {
                if (!maquetteById.TryGetValue(maquetteId, out var maquette))
                {
                    AddIssue(
                        issues,
                        ReferentielValidationSeverity.Erreur,
                        parcoursItem,
                        $"L'affectation référence une maquette pédagogique inexistante (Id {maquetteId}).",
                        maquetteId: maquetteId);
                    continue;
                }

                ValidateMaquette(
                    parcoursItem,
                    maquette,
                    semestres,
                    niveauById,
                    maquetteElements,
                    elementById,
                    uniteById,
                    issues);
            }

            if (!issues
                .Skip(parcoursIssuesBefore)
                .Any(x => x.Severity == ReferentielValidationSeverity.Erreur))
            {
                parcoursExploitables++;
            }
        }

        return new ReferentielCoursValidationReportDto
        {
            ParcoursAnalyses = parcours.Count,
            ParcoursExploitables = parcoursExploitables,
            Issues = issues
        };
    }

    private static void ValidateMaquette(
        ParcoursAcademique parcours,
        MaquettePedagogique maquette,
        IReadOnlyCollection<SemestrePedagogique> semestres,
        IReadOnlyDictionary<long, NiveauEtude> niveaux,
        IReadOnlyCollection<MaquetteElementConstitutif> maquetteElements,
        IReadOnlyDictionary<long, ElementConstitutif> elements,
        IReadOnlyDictionary<long, UniteEnseignement> unites,
        ICollection<ReferentielCoursValidationIssueDto> issues)
    {
        var maquetteSemestres = semestres
            .Where(x => x.MaquettePedagogiqueId == maquette.Id)
            .OrderBy(x => x.NumeroSemestre)
            .ToList();

        if (maquetteSemestres.Count == 0)
        {
            AddIssue(
                issues,
                ReferentielValidationSeverity.Erreur,
                parcours,
                $"La maquette '{maquette.Code}' ne contient aucun semestre pédagogique.",
                maquetteId: maquette.Id);
            return;
        }

        foreach (var semestre in maquetteSemestres)
        {
            var expectedLevel = (semestre.NumeroSemestre + 1) / 2;
            if (semestre.NiveauEtudeId is null)
            {
                AddIssue(
                    issues,
                    ReferentielValidationSeverity.Erreur,
                    parcours,
                    $"Le semestre '{semestre.Libelle}' n'a aucun niveau d'étude.",
                    maquetteId: maquette.Id,
                    semestreId: semestre.Id);
            }
            else if (!niveaux.TryGetValue(semestre.NiveauEtudeId.Value, out var niveau))
            {
                AddIssue(
                    issues,
                    ReferentielValidationSeverity.Erreur,
                    parcours,
                    $"Le semestre '{semestre.Libelle}' référence un niveau d'étude inexistant.",
                    maquetteId: maquette.Id,
                    semestreId: semestre.Id);
            }
            else if (niveau.Numero != expectedLevel)
            {
                AddIssue(
                    issues,
                    ReferentielValidationSeverity.Erreur,
                    parcours,
                    $"Le semestre {semestre.NumeroSemestre} est rattaché au niveau {niveau.Numero}, alors qu'il devrait être au niveau {expectedLevel}.",
                    maquetteId: maquette.Id,
                    semestreId: semestre.Id);
            }

            var semestreElements = maquetteElements
                .Where(x => x.SemestrePedagogiqueId == semestre.Id)
                .ToList();
            if (semestreElements.Count == 0)
            {
                AddIssue(
                    issues,
                    ReferentielValidationSeverity.Erreur,
                    parcours,
                    $"Le semestre '{semestre.Libelle}' ne contient aucun EC rattaché.",
                    maquetteId: maquette.Id,
                    semestreId: semestre.Id);
            }

            foreach (var maquetteElement in semestreElements)
            {
                if (!elements.TryGetValue(maquetteElement.ElementConstitutifId, out var element))
                {
                    AddIssue(
                        issues,
                        ReferentielValidationSeverity.Erreur,
                        parcours,
                        $"Le semestre '{semestre.Libelle}' référence un EC inexistant (Id {maquetteElement.ElementConstitutifId}).",
                        maquetteId: maquette.Id,
                        semestreId: semestre.Id,
                        elementId: maquetteElement.ElementConstitutifId);
                    continue;
                }

                if (!unites.TryGetValue(element.UniteEnseignementId, out var unite))
                {
                    AddIssue(
                        issues,
                        ReferentielValidationSeverity.Erreur,
                        parcours,
                        $"L'EC '{element.Libelle}' est rattaché à une UE inexistante.",
                        maquetteId: maquette.Id,
                        semestreId: semestre.Id,
                        elementId: element.Id);
                    continue;
                }

                if (unite.MaquettePedagogiqueId is not null
                    && unite.MaquettePedagogiqueId != maquette.Id)
                {
                    AddIssue(
                        issues,
                        ReferentielValidationSeverity.Erreur,
                        parcours,
                        $"L'UE '{unite.Libelle}' de l'EC '{element.Libelle}' appartient à une autre maquette.",
                        maquetteId: maquette.Id,
                        semestreId: semestre.Id,
                        elementId: element.Id);
                }
            }
        }

        AddDuplicateLabelIssues(
            parcours,
            maquette,
            maquetteSemestres,
            maquetteElements,
            elements,
            unites,
            issues);
    }

    private static void AddDuplicateLabelIssues(
        ParcoursAcademique parcours,
        MaquettePedagogique maquette,
        IReadOnlyCollection<SemestrePedagogique> semestres,
        IReadOnlyCollection<MaquetteElementConstitutif> maquetteElements,
        IReadOnlyDictionary<long, ElementConstitutif> elements,
        IReadOnlyDictionary<long, UniteEnseignement> unites,
        ICollection<ReferentielCoursValidationIssueDto> issues)
    {
        var elementRows = maquetteElements
            .Where(x => semestres.Any(s => s.Id == x.SemestrePedagogiqueId))
            .Select(x => new
            {
                MaquetteElement = x,
                Element = elements.GetValueOrDefault(x.ElementConstitutifId),
                Unite = elements.TryGetValue(x.ElementConstitutifId, out var element)
                    ? unites.GetValueOrDefault(element.UniteEnseignementId)
                    : null
            })
            .Where(x => x.Element is not null && x.Unite is not null)
            .ToList();

        foreach (var group in elementRows.GroupBy(x => NormalizeLabel(x.Unite!.Libelle)))
        {
            if (group.Select(x => x.Unite!.Id).Distinct().Count() > 1)
            {
                AddIssue(
                    issues,
                    ReferentielValidationSeverity.Avertissement,
                    parcours,
                    $"La maquette '{maquette.Code}' contient plusieurs UE portant le même libellé '{group.First().Unite!.Libelle}'. La sélection utilisateur peut être ambiguë.",
                    maquetteId: maquette.Id);
            }
        }

        foreach (var group in elementRows.GroupBy(x => NormalizeLabel(x.Element!.Libelle)))
        {
            if (group.Select(x => x.Element!.Id).Distinct().Count() > 1)
            {
                AddIssue(
                    issues,
                    ReferentielValidationSeverity.Avertissement,
                    parcours,
                    $"La maquette '{maquette.Code}' contient plusieurs EC portant le même libellé '{group.First().Element!.Libelle}'. La sélection utilisateur peut être ambiguë.",
                    maquetteId: maquette.Id);
            }
        }
    }

    private static void AddIssue(
        ICollection<ReferentielCoursValidationIssueDto> issues,
        ReferentielValidationSeverity severity,
        ParcoursAcademique parcours,
        string message,
        long? maquetteId = null,
        long? semestreId = null,
        long? elementId = null)
        => issues.Add(new ReferentielCoursValidationIssueDto
        {
            Severity = severity,
            ParcoursAcademiqueId = parcours.Id,
            ParcoursLibelle = parcours.Libelle,
            MaquettePedagogiqueId = maquetteId,
            SemestrePedagogiqueId = semestreId,
            ElementConstitutifId = elementId,
            Message = message
        });

    private static string NormalizeLabel(string value)
        => string.Join(' ', value.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            .ToUpperInvariant();
}
