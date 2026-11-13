using RIIS.Academic.Application.Abstractions.Persistence;
using RIIS.Academic.Application.Notes.Dtos;
using RIIS.Academic.Domain;

namespace RIIS.Academic.Application.Notes.Services;

public sealed class NotesCanevasService(
    IRepository<AnneeAcademique> anneesAcademiques,
    IRepository<ClassePedagogique> classesPedagogiques,
    IRepository<DossierAcademique> dossiersAcademiques,
    IRepository<DossierEtudiant> dossiersEtudiants,
    IRepository<Etudiant> etudiants,
    IRepository<ParcoursAcademique> parcoursAcademiques,
    IRepository<MaquettePedagogique> maquettesPedagogiques,
    IRepository<AffectationMaquette> affectationsMaquettes,
    IRepository<SemestrePedagogique> semestresPedagogiques,
    IRepository<MaquetteElementConstitutif> maquetteElementsConstitutifs,
    IRepository<ElementConstitutif> elementsConstitutifs,
    IRepository<UniteEnseignement> unitesEnseignement,
    IRepository<EvaluationAcademique> evaluationsAcademiques,
    IRepository<NoteEvaluation> notesEvaluations) : INotesCanevasService
{
    public async Task<NotesCanevasDto?> GetCanevasAsync(
        long anneeAcademiqueId,
        long classePedagogiqueId,
        long semestrePedagogiqueId,
        long? uniteEnseignementId = null,
        long? elementConstitutifId = null,
        TypeEvaluation? typeEvaluation = null,
        CancellationToken cancellationToken = default)
    {
        var annee = (await anneesAcademiques.ListAsync(cancellationToken))
            .FirstOrDefault(x => x.Id == anneeAcademiqueId);
        var classe = await classesPedagogiques.GetByIdAsync(classePedagogiqueId, cancellationToken);
        if (annee is null)
        {
            throw new NotesCanevasUnavailableException($"L'année académique {anneeAcademiqueId} est introuvable.");
        }

        if (classe is null)
        {
            throw new NotesCanevasUnavailableException($"La classe pédagogique {classePedagogiqueId} est introuvable.");
        }

        var parcours = (await parcoursAcademiques.ListAsync(cancellationToken))
            .FirstOrDefault(x => x.Id == classe.ParcoursAcademiqueId && x.AnneeAcademiqueId == annee.Id);
        if (parcours is null)
        {
            throw new NotesCanevasUnavailableException(
                $"La classe {classe.Libelle} n'est pas rattachée à un parcours de l'année {annee.Libelle}.");
        }

        var maquetteItems = await maquettesPedagogiques.ListAsync(cancellationToken);
        var affectationItems = await affectationsMaquettes.ListAsync(cancellationToken);
        var parcoursAffectations = affectationItems
            .Where(x => x.ParcoursAcademiqueId == parcours.Id)
            .ToList();
        var maquetteIds = parcoursAffectations
            .Where(x => x.EstActive)
            .OrderByDescending(x => x.EstPrincipale)
            .ThenByDescending(x => x.Id)
            .Select(x => x.MaquettePedagogiqueId)
            .Take(1)
            .ToHashSet();

        if (maquetteIds.Count == 0)
        {
            maquetteIds = parcoursAffectations
                .OrderByDescending(x => x.EstPrincipale)
                .ThenByDescending(x => x.Id)
                .Select(x => x.MaquettePedagogiqueId)
                .Take(1)
                .ToHashSet();
        }

        if (maquetteIds.Count == 0)
        {
            maquetteIds = maquetteItems
                .Where(x => x.ParcoursAcademiqueId == parcours.Id)
                .OrderByDescending(x => x.Id)
                .Take(1)
                .Select(x => x.Id)
                .ToHashSet();
        }

        if (maquetteIds.Count == 0)
        {
            throw new NotesCanevasUnavailableException(
                $"Aucune maquette pédagogique n'est affectée au parcours {parcours.Libelle}.");
        }

        var semestreItems = await semestresPedagogiques.ListAsync(cancellationToken);
        var semestres = semestreItems
            .Where(x => maquetteIds.Contains(x.MaquettePedagogiqueId))
            .OrderBy(x => x.NumeroSemestre)
            .ThenBy(x => x.Id)
            .ToList();

        var selectedSemestre = semestres.FirstOrDefault(x => x.Id == semestrePedagogiqueId);
        if (selectedSemestre is null)
        {
            throw new NotesCanevasUnavailableException(
                $"Le semestre pédagogique {semestrePedagogiqueId} n'appartient pas à la maquette affectée à la classe.");
        }

        semestres = [selectedSemestre];
        var semestreIds = semestres.Select(x => x.Id).ToHashSet();
        if (semestres.Count == 0)
        {
            throw new NotesCanevasUnavailableException("La maquette affectée ne contient aucun semestre pédagogique.");
        }

        var maquetteEcItems = (await maquetteElementsConstitutifs.ListAsync(cancellationToken))
            .Where(x => semestreIds.Contains(x.SemestrePedagogiqueId))
            .OrderBy(x => x.SemestrePedagogiqueId)
            .ThenBy(x => x.OrdreAffichage)
            .ThenBy(x => x.Id)
            .ToList();
        var maquetteEcIds = maquetteEcItems.Select(x => x.Id).ToHashSet();
        var ecItems = await elementsConstitutifs.ListAsync(cancellationToken);
        var ueItems = await unitesEnseignement.ListAsync(cancellationToken);
        var ecById = ecItems.ToDictionary(x => x.Id);
        var ueById = ueItems.ToDictionary(x => x.Id);
        var mecById = maquetteEcItems.ToDictionary(x => x.Id);
        var semesterById = semestres.ToDictionary(x => x.Id);

        var evaluationItems = await evaluationsAcademiques.ListAsync(cancellationToken);
        await EnsureDefaultEvaluationsAsync(
            annee,
            maquetteEcItems,
            ecById,
            evaluationItems,
            cancellationToken);

        // Refresh the list after creating missing definitions so the workbook
        // always contains real database identifiers that can be imported later.
        evaluationItems = await evaluationsAcademiques.ListAsync(cancellationToken);

        var evaluations = evaluationItems
            .Where(x => x.AnneeAcademiqueId == annee.Id && maquetteEcIds.Contains(x.MaquetteElementConstitutifId))
            .Where(x => uniteEnseignementId is null
                || (mecById.TryGetValue(x.MaquetteElementConstitutifId, out var mec)
                    && ecById.TryGetValue(mec.ElementConstitutifId, out var ec)
                    && ec.UniteEnseignementId == uniteEnseignementId))
            .Where(x => elementConstitutifId is null
                || (mecById.TryGetValue(x.MaquetteElementConstitutifId, out var mec)
                    && mec.ElementConstitutifId == elementConstitutifId))
            .Where(x => typeEvaluation is null || x.Type == typeEvaluation)
            .OrderBy(x => semesterById[mecById[x.MaquetteElementConstitutifId].SemestrePedagogiqueId].NumeroSemestre)
            .ThenBy(x => maquetteEcItems.FindIndex(m => m.Id == x.MaquetteElementConstitutifId))
            .ThenBy(x => EvaluationTypeOrder(x.Type))
            .ThenBy(x => x.Numero)
            .ThenBy(x => x.Code)
            .ToList();
        var evaluationIds = evaluations.Select(x => x.Id).ToHashSet();

        var dossierItems = await dossiersAcademiques.ListAsync(cancellationToken);
        var dossiers = dossierItems
            // Règle actuelle : un parcours est dispensé dans une seule classe.
            // Le parcours est donc la source de vérité pour inclure tous ses étudiants,
            // y compris les anciens dossiers dont la classe n'a pas été renseignée.
            .Where(x => x.ParcoursAcademiqueId == parcours.Id
                && x.Statut != StatutDossierAcademique.Abandonne)
            .OrderBy(x => x.Id)
            .ToList();
        var dossierIds = dossiers.Select(x => x.Id).ToHashSet();
        var dossierEtudiantItems = await dossiersEtudiants.ListAsync(cancellationToken);
        var etudiantItems = await etudiants.ListAsync(cancellationToken);
        var noteItems = await notesEvaluations.ListAsync(cancellationToken);
        var maquette = maquetteItems.FirstOrDefault(x => maquetteIds.Contains(x.Id));

        if (maquette is null || evaluations.Count == 0)
        {
            throw new NotesCanevasUnavailableException(
                evaluations.Count == 0
                    ? "Aucune évaluation académique n'est enregistrée pour les EC de la maquette sélectionnée."
                    : "La maquette pédagogique sélectionnée est introuvable.");
        }

        return new NotesCanevasDto
        {
            AnneeAcademiqueId = annee.Id,
            AnneeAcademiqueLibelle = annee.Libelle,
            ParcoursAcademiqueId = parcours.Id,
            ParcoursAcademiqueCode = parcours.Code,
            ParcoursAcademiqueLibelle = parcours.Libelle,
            ClassePedagogiqueId = classe.Id,
            ClassePedagogiqueLibelle = classe.Libelle,
            MaquettePedagogiqueId = maquette.Id,
            MaquettePedagogiqueCode = maquette.Code,
            MaquettePedagogiqueLibelle = maquetteIds.Count == 1 ? maquette.Libelle : "Maquettes du parcours",
            MaquettePedagogiqueVersion = maquetteIds.Count == 1 ? maquette.Version : string.Empty,
            SemestrePedagogiqueId = selectedSemestre.Id,
            NumeroSemestre = selectedSemestre.NumeroSemestre,
            SemestrePedagogiqueLibelle = selectedSemestre.Libelle,
            Evaluations = evaluations.Select(x =>
            {
                var mec = mecById[x.MaquetteElementConstitutifId];
                var ec = ecById[mec.ElementConstitutifId];
                var ue = ueById[ec.UniteEnseignementId];
                var semestre = semesterById[mec.SemestrePedagogiqueId];

                return new NotesCanevasEvaluationDto
                {
                    Id = x.Id,
                    Code = x.Code,
                    Libelle = x.Libelle,
                    Type = x.Type,
                    Numero = x.Numero,
                    Bareme = x.Bareme,
                    PonderationPourcentage = x.PonderationPourcentage,
                    MaquetteElementConstitutifId = mec.Id,
                    SemestrePedagogiqueId = semestre.Id,
                    NumeroSemestre = semestre.NumeroSemestre,
                    SemestrePedagogiqueLibelle = semestre.Libelle,
                    ElementConstitutifCode = ec.Code ?? string.Empty,
                    ElementConstitutifLibelle = ec.Libelle,
                    UniteEnseignementId = ue.Id,
                    UniteEnseignementCode = ue.Code ?? string.Empty,
                    UniteEnseignementLibelle = ue.Libelle
                };
            }).ToList(),
            Etudiants = dossiers.Select(x =>
            {
                var etudiant = etudiantItems.FirstOrDefault(e => e.Id == x.EtudiantId);
                var dossierEtudiant = dossierEtudiantItems.FirstOrDefault(d =>
                    d.EtudiantId == x.EtudiantId && d.ParcoursAcademiqueId == x.ParcoursAcademiqueId);

                return new NotesCanevasEtudiantDto
                {
                    DossierAcademiqueId = x.Id,
                    EtudiantId = x.EtudiantId,
                    Matricule = dossierEtudiant?.Matricule ?? string.Empty,
                    NomComplet = etudiant is null ? string.Empty : $"{etudiant.Nom} {etudiant.Prenoms}".Trim()
                };
            }).OrderBy(x => x.NomComplet).ToList(),
            Notes = noteItems
                .Where(x => evaluationIds.Contains(x.EvaluationAcademiqueId) && dossierIds.Contains(x.DossierAcademiqueId))
                .Select(x => new NotesCanevasNoteDto
                {
                    DossierAcademiqueId = x.DossierAcademiqueId,
                    EvaluationAcademiqueId = x.EvaluationAcademiqueId,
                    Valeur = x.Valeur,
                    StatutPresence = x.StatutPresence,
                    Observation = x.Observation
                }).ToList()
        };
    }

    private async Task EnsureDefaultEvaluationsAsync(
        AnneeAcademique annee,
        IReadOnlyCollection<MaquetteElementConstitutif> maquetteElements,
        IReadOnlyDictionary<long, ElementConstitutif> ecById,
        IReadOnlyCollection<EvaluationAcademique> existingEvaluations,
        CancellationToken cancellationToken)
    {
        var definitions = new[]
        {
            new EvaluationDefinition(TypeEvaluation.ControleContinu, "CCON", "Contrôle continu", 20m),
            new EvaluationDefinition(TypeEvaluation.ControleConnaissance, "CC", "Contrôle de connaissance", 10m),
            new EvaluationDefinition(TypeEvaluation.SessionNormale, "SN", "Session normale", 70m)
        };

        var created = 0;
        foreach (var maquetteElement in maquetteElements)
        {
            if (!ecById.TryGetValue(maquetteElement.ElementConstitutifId, out var ec))
            {
                continue;
            }

            foreach (var definition in definitions)
            {
                var exists = existingEvaluations.Any(x =>
                    x.AnneeAcademiqueId == annee.Id
                    && x.MaquetteElementConstitutifId == maquetteElement.Id
                    && x.Type == definition.Type
                    && x.Numero == 1);

                if (exists)
                {
                    continue;
                }

                await evaluationsAcademiques.AddAsync(new EvaluationAcademique
                {
                    AnneeAcademiqueId = annee.Id,
                    MaquetteElementConstitutifId = maquetteElement.Id,
                    Type = definition.Type,
                    Numero = 1,
                    Code = Truncate($"{definition.CodePrefix}-{ec.Code ?? $"EC{ec.Id}"}", 50),
                    Libelle = Truncate($"{definition.Libelle} - {ec.Libelle}", 200),
                    Bareme = 20m,
                    PonderationPourcentage = definition.PonderationPourcentage,
                    Observation = "Créée automatiquement pour le canevas de saisie des notes."
                }, cancellationToken);

                created++;
            }
        }

        if (created > 0)
        {
            await evaluationsAcademiques.SaveChangesAsync(cancellationToken);
        }
    }

    private static string Truncate(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength];

    private sealed record EvaluationDefinition(
        TypeEvaluation Type,
        string CodePrefix,
        string Libelle,
        decimal PonderationPourcentage);

    private static int EvaluationTypeOrder(TypeEvaluation type)
        => type switch
        {
            TypeEvaluation.ControleContinu => 1,
            TypeEvaluation.ControleConnaissance => 2,
            TypeEvaluation.SessionNormale => 3,
            TypeEvaluation.SessionRattrapage => 4,
            _ => 99
        };
}

public sealed class NotesCanevasUnavailableException(string message) : InvalidOperationException(message);
