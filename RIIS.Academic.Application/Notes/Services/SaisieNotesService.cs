using RIIS.Academic.Application.Abstractions.Persistence;
using RIIS.Academic.Application.Common.Dtos;
using RIIS.Academic.Application.Notes.Dtos;
using RIIS.Academic.Domain;

namespace RIIS.Academic.Application.Notes.Services;

public class SaisieNotesService(
    IRepository<AnneeAcademique> anneesAcademiques,
    IRepository<ClassePedagogique> classesPedagogiques,
    IRepository<DossierAcademique> dossiersAcademiques,
    IRepository<DossierEtudiant> dossiersEtudiants,
    IRepository<ParcoursAcademique> parcoursAcademiques,
    IRepository<NiveauEtude> niveauxEtude,
    IRepository<Etudiant> etudiants,
    IRepository<EvaluationAcademique> evaluations,
    IRepository<NoteEvaluation> notesEvaluations,
    IRepository<MaquetteElementConstitutif> maquetteElementsConstitutifs,
    IRepository<SemestrePedagogique> semestresPedagogiques,
    IRepository<MaquettePedagogique> maquettesPedagogiques,
    IRepository<AffectationMaquette> affectationsMaquettes,
    IRepository<UniteEnseignement> unitesEnseignement,
    IRepository<ElementConstitutif> elementsConstitutifs,
    IRepository<ResultatSemestre> resultatsSemestres) : ISaisieNotesService
{
    public async Task<List<LookupDto>> GetAnneesAcademiquesLookupAsync(CancellationToken cancellationToken = default)
    {
        var items = await anneesAcademiques.ListAsync(cancellationToken);

        return items
            .OrderByDescending(x => x.AnneeDebut)
            .Select(x => new LookupDto { Id = x.Id, Libelle = x.Libelle })
            .ToList();
    }

    public async Task<List<LookupDto>> GetSemestresForClasseLookupAsync(
        long anneeAcademiqueId,
        long classePedagogiqueId,
        CancellationToken cancellationToken = default)
    {
        var classe = await classesPedagogiques.GetByIdAsync(classePedagogiqueId, cancellationToken);
        if (classe is null)
        {
            return [];
        }

        var parcours = (await parcoursAcademiques.ListAsync(cancellationToken))
            .FirstOrDefault(x => x.Id == classe.ParcoursAcademiqueId && x.AnneeAcademiqueId == anneeAcademiqueId);
        if (parcours is null)
        {
            return [];
        }

        var niveau = await niveauxEtude.GetByIdAsync(parcours.NiveauEtudeId, cancellationToken);
        if (niveau is null || niveau.Numero == 0)
        {
            return [];
        }

        var maquettes = await maquettesPedagogiques.ListAsync(cancellationToken);
        var semestreItems = await semestresPedagogiques.ListAsync(cancellationToken);
        var affectations = (await affectationsMaquettes.ListAsync(cancellationToken))
            .Where(x => x.ParcoursAcademiqueId == parcours.Id)
            .ToList();

        // Une affectation explicite est la source de vérité depuis la séparation
        // Parcours/Maquette. Les anciennes maquettes encore liées directement au
        // parcours restent utilisées comme solution de compatibilité.
        // Un parcours peut posséder plusieurs maquettes : par exemple une
        // maquette N1 (S1-S2) et une maquette N2 (S3-S4). Il ne faut pas
        // retenir uniquement la première, sinon le semestre extrait du
        // document Word peut être absent de la liste proposée.
        var maquetteIds = affectations
            .Where(x => x.EstActive)
            .Select(x => x.MaquettePedagogiqueId)
            .ToHashSet();

        if (maquetteIds.Count == 0)
        {
            maquetteIds = affectations
                .Select(x => x.MaquettePedagogiqueId)
                .ToHashSet();
        }

        // Compatibilité avec les anciennes maquettes encore liées directement
        // au parcours, même lorsqu'une affectation explicite existe déjà.
        maquetteIds.UnionWith(
            maquettes
                .Where(x => x.ParcoursAcademiqueId == parcours.Id)
                .Select(x => x.Id));

        var semestres = semestreItems
            .Where(x => maquetteIds.Contains(x.MaquettePedagogiqueId))
            // Un niveau regroupe toujours deux semestres :
            // N1 = S1-S2, N2 = S3-S4, N3 = S5-S6, etc.
            .Where(x => IsSemestreOfNiveau(x.NumeroSemestre, niveau.Numero))
            .OrderBy(x => x.NumeroSemestre)
            .ThenBy(x => x.OrdreAffichage)
            .ThenBy(x => x.Id)
            .ToList();

        if (semestres.Count == 0)
        {
            return [];
        }

        return semestres
            .Select(x => new LookupDto
            {
                Id = x.Id,
                Libelle = $"{x.NumeroSemestre} - {x.Libelle}"
            })
            .ToList();
    }

    public async Task<NotesEvaluationContextDto?> GetEvaluationContextAsync(
        long anneeAcademiqueId,
        long classePedagogiqueId,
        long semestrePedagogiqueId,
        long elementConstitutifId,
        CancellationToken cancellationToken = default)
    {
        var classe = await classesPedagogiques.GetByIdAsync(classePedagogiqueId, cancellationToken);
        if (classe is null)
        {
            return null;
        }

        var parcours = (await parcoursAcademiques.ListAsync(cancellationToken))
            .FirstOrDefault(x => x.Id == classe.ParcoursAcademiqueId
                && x.AnneeAcademiqueId == anneeAcademiqueId);
        if (parcours is null)
        {
            return null;
        }

        var semestres = await GetSemestresForClasseLookupAsync(
            anneeAcademiqueId,
            classePedagogiqueId,
            cancellationToken);
        if (semestres.All(x => x.Id != semestrePedagogiqueId))
        {
            return null;
        }

        var maquetteElement = (await maquetteElementsConstitutifs.ListAsync(cancellationToken))
            .FirstOrDefault(x => x.SemestrePedagogiqueId == semestrePedagogiqueId
                && x.ElementConstitutifId == elementConstitutifId);

        return maquetteElement is null
            ? null
            : new NotesEvaluationContextDto
            {
                ParcoursAcademiqueId = parcours.Id,
                MaquetteElementConstitutifId = maquetteElement.Id
             };
    }

    public async Task<SaisieNotesEvaluationContextDto?> GetEvaluationContextForEvaluationAsync(
        long evaluationAcademiqueId,
        CancellationToken cancellationToken = default)
    {
        var evaluation = await evaluations.GetByIdAsync(evaluationAcademiqueId, cancellationToken);
        if (evaluation is null)
        {
            return null;
        }

        var maquetteElement = (await maquetteElementsConstitutifs.ListAsync(cancellationToken))
            .FirstOrDefault(x => x.Id == evaluation.MaquetteElementConstitutifId);
        if (maquetteElement is null)
        {
            return null;
        }

        var semestre = (await semestresPedagogiques.ListAsync(cancellationToken))
            .FirstOrDefault(x => x.Id == maquetteElement.SemestrePedagogiqueId);
        if (semestre is null)
        {
            return null;
        }

        var maquette = (await maquettesPedagogiques.ListAsync(cancellationToken))
            .FirstOrDefault(x => x.Id == semestre.MaquettePedagogiqueId);
        if (maquette is null)
        {
            return null;
        }

        var affectations = await affectationsMaquettes.ListAsync(cancellationToken);
        var parcoursIds = affectations
            .Where(x => x.MaquettePedagogiqueId == maquette.Id && x.EstActive)
            .Select(x => x.ParcoursAcademiqueId)
            .ToHashSet();

        if (parcoursIds.Count == 0)
        {
            parcoursIds = affectations
                .Where(x => x.MaquettePedagogiqueId == maquette.Id)
                .Select(x => x.ParcoursAcademiqueId)
                .ToHashSet();
        }

        if (maquette.ParcoursAcademiqueId is long legacyParcoursId)
        {
            parcoursIds.Add(legacyParcoursId);
        }

        var parcoursItems = await parcoursAcademiques.ListAsync(cancellationToken);
        var parcours = parcoursItems
            .Where(x => x.AnneeAcademiqueId == evaluation.AnneeAcademiqueId
                && parcoursIds.Contains(x.Id))
            .OrderByDescending(x => x.EstActive)
            .ThenByDescending(x => x.Id)
            .FirstOrDefault();

        var classes = await classesPedagogiques.ListAsync(cancellationToken);
        var dossiers = await dossiersAcademiques.ListAsync(cancellationToken);
        var notes = (await notesEvaluations.ListAsync(cancellationToken))
            .Where(x => x.EvaluationAcademiqueId == evaluation.Id)
            .ToList();

        // Une maquette peut être affectée à plusieurs parcours. Lorsque des
        // notes existent déjà, la classe portée par leurs dossiers est la
        // source de vérité pour rouvrir exactement la même grille.
        var classeIdDepuisLesNotes = notes
            .Join(
                dossiers,
                note => note.DossierAcademiqueId,
                dossier => dossier.Id,
                (_, dossier) => dossier.ClassePedagogiqueId)
            .Where(x => x is not null)
            .GroupBy(x => x!.Value)
            .OrderByDescending(x => x.Count())
            .ThenBy(x => x.Key)
            .Select(x => (long?)x.Key)
            .FirstOrDefault();

        var classe = classeIdDepuisLesNotes is long classeId
            ? classes.FirstOrDefault(x => x.Id == classeId)
            : parcours is null
                ? null
                : classes
                    .Where(x => x.ParcoursAcademiqueId == parcours.Id)
                    .OrderBy(x => x.Id)
                    .FirstOrDefault();
        if (classe is null)
        {
            return null;
        }

        parcours = parcoursItems.FirstOrDefault(x => x.Id == classe.ParcoursAcademiqueId
            && x.AnneeAcademiqueId == evaluation.AnneeAcademiqueId)
            ?? parcours;
        if (parcours is null)
        {
            return null;
        }

        var element = await elementsConstitutifs.GetByIdAsync(maquetteElement.ElementConstitutifId, cancellationToken);
        if (element is null)
        {
            return null;
        }

        return new SaisieNotesEvaluationContextDto
        {
            EvaluationAcademiqueId = evaluation.Id,
            AnneeAcademiqueId = evaluation.AnneeAcademiqueId,
            ClassePedagogiqueId = classe.Id,
            SemestrePedagogiqueId = semestre.Id,
            UniteEnseignementId = element.UniteEnseignementId,
            ElementConstitutifId = element.Id
        };
    }

    private static bool IsSemestreOfNiveau(byte numeroSemestre, byte numeroNiveau)
    {
        var premierSemestre = ((int)numeroNiveau - 1) * 2 + 1;
        var dernierSemestre = premierSemestre + 1;
        return numeroSemestre >= premierSemestre && numeroSemestre <= dernierSemestre;
    }

    public async Task<List<LookupDto>> GetClassesPedagogiquesLookupAsync(
        long? anneeAcademiqueId = null,
        CancellationToken cancellationToken = default)
    {
        var items = await classesPedagogiques.ListAsync(cancellationToken);
        var parcoursItems = await parcoursAcademiques.ListAsync(cancellationToken);
        if (anneeAcademiqueId is not null)
        {
            var parcoursIds = parcoursItems
                .Where(x => x.AnneeAcademiqueId == anneeAcademiqueId)
                .Select(x => x.Id)
                .ToHashSet();
            items = items.Where(x => parcoursIds.Contains(x.ParcoursAcademiqueId)).ToList();
        }

        return items
            .Select(x =>
            {
                var parcours = parcoursItems.FirstOrDefault(p => p.Id == x.ParcoursAcademiqueId);
                return new LookupDto
                {
                    Id = x.Id,
                    Libelle = parcours?.Libelle ?? x.Libelle
                };
            })
            .OrderBy(x => x.Libelle)
            .ToList();
    }

    public async Task<List<LookupDto>> GetClassesForMatriculesLookupAsync(
        IEnumerable<string> matricules,
        long anneeAcademiqueId,
        CancellationToken cancellationToken = default)
    {
        var matriculesRecherche = matricules
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(NormalizeMatricule)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (matriculesRecherche.Count == 0)
        {
            return [];
        }

        var dossierEtudiantItems = await dossiersEtudiants.ListAsync(cancellationToken);
        var etudiantIds = dossierEtudiantItems
            .Where(x => matriculesRecherche.Contains(NormalizeMatricule(x.Matricule)))
            .Select(x => x.EtudiantId)
            .ToHashSet();

        if (etudiantIds.Count == 0)
        {
            return [];
        }

        var parcoursItems = await parcoursAcademiques.ListAsync(cancellationToken);
        var parcoursAnnee = parcoursItems
            .Where(x => x.AnneeAcademiqueId == anneeAcademiqueId)
            .ToDictionary(x => x.Id);
        var classeItems = await classesPedagogiques.ListAsync(cancellationToken);
        var classeParcours = classeItems
            .GroupBy(x => x.ParcoursAcademiqueId)
            .ToDictionary(x => x.Key, x => x.OrderBy(y => y.Id).First());
        var dossierAcademiqueItems = await dossiersAcademiques.ListAsync(cancellationToken);

        var classes = dossierAcademiqueItems
            .Where(x => etudiantIds.Contains(x.EtudiantId)
                && parcoursAnnee.ContainsKey(x.ParcoursAcademiqueId))
            .Select(x => x.ClassePedagogiqueId is long classeId
                ? classeItems.FirstOrDefault(classe => classe.Id == classeId)
                : classeParcours.GetValueOrDefault(x.ParcoursAcademiqueId))
            .Where(x => x is not null)
            .Select(x => x!)
            .GroupBy(x => x.Id)
            .Select(x => x.First())
            .OrderBy(x => x.Libelle)
            .Select(x => new LookupDto
            {
                Id = x.Id,
                Libelle = parcoursItems.FirstOrDefault(p => p.Id == x.ParcoursAcademiqueId)?.Libelle
                    ?? x.Libelle
            })
            .ToList();

        return classes;
    }

    public async Task<List<LookupDto>> GetUnitesEnseignementLookupAsync(
        long? semestrePedagogiqueId = null,
        CancellationToken cancellationToken = default)
    {
        var items = await unitesEnseignement.ListAsync(cancellationToken);

        if (semestrePedagogiqueId is not null)
        {
            var maquetteElementItems = await maquetteElementsConstitutifs.ListAsync(cancellationToken);
            var elementIds = maquetteElementItems
                .Where(x => x.SemestrePedagogiqueId == semestrePedagogiqueId.Value)
                .Select(x => x.ElementConstitutifId)
                .ToHashSet();
            var uniteIds = (await elementsConstitutifs.ListAsync(cancellationToken))
                .Where(x => elementIds.Contains(x.Id))
                .Select(x => x.UniteEnseignementId)
                .ToHashSet();

            items = items.Where(x => uniteIds.Contains(x.Id)).ToList();
        }

        return items
            .OrderBy(x => x.Code)
            .Select(x => new LookupDto { Id = x.Id, Libelle = FormatCodeLibelle(x.Code, x.Libelle) })
            .ToList();
    }

    public async Task<List<LookupDto>> GetElementsConstitutifsLookupAsync(
        long? uniteEnseignementId = null,
        long? semestrePedagogiqueId = null,
        CancellationToken cancellationToken = default)
    {
        var items = await elementsConstitutifs.ListAsync(cancellationToken);

        if (semestrePedagogiqueId is not null)
        {
            var elementIds = (await maquetteElementsConstitutifs.ListAsync(cancellationToken))
                .Where(x => x.SemestrePedagogiqueId == semestrePedagogiqueId.Value)
                .Select(x => x.ElementConstitutifId)
                .ToHashSet();

            items = items.Where(x => elementIds.Contains(x.Id)).ToList();
        }

        if (uniteEnseignementId is not null)
        {
            items = items.Where(x => x.UniteEnseignementId == uniteEnseignementId).ToList();
        }

        return items
            .OrderBy(x => x.Libelle)
            .Select(x => new LookupDto { Id = x.Id, Libelle = FormatCodeLibelle(x.Code, x.Libelle) })
            .ToList();
    }

    public async Task<List<LookupDto>> GetEvaluationsLookupAsync(
        long? anneeAcademiqueId = null,
        long? uniteEnseignementId = null,
        long? elementConstitutifId = null,
        TypeEvaluation? typeEvaluation = null,
        long? semestrePedagogiqueId = null,
        CancellationToken cancellationToken = default)
    {
        var evaluationItems = await evaluations.ListAsync(cancellationToken);
        var mecItems = await maquetteElementsConstitutifs.ListAsync(cancellationToken);
        var ecItems = await elementsConstitutifs.ListAsync(cancellationToken);
        var semestreItems = await semestresPedagogiques.ListAsync(cancellationToken);

        if (anneeAcademiqueId is not null)
        {
            evaluationItems = evaluationItems.Where(x => x.AnneeAcademiqueId == anneeAcademiqueId).ToList();
        }

        if (semestrePedagogiqueId is not null)
        {
            var mecIds = mecItems
                .Where(x => x.SemestrePedagogiqueId == semestrePedagogiqueId.Value)
                .Select(x => x.Id)
                .ToHashSet();

            evaluationItems = evaluationItems
                .Where(x => mecIds.Contains(x.MaquetteElementConstitutifId))
                .ToList();
        }

        if (uniteEnseignementId is not null)
        {
            var ecIds = ecItems
                .Where(ec => ec.UniteEnseignementId == uniteEnseignementId)
                .Select(ec => ec.Id)
                .ToHashSet();
            var mecIds = mecItems
                .Where(m => ecIds.Contains(m.ElementConstitutifId))
                .Select(m => m.Id)
                .ToHashSet();

            evaluationItems = evaluationItems.Where(x => mecIds.Contains(x.MaquetteElementConstitutifId)).ToList();
        }

        if (elementConstitutifId is not null)
        {
            var mecIds = mecItems
                .Where(m => m.ElementConstitutifId == elementConstitutifId)
                .Select(m => m.Id)
                .ToHashSet();

            evaluationItems = evaluationItems.Where(x => mecIds.Contains(x.MaquetteElementConstitutifId)).ToList();
        }

        if (typeEvaluation is not null)
        {
            evaluationItems = evaluationItems.Where(x => x.Type == typeEvaluation).ToList();
        }

        return evaluationItems
            .OrderBy(x => x.Type)
            .ThenBy(x => x.Numero)
            .ThenBy(x => x.Code)
            .Select(x =>
            {
                var mec = mecItems.FirstOrDefault(m => m.Id == x.MaquetteElementConstitutifId);
                var ec = mec is null ? null : ecItems.FirstOrDefault(ec => ec.Id == mec.ElementConstitutifId);
                var ecLibelle = ec is null ? string.Empty : FormatCodeLibelle(ec.Code, ec.Libelle);

                return new LookupDto
                {
                    Id = x.Id,
                    Libelle = $"{GetTypeCode(x.Type)}{x.Numero} - {x.Libelle} | {ecLibelle}"
                };
            })
            .ToList();
    }

    public async Task<SaisieNotesGrilleDto> GetGrilleSaisieAsync(
        long evaluationAcademiqueId,
        long classePedagogiqueId,
        CancellationToken cancellationToken = default)
    {
        var evaluation = await evaluations.GetByIdAsync(evaluationAcademiqueId, cancellationToken)
            ?? throw new InvalidOperationException("L'évaluation sélectionnée est introuvable.");

        var classe = await classesPedagogiques.GetByIdAsync(classePedagogiqueId, cancellationToken)
            ?? throw new InvalidOperationException("La classe sélectionnée est introuvable.");

        var anneeItems = await anneesAcademiques.ListAsync(cancellationToken);
        var dossierItems = await dossiersAcademiques.ListAsync(cancellationToken);
        var dossierEtudiantItems = await dossiersEtudiants.ListAsync(cancellationToken);
        var parcoursItems = await parcoursAcademiques.ListAsync(cancellationToken);
        var etudiantItems = await etudiants.ListAsync(cancellationToken);
        var noteItems = await notesEvaluations.ListAsync(cancellationToken);
        var mecItems = await maquetteElementsConstitutifs.ListAsync(cancellationToken);
        var semestreItems = await semestresPedagogiques.ListAsync(cancellationToken);
        var ueItems = await unitesEnseignement.ListAsync(cancellationToken);
        var ecItems = await elementsConstitutifs.ListAsync(cancellationToken);
        var resultatSemestreItems = await resultatsSemestres.ListAsync(cancellationToken);

        var annee = anneeItems.FirstOrDefault(x => x.Id == evaluation.AnneeAcademiqueId);
        var mec = mecItems.FirstOrDefault(x => x.Id == evaluation.MaquetteElementConstitutifId)
            ?? throw new InvalidOperationException("Le MEC de l'évaluation est introuvable.");
        var semestre = semestreItems.FirstOrDefault(x => x.Id == mec.SemestrePedagogiqueId)
            ?? throw new InvalidOperationException("Le semestre de l'évaluation est introuvable.");
        var ec = ecItems.FirstOrDefault(x => x.Id == mec.ElementConstitutifId)
            ?? throw new InvalidOperationException("L'EC de l'évaluation est introuvable.");
        var ue = ueItems.FirstOrDefault(x => x.Id == ec.UniteEnseignementId)
            ?? throw new InvalidOperationException("L'UE de l'évaluation est introuvable.");

        var parcoursAnneeIds = parcoursItems
            .Where(x => x.AnneeAcademiqueId == evaluation.AnneeAcademiqueId)
            .Select(x => x.Id)
            .ToHashSet();

        var dossiersClasse = dossierItems
            .Where(x => x.ClassePedagogiqueId == classePedagogiqueId
                && parcoursAnneeIds.Contains(x.ParcoursAcademiqueId)
                && x.Statut != StatutDossierAcademique.Abandonne)
            .ToList();

        string? avertissement = null;

        if (evaluation.Type == TypeEvaluation.SessionRattrapage)
        {
            var resultatsPourSemestre = resultatSemestreItems
                .Where(x => x.SemestrePedagogiqueId == semestre.Id)
                .ToList();

            if (resultatsPourSemestre.Count == 0)
            {
                avertissement = "Aucun résultat semestriel n'est encore calculé pour filtrer les étudiants éligibles au rattrapage. Tous les étudiants de la classe sont affichés provisoirement.";
            }
            else
            {
                var dossiersEligibles = resultatsPourSemestre
                    .Where(x => x.CreditsAcquis < x.CreditsRequis)
                    .Select(x => x.DossierAcademiqueId)
                    .ToHashSet();

                dossiersClasse = dossiersClasse
                    .Where(x => dossiersEligibles.Contains(x.Id))
                    .ToList();
            }
        }

        return new SaisieNotesGrilleDto
        {
            EvaluationAcademiqueId = evaluation.Id,
            ClassePedagogiqueId = classe.Id,
            EvaluationLibelle = $"{evaluation.Code} - {evaluation.Libelle}",
            ClassePedagogiqueLibelle = classe.Libelle,
            AnneeAcademiqueLibelle = annee?.Libelle ?? string.Empty,
            ElementConstitutifLibelle = FormatCodeLibelle(ec.Code, ec.Libelle),
            UniteEnseignementLibelle = FormatCodeLibelle(ue.Code, ue.Libelle),
            TypeEvaluation = evaluation.Type,
            Bareme = evaluation.Bareme,
            PonderationPourcentage = evaluation.PonderationPourcentage,
            Avertissement = avertissement,
            Lignes = dossiersClasse
                .Select(dossier =>
                {
                    var etudiant = etudiantItems.FirstOrDefault(x => x.Id == dossier.EtudiantId);
                    var note = noteItems.FirstOrDefault(x => x.EvaluationAcademiqueId == evaluation.Id && x.DossierAcademiqueId == dossier.Id);
                    var resultatSemestre = resultatSemestreItems.FirstOrDefault(x =>
                        x.DossierAcademiqueId == dossier.Id
                        && x.SemestrePedagogiqueId == semestre.Id);

                    return new SaisieNoteLigneDto
                    {
                        NoteEvaluationId = note?.Id ?? 0,
                        DossierAcademiqueId = dossier.Id,
                        Matricule = dossierEtudiantItems.FirstOrDefault(x =>
                            x.EtudiantId == dossier.EtudiantId
                            && x.ParcoursAcademiqueId == dossier.ParcoursAcademiqueId)?.Matricule ?? string.Empty,
                        NomComplet = etudiant is null ? string.Empty : $"{etudiant.Nom} {etudiant.Prenoms}".Trim(),
                        Valeur = note?.Valeur,
                        StatutPresence = note?.StatutPresence ?? StatutPresenceEvaluation.Present,
                        Observation = note?.Observation,
                        CreditsSemestreAcquis = resultatSemestre?.CreditsAcquis,
                        CreditsSemestreRequis = resultatSemestre?.CreditsRequis,
                        EstEligibleSessionRattrapage = evaluation.Type != TypeEvaluation.SessionRattrapage
                            || resultatSemestre is null
                            || resultatSemestre.CreditsAcquis < resultatSemestre.CreditsRequis
                    };
                })
                .OrderBy(x => x.NomComplet)
                .ToList()
        };
    }

    public async Task SaveNotesAsync(SaisieNotesGrilleDto grille, string? saisiePar = null, CancellationToken cancellationToken = default)
    {
        var evaluation = await evaluations.GetByIdAsync(grille.EvaluationAcademiqueId, cancellationToken)
            ?? throw new InvalidOperationException("L'évaluation sélectionnée est introuvable.");

        var classe = await classesPedagogiques.GetByIdAsync(grille.ClassePedagogiqueId, cancellationToken)
            ?? throw new InvalidOperationException("La classe sélectionnée est introuvable.");

        var dossierItems = await dossiersAcademiques.ListAsync(cancellationToken);
        var parcoursItems = await parcoursAcademiques.ListAsync(cancellationToken);
        var noteItems = await notesEvaluations.ListAsync(cancellationToken);
        var now = DateTime.UtcNow;

        foreach (var ligne in grille.Lignes)
        {
            var dossier = dossierItems.FirstOrDefault(x => x.Id == ligne.DossierAcademiqueId)
                ?? throw new InvalidOperationException("Un dossier academique de la grille est introuvable.");

            var parcours = parcoursItems.FirstOrDefault(x => x.Id == dossier.ParcoursAcademiqueId)
                ?? throw new InvalidOperationException("Le parcours du dossier academique est introuvable.");

            if (dossier.ClassePedagogiqueId != grille.ClassePedagogiqueId || parcours.AnneeAcademiqueId != evaluation.AnneeAcademiqueId)
            {
                throw new InvalidOperationException("Une inscription de la grille n'appartient pas à la classe ou à l'année de l'évaluation.");
            }

            if (ligne.StatutPresence == StatutPresenceEvaluation.Present)
            {
                if (ligne.Valeur is not null && (ligne.Valeur < 0 || ligne.Valeur > evaluation.Bareme))
                {
                    throw new InvalidOperationException($"La note de {ligne.NomComplet} doit être comprise entre 0 et {evaluation.Bareme}.");
                }
            }
            else
            {
                ligne.Valeur = null;
            }

            var note = noteItems.FirstOrDefault(x => x.EvaluationAcademiqueId == evaluation.Id && x.DossierAcademiqueId == ligne.DossierAcademiqueId);

            if (note is null)
            {
                await notesEvaluations.AddAsync(new NoteEvaluation
                {
                    EvaluationAcademiqueId = evaluation.Id,
                    DossierAcademiqueId = ligne.DossierAcademiqueId,
                    Valeur = ligne.Valeur,
                    StatutPresence = ligne.StatutPresence,
                    Observation = NormalizeNullable(ligne.Observation),
                    SaisieLeUtc = now,
                    SaisiePar = NormalizeNullable(saisiePar)
                }, cancellationToken);
            }
            else
            {
                note.Valeur = ligne.Valeur;
                note.StatutPresence = ligne.StatutPresence;
                note.Observation = NormalizeNullable(ligne.Observation);
                note.SaisieLeUtc = now;
                note.SaisiePar = NormalizeNullable(saisiePar);
            }
        }

        await notesEvaluations.SaveChangesAsync(cancellationToken);
    }

    private static string GetTypeCode(TypeEvaluation type)
        => type switch
        {
            TypeEvaluation.ControleContinu => "CCON",
            TypeEvaluation.ControleConnaissance => "CC",
            TypeEvaluation.SessionNormale => "SN",
            TypeEvaluation.SessionRattrapage => "SR",
            _ => "EV"
        };

    private static string FormatCodeLibelle(string? code, string libelle)
        => string.IsNullOrWhiteSpace(code) ? libelle : $"{code} - {libelle}";

    private static string NormalizeMatricule(string? value)
        => new string((value ?? string.Empty)
            .Where(char.IsLetterOrDigit)
            .ToArray())
            .ToUpperInvariant();

    private static string? NormalizeNullable(string? value)
    {
        var normalized = value?.Trim();

        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
