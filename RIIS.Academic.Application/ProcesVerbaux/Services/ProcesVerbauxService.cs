using RIIS.Academic.Application.Abstractions.Persistence;
using RIIS.Academic.Application.Abstractions.Services;
using RIIS.Academic.Application.Common.Dtos;
using RIIS.Academic.Application.ProcesVerbaux.Dtos;
using RIIS.Academic.Domain;
using System.Text.Json;

namespace RIIS.Academic.Application.ProcesVerbaux.Services;

public class ProcesVerbauxService(
    IRepository<ProcesVerbal> procesVerbaux,
    IRepository<ProcesVerbalLigne> procesVerbauxLignes,
    IRepository<AnneeAcademique> anneesAcademiques,
    IRepository<CycleFormation> cyclesFormation,
    IRepository<Filiere> filieres,
    IRepository<ParcoursAcademique> parcoursAcademiques,
    IRepository<ClassePedagogique> classesPedagogiques,
    IRepository<SemestrePedagogique> semestresPedagogiques,
    IRepository<MaquettePedagogique> maquettesPedagogiques,
    IRepository<AffectationMaquette> affectationsMaquettes,
    IRepository<DossierAcademique> dossiersAcademiques,
    IRepository<DossierEtudiant> dossiersEtudiants,
    IRepository<Etudiant> etudiants,
    IRepository<MaquetteElementConstitutif> maquetteElementsConstitutifs,
    IRepository<ElementConstitutif> elementsConstitutifs,
    IRepository<UniteEnseignement> unitesEnseignement,
    IRepository<EvaluationAcademique> evaluationsAcademiques,
    IRepository<NoteEvaluation> notesEvaluations,
    ICalculNotesService calculNotesService) : IProcesVerbauxService
{
    public async Task<List<ProcesVerbalDto>> GetProcesVerbauxAsync(
        long? anneeAcademiqueId = null,
        long? cycleFormationId = null,
        long? classePedagogiqueId = null,
        byte? semestreNumero = null,
        TypeProcesVerbal? type = null,
        long? parcoursAcademiqueId = null,
        CancellationToken cancellationToken = default)
    {
        var pvItems = await procesVerbaux.ListAsync(cancellationToken);
        var ligneItems = await procesVerbauxLignes.ListAsync(cancellationToken);
        var anneeItems = await anneesAcademiques.ListAsync(cancellationToken);
        var cycleItems = await cyclesFormation.ListAsync(cancellationToken);
        var filiereItems = await filieres.ListAsync(cancellationToken);
        var parcoursItems = await parcoursAcademiques.ListAsync(cancellationToken);
        var classeItems = await classesPedagogiques.ListAsync(cancellationToken);
        var semestreItems = await semestresPedagogiques.ListAsync(cancellationToken);

        pvItems = ApplyFilters(
            pvItems,
            parcoursItems,
            classeItems,
            semestreItems,
            anneeAcademiqueId,
            cycleFormationId,
            classePedagogiqueId,
            semestreNumero,
            type,
            parcoursAcademiqueId);

        return pvItems
            .Select(pv => ToDto(
                pv,
                ligneItems.Where(x => x.ProcesVerbalId == pv.Id).ToList(),
                anneeItems,
                cycleItems,
                filiereItems,
                parcoursItems,
                classeItems,
                semestreItems))
            .OrderByDescending(x => x.AnneeAcademiqueLibelle)
            .ThenBy(x => x.CycleFormationLibelle)
            .ThenBy(x => x.ParcoursLibelle)
            .ThenBy(x => x.SemestreNumero)
            .ThenBy(x => x.ClassePedagogiqueLibelle)
            .ToList();
    }

    public async Task<ProcesVerbalDto?> GetProcesVerbalAsync(long id, CancellationToken cancellationToken = default)
    {
        var pvItems = await procesVerbaux.ListAsync(cancellationToken);
        var pv = pvItems.FirstOrDefault(x => x.Id == id);

        if (pv is null)
        {
            return null;
        }

        var ligneItems = await procesVerbauxLignes.ListAsync(cancellationToken);
        var anneeItems = await anneesAcademiques.ListAsync(cancellationToken);
        var cycleItems = await cyclesFormation.ListAsync(cancellationToken);
        var filiereItems = await filieres.ListAsync(cancellationToken);
        var parcoursItems = await parcoursAcademiques.ListAsync(cancellationToken);
        var classeItems = await classesPedagogiques.ListAsync(cancellationToken);
        var semestreItems = await semestresPedagogiques.ListAsync(cancellationToken);

        return ToDto(
            pv,
            ligneItems.Where(x => x.ProcesVerbalId == pv.Id).ToList(),
            anneeItems,
            cycleItems,
            filiereItems,
            parcoursItems,
            classeItems,
            semestreItems);
    }

    public async Task SupprimerProcesVerbalAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        var procesVerbal = await procesVerbaux.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("Le procès-verbal sélectionné est introuvable.");

        // La relation ProcesVerbal -> ProcesVerbalLigne est en cascade dans le
        // modèle. La suppression du parent suffit donc et évite de laisser
        // des lignes orphelines dans la base.
        procesVerbaux.Delete(procesVerbal);
        await procesVerbaux.SaveChangesAsync(cancellationToken);
    }

    public async Task<long> GenererProcesVerbalAsync(
        GenererProcesVerbalRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var parcours = (await parcoursAcademiques.ListAsync(cancellationToken))
            .FirstOrDefault(x => x.Id == request.ParcoursAcademiqueId)
            ?? throw new InvalidOperationException("Le parcours académique sélectionné est introuvable.");

        var classes = (await classesPedagogiques.ListAsync(cancellationToken))
            .Where(x => x.ParcoursAcademiqueId == parcours.Id)
            .OrderBy(x => x.Id)
            .ToList();

        if (classes.Count == 0)
        {
            throw new InvalidOperationException("Aucune classe pédagogique n'est rattachée à ce parcours académique.");
        }

        if (classes.Count > 1)
        {
            throw new InvalidOperationException(
                "Plusieurs classes pédagogiques sont rattachées à ce parcours. La règle actuelle exige une seule classe par parcours.");
        }

        var classe = classes[0];

        var annee = (await anneesAcademiques.ListAsync(cancellationToken))
            .FirstOrDefault(x => x.Id == parcours.AnneeAcademiqueId)
            ?? throw new InvalidOperationException("L'année académique du parcours est introuvable.");

        var maquette = await ResolveMaquetteAsync(parcours.Id, cancellationToken)
            ?? throw new InvalidOperationException("Aucune maquette pédagogique active n'est rattachée à ce parcours.");

        var semestreItems = await semestresPedagogiques.ListAsync(cancellationToken);
        var semestresMaquette = semestreItems
            .Where(x => x.MaquettePedagogiqueId == maquette.Id)
            .OrderBy(x => x.NumeroSemestre)
            .ThenBy(x => x.OrdreAffichage)
            .ToList();

        var semestresCibles = request.SemestrePedagogiqueId is null
            ? semestresMaquette
            : semestresMaquette
                .Where(x => x.Id == request.SemestrePedagogiqueId.Value)
                .ToList();

        if (semestresCibles.Count == 0)
        {
            throw new InvalidOperationException(
                request.SemestrePedagogiqueId is null
                    ? "Aucun semestre n'est enregistré dans la maquette du parcours."
                    : "Le semestre sélectionné n'appartient pas à la maquette du parcours.");
        }

        var maquetteElementItems = await maquetteElementsConstitutifs.ListAsync(cancellationToken);
        var elementItems = await elementsConstitutifs.ListAsync(cancellationToken);
        var ueItems = await unitesEnseignement.ListAsync(cancellationToken);
        var maquetteElementIds = maquetteElementItems
            .Where(x => semestresCibles.Any(s => s.Id == x.SemestrePedagogiqueId))
            .Select(x => x.Id)
            .ToHashSet();

        var maquetteElements = maquetteElementItems
            .Where(x => maquetteElementIds.Contains(x.Id))
            .OrderBy(x => x.SemestrePedagogiqueId)
            .ThenBy(x => x.OrdreAffichage)
            .ToList();

        if (maquetteElements.Count == 0)
        {
            throw new InvalidOperationException("Aucun élément constitutif n'est rattaché au semestre sélectionné.");
        }

        var evaluationItems = (await evaluationsAcademiques.ListAsync(cancellationToken))
            .Where(x => x.AnneeAcademiqueId == annee.Id && maquetteElementIds.Contains(x.MaquetteElementConstitutifId))
            .ToList();
        var evaluationIds = evaluationItems.Select(x => x.Id).ToHashSet();
        var noteItems = (await notesEvaluations.ListAsync(cancellationToken))
            .Where(x => evaluationIds.Contains(x.EvaluationAcademiqueId))
            .ToList();
        var dossierItems = (await dossiersAcademiques.ListAsync(cancellationToken))
            .Where(x => x.ParcoursAcademiqueId == parcours.Id
                // Les dossiers créés avant l'affectation obligatoire à une classe
                // peuvent encore avoir une classe null. La classe est alors déduite
                // du parcours, puisque la règle actuelle impose une seule classe.
                && (x.ClassePedagogiqueId == classe.Id || x.ClassePedagogiqueId is null)
                && x.Statut != StatutDossierAcademique.Abandonne)
            .ToList();
        var etudiantItems = await etudiants.ListAsync(cancellationToken);
        var dossierEtudiantItems = await dossiersEtudiants.ListAsync(cancellationToken);

        if (dossierItems.Count == 0)
        {
            throw new InvalidOperationException("Aucun dossier étudiant n'est rattaché à cette classe et à ce parcours.");
        }

        var lignesCalculees = dossierItems
            .Select(dossier => BuildProcesVerbalLigne(
                dossier,
                etudiantItems,
                dossierEtudiantItems,
                parcours,
                semestresCibles,
                maquetteElements,
                elementItems,
                ueItems,
                evaluationItems,
                noteItems,
                request))
            .ToList();

        var rang = 1;
        foreach (var ligneClassee in lignesCalculees
            .Where(x => x.MoyenneGenerale.HasValue)
            .OrderByDescending(x => x.MoyenneGenerale)
            .ThenBy(x => x.NomCompletSnapshot))
        {
            ligneClassee.Rang = rang++;
        }

        if (request.EstDefinitif && lignesCalculees.Any(x => x.DecisionJury == DecisionAcademique.NonDeliberee))
        {
            throw new InvalidOperationException(
                "Le PV ne peut pas être définitif : certaines notes ou moyennes sont encore incomplètes.");
        }

        var codeSession = NormalizeCodeSession(request.CodeSession)
            ?? BuildDefaultCodeSession(request, semestresCibles);
        var pvItems = await procesVerbaux.ListAsync(cancellationToken);
        var existing = pvItems.FirstOrDefault(x =>
            x.AnneeAcademiqueId == annee.Id
            && x.ParcoursAcademiqueId == parcours.Id
            && x.ClassePedagogiqueId == classe.Id
            && x.SemestrePedagogiqueId == request.SemestrePedagogiqueId
            && x.Type == request.Type
            && string.Equals(x.CodeSession, codeSession, StringComparison.OrdinalIgnoreCase));

        ProcesVerbal procesVerbal;
        var isNewProcesVerbal = false;
        if (existing is not null)
        {
            if (existing.EstDefinitif)
            {
                throw new InvalidOperationException(
                    "Ce PV est déjà définitif. Créez une nouvelle session ou réouvrez le PV avant de le régénérer.");
            }

            procesVerbal = await procesVerbaux.GetByIdAsync(existing.Id, cancellationToken)
                ?? throw new InvalidOperationException("Le PV existant est introuvable.");

            var oldLines = (await procesVerbauxLignes.ListAsync(cancellationToken))
                .Where(x => x.ProcesVerbalId == procesVerbal.Id)
                .ToList();

            foreach (var oldLine in oldLines)
            {
                procesVerbauxLignes.Delete(oldLine);
            }

            if (oldLines.Count > 0)
            {
                await procesVerbauxLignes.SaveChangesAsync(cancellationToken);
            }
        }
        else
        {
            isNewProcesVerbal = true;
            procesVerbal = new ProcesVerbal
            {
                AnneeAcademiqueId = annee.Id,
                ParcoursAcademiqueId = parcours.Id,
                ClassePedagogiqueId = classe.Id,
                SemestrePedagogiqueId = request.SemestrePedagogiqueId,
                Type = request.Type,
                CodeSession = codeSession,
                Titre = BuildTitle(parcours, classe, request, semestresCibles),
                Lignes = []
            };

            await procesVerbaux.AddAsync(procesVerbal, cancellationToken);
        }

        procesVerbal.AnneeAcademiqueId = annee.Id;
        procesVerbal.ParcoursAcademiqueId = parcours.Id;
        procesVerbal.ClassePedagogiqueId = classe.Id;
        procesVerbal.SemestrePedagogiqueId = request.SemestrePedagogiqueId;
        procesVerbal.Type = request.Type;
        procesVerbal.CodeSession = codeSession;
        procesVerbal.Titre = BuildTitle(parcours, classe, request, semestresCibles);
        procesVerbal.EstDefinitif = request.EstDefinitif;
        procesVerbal.DateEditionUtc = DateTime.UtcNow;
        procesVerbal.Observation = NormalizeNullable(request.Observation)
            ?? "Généré à partir des notes saisies ou importées.";

        if (isNewProcesVerbal)
        {
            await procesVerbaux.SaveChangesAsync(cancellationToken);
        }

        foreach (var ligne in lignesCalculees)
        {
            ligne.ProcesVerbalId = procesVerbal.Id;
            await procesVerbauxLignes.AddAsync(ligne, cancellationToken);
        }

        // Les lignes sont enregistrées explicitement par leur repository afin
        // de garantir leur persistance après la création ou la régénération du PV.
        await procesVerbauxLignes.SaveChangesAsync(cancellationToken);
        return procesVerbal.Id;
    }

    public async Task<List<LookupDto>> GetSemestresForParcoursLookupAsync(
        long parcoursAcademiqueId,
        CancellationToken cancellationToken = default)
    {
        var maquette = await ResolveMaquetteAsync(parcoursAcademiqueId, cancellationToken);
        if (maquette is null)
        {
            return [];
        }

        return (await semestresPedagogiques.ListAsync(cancellationToken))
            .Where(x => x.MaquettePedagogiqueId == maquette.Id)
            .OrderBy(x => x.NumeroSemestre)
            .ThenBy(x => x.OrdreAffichage)
            .Select(x => new LookupDto
            {
                Id = x.Id,
                Libelle = $"{x.NumeroSemestre} - {x.Libelle}"
            })
            .ToList();
    }

    private async Task<MaquettePedagogique?> ResolveMaquetteAsync(
        long parcoursAcademiqueId,
        CancellationToken cancellationToken)
    {
        var maquettes = await maquettesPedagogiques.ListAsync(cancellationToken);
        var affectation = (await affectationsMaquettes.ListAsync(cancellationToken))
            .Where(x => x.ParcoursAcademiqueId == parcoursAcademiqueId)
            .OrderByDescending(x => x.EstActive)
            .ThenByDescending(x => x.EstPrincipale)
            .ThenByDescending(x => x.Id)
            .FirstOrDefault();

        if (affectation is not null)
        {
            return maquettes.FirstOrDefault(x => x.Id == affectation.MaquettePedagogiqueId);
        }

        return maquettes
            .Where(x => x.ParcoursAcademiqueId == parcoursAcademiqueId)
            .OrderByDescending(x => x.Statut == StatutMaquettePedagogique.Active)
            .ThenByDescending(x => x.Version)
            .ThenByDescending(x => x.Id)
            .FirstOrDefault();
    }

    private ProcesVerbalLigne BuildProcesVerbalLigne(
        DossierAcademique dossier,
        IReadOnlyCollection<Etudiant> etudiants,
        IReadOnlyCollection<DossierEtudiant> dossiersEtudiants,
        ParcoursAcademique parcours,
        IReadOnlyCollection<SemestrePedagogique> semestres,
        IReadOnlyCollection<MaquetteElementConstitutif> maquetteElements,
        IReadOnlyCollection<ElementConstitutif> elements,
        IReadOnlyCollection<UniteEnseignement> unites,
        IReadOnlyCollection<EvaluationAcademique> evaluations,
        IReadOnlyCollection<NoteEvaluation> notes,
        GenererProcesVerbalRequestDto request)
    {
        var calculatedElements = maquetteElements
            .Select(maquetteElement =>
            {
                var element = elements.FirstOrDefault(x => x.Id == maquetteElement.ElementConstitutifId);
                var unite = element is null
                    ? null
                    : unites.FirstOrDefault(x => x.Id == element.UniteEnseignementId);
                var evaluationsElement = evaluations
                    .Where(x => x.MaquetteElementConstitutifId == maquetteElement.Id)
                    .ToList();

                var moyenneCcon = CalculateAverage(
                    dossier.Id,
                    evaluationsElement,
                    notes,
                    TypeEvaluation.ControleContinu);
                var moyenneCc = CalculateAverage(
                    dossier.Id,
                    evaluationsElement,
                    notes,
                    TypeEvaluation.ControleConnaissance);
                var moyenneSn = CalculateAverage(
                    dossier.Id,
                    evaluationsElement,
                    notes,
                    TypeEvaluation.SessionNormale);
                var moyenneSr = CalculateAverage(
                    dossier.Id,
                    evaluationsElement,
                    notes,
                    TypeEvaluation.SessionRattrapage);
                var hasCconNotes = HasPresentNote(
                    dossier.Id,
                    evaluationsElement,
                    notes,
                    TypeEvaluation.ControleContinu);
                var hasCcNotes = HasPresentNote(
                    dossier.Id,
                    evaluationsElement,
                    notes,
                    TypeEvaluation.ControleConnaissance);
                var hasSnNotes = HasPresentNote(
                    dossier.Id,
                    evaluationsElement,
                    notes,
                    TypeEvaluation.SessionNormale);
                var hasSrNotes = HasPresentNote(
                    dossier.Id,
                    evaluationsElement,
                    notes,
                    TypeEvaluation.SessionRattrapage);
                var moyenneSessionRetenue = moyenneSr ?? moyenneSn;
                var moyenneFinale = calculNotesService.CalculerMoyenneElementConstitutif(
                    moyenneCcon,
                    moyenneCc,
                    moyenneSessionRetenue);

                return new CalculatedElement(
                    maquetteElement.SemestrePedagogiqueId,
                    maquetteElement.Credits,
                    evaluationsElement.Count == 0
                        || evaluationsElement.Any(evaluation => !HasPresentNote(dossier.Id, evaluation.Id, notes)),
                    new ProcesVerbalElementSnapshot
                    {
                        UniteEnseignementCode = unite?.Code ?? string.Empty,
                        UniteEnseignementLibelle = unite?.Libelle ?? string.Empty,
                        ElementConstitutifCode = element?.Code ?? string.Empty,
                        ElementConstitutifLibelle = element?.Libelle ?? string.Empty,
                        Credits = maquetteElement.Credits,
                        MoyenneCcon = moyenneCcon,
                        MoyenneCc = moyenneCc,
                        MoyenneSn = moyenneSn,
                        MoyenneSr = moyenneSr,
                        HasCconNotes = hasCconNotes,
                        HasCcNotes = hasCcNotes,
                        HasSnNotes = hasSnNotes,
                        HasSrNotes = hasSrNotes,
                        MoyenneFinale = moyenneFinale,
                        CreditsAcquis = calculNotesService.CalculerCreditsAcquis(maquetteElement, moyenneFinale),
                        Decision = moyenneFinale is null
                            ? "Non délibéré"
                            : moyenneFinale >= 10m ? "Validé" : "Non validé"
                    },
                    moyenneFinale);
            })
            .ToList();

        var moyenneParSemestre = semestres
            .Select(semestre => calculatedElements
                .Where(x => x.SemestrePedagogiqueId == semestre.Id && x.MoyenneFinale.HasValue)
                .Select(x => x.MoyenneFinale!.Value)
                .ToList())
            .Where(x => x.Count > 0)
            .Select(x => Truncate2(x.Average()))
            .ToList();
        var moyennesFinales = calculatedElements
            .Where(x => x.MoyenneFinale.HasValue)
            .Select(x => x.MoyenneFinale!.Value)
            .ToList();
        var moyenneGenerale = request.SemestrePedagogiqueId is null
            ? moyenneParSemestre.DefaultIfEmpty().Average()
            : moyennesFinales.DefaultIfEmpty().Average();
        var hasMoyenne = request.SemestrePedagogiqueId is null
            ? moyenneParSemestre.Count > 0
            : moyennesFinales.Count > 0;
        var creditsRequis = calculatedElements.Sum(x => x.Credits);
        var creditsAcquis = calculatedElements.Sum(x => x.Snapshot.CreditsAcquis);
        var notesIncompletes = calculatedElements.Any(x => x.NotesIncompletes || !x.MoyenneFinale.HasValue);
        decimal? moyenne = hasMoyenne ? Truncate2(moyenneGenerale) : null;
        var decision = DetermineDecision(moyenne, creditsAcquis, creditsRequis, notesIncompletes, request.Type);
        var etudiant = etudiants.FirstOrDefault(x => x.Id == dossier.EtudiantId);
        var dossierEtudiant = dossiersEtudiants.FirstOrDefault(x =>
            x.EtudiantId == dossier.EtudiantId
            && x.ParcoursAcademiqueId == parcours.Id);

        return new ProcesVerbalLigne
        {
            DossierAcademiqueId = dossier.Id,
            MatriculeSnapshot = dossierEtudiant?.Matricule ?? string.Empty,
            NomCompletSnapshot = etudiant is null ? string.Empty : $"{etudiant.Nom} {etudiant.Prenoms}".Trim(),
            MoyenneGenerale = moyenne,
            CreditsAcquis = creditsAcquis,
            DecisionJury = decision,
            DetailsNotesJson = JsonSerializer.Serialize(
                new { elementsConstitutifs = calculatedElements.Select(x => x.Snapshot).ToList() },
                JsonOptions)
        };
    }

    private static decimal? CalculateAverage(
        long dossierAcademiqueId,
        IReadOnlyCollection<EvaluationAcademique> evaluations,
        IReadOnlyCollection<NoteEvaluation> notes,
        TypeEvaluation type)
    {
        var evaluationIds = evaluations
            .Where(x => x.Type == type)
            .Select(x => x.Id)
            .ToHashSet();
        var values = notes
            .Where(x => x.DossierAcademiqueId == dossierAcademiqueId
                && evaluationIds.Contains(x.EvaluationAcademiqueId)
                && x.StatutPresence == StatutPresenceEvaluation.Present
                && x.Valeur.HasValue)
            .Select(x => x.Valeur!.Value)
            .ToList();

        return values.Count == 0 ? null : Truncate2(values.Average());
    }

    private static bool HasPresentNote(
        long dossierAcademiqueId,
        long evaluationId,
        IReadOnlyCollection<NoteEvaluation> notes)
        => notes.Any(x => x.DossierAcademiqueId == dossierAcademiqueId
            && x.EvaluationAcademiqueId == evaluationId
            && x.StatutPresence == StatutPresenceEvaluation.Present
            && x.Valeur.HasValue);

    private static bool HasPresentNote(
        long dossierAcademiqueId,
        IReadOnlyCollection<EvaluationAcademique> evaluations,
        IReadOnlyCollection<NoteEvaluation> notes,
        TypeEvaluation type)
    {
        var evaluationIds = evaluations
            .Where(x => x.Type == type)
            .Select(x => x.Id)
            .ToHashSet();

        return notes.Any(x => x.DossierAcademiqueId == dossierAcademiqueId
            && evaluationIds.Contains(x.EvaluationAcademiqueId)
            && x.StatutPresence == StatutPresenceEvaluation.Present
            && x.Valeur.HasValue);
    }

    private static DecisionAcademique DetermineDecision(
        decimal? moyenne,
        decimal creditsAcquis,
        decimal creditsRequis,
        bool notesIncompletes,
        TypeProcesVerbal type)
    {
        if (moyenne is null || notesIncompletes)
        {
            return DecisionAcademique.NonDeliberee;
        }

        if (moyenne >= 10m && creditsAcquis >= creditsRequis)
        {
            return DecisionAcademique.Valide;
        }

        return type is TypeProcesVerbal.SessionRattrapage or TypeProcesVerbal.Definitif
            ? DecisionAcademique.Ajoune
            : DecisionAcademique.AutoriseRattrapage;
    }

    private static string BuildTitle(
        ParcoursAcademique parcours,
        ClassePedagogique classe,
        GenererProcesVerbalRequestDto request,
        IReadOnlyCollection<SemestrePedagogique> semestres)
    {
        var periode = request.SemestrePedagogiqueId is null
            ? "Année"
            : $"Semestre {semestres.FirstOrDefault()?.NumeroSemestre}";
        return $"PV {periode} - {parcours.Libelle} - {classe.Libelle}";
    }

    private static string BuildDefaultCodeSession(
        GenererProcesVerbalRequestDto request,
        IReadOnlyCollection<SemestrePedagogique> semestres)
    {
        var periode = request.SemestrePedagogiqueId is null
            ? "ANNUEL"
            : $"S{semestres.FirstOrDefault()?.NumeroSemestre}";
        return $"{periode}-{GetTypeCode(request.Type)}";
    }

    private static string GetTypeCode(TypeProcesVerbal type)
        => type switch
        {
            TypeProcesVerbal.ControleContinu => "CCON",
            TypeProcesVerbal.SessionNormale => "SN",
            TypeProcesVerbal.SessionRattrapage => "SR",
            TypeProcesVerbal.Definitif => "DEF",
            _ => "PV"
        };

    private static string? NormalizeCodeSession(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();

    private static string? NormalizeNullable(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static decimal Truncate2(decimal value)
        => Math.Truncate(value * 100m) / 100m;

    private sealed record CalculatedElement(
        long SemestrePedagogiqueId,
        decimal Credits,
        bool NotesIncompletes,
        ProcesVerbalElementSnapshot Snapshot,
        decimal? MoyenneFinale);

    private sealed class ProcesVerbalElementSnapshot
    {
        public string UniteEnseignementCode { get; set; } = string.Empty;
        public string UniteEnseignementLibelle { get; set; } = string.Empty;
        public string ElementConstitutifCode { get; set; } = string.Empty;
        public string ElementConstitutifLibelle { get; set; } = string.Empty;
        public decimal Credits { get; set; }
        public decimal? MoyenneCcon { get; set; }
        public decimal? MoyenneCc { get; set; }
        public decimal? MoyenneSn { get; set; }
        public decimal? MoyenneSr { get; set; }
        public bool HasCconNotes { get; set; }
        public bool HasCcNotes { get; set; }
        public bool HasSnNotes { get; set; }
        public bool HasSrNotes { get; set; }
        public decimal? MoyenneFinale { get; set; }
        public decimal CreditsAcquis { get; set; }
        public string Decision { get; set; } = string.Empty;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<List<LookupDto>> GetAnneesAcademiquesLookupAsync(CancellationToken cancellationToken = default)
    {
        var items = await anneesAcademiques.ListAsync(cancellationToken);

        return items
            .OrderByDescending(x => x.AnneeDebut)
            .Select(x => new LookupDto { Id = x.Id, Libelle = x.Libelle })
            .ToList();
    }

    public async Task<List<LookupDto>> GetCyclesFormationLookupAsync(CancellationToken cancellationToken = default)
    {
        var items = await cyclesFormation.ListAsync(cancellationToken);

        return items
            .Where(x => x.EstActif)
            .OrderBy(x => x.OrdreAffichage)
            .ThenBy(x => x.Libelle)
            .Select(x => new LookupDto { Id = x.Id, Libelle = FormatCodeLibelle(x.Code, x.Libelle) })
            .ToList();
    }

    public async Task<List<LookupDto>> GetParcoursAcademiquesLookupAsync(CancellationToken cancellationToken = default)
    {
        var items = await parcoursAcademiques.ListAsync(cancellationToken);

        return items
            .Where(x => x.EstActive)
            .OrderByDescending(x => x.AnneeAcademiqueId)
            .ThenBy(x => x.Libelle)
            .Select(x => new LookupDto
            {
                Id = x.Id,
                Libelle = FormatCodeLibelle(x.Code, x.Libelle)
            })
            .ToList();
    }

    public async Task<List<LookupDto>> GetClassesPedagogiquesLookupAsync(
        long? anneeAcademiqueId = null,
        long? cycleFormationId = null,
        long? parcoursAcademiqueId = null,
        CancellationToken cancellationToken = default)
    {
        var classeItems = await classesPedagogiques.ListAsync(cancellationToken);
        var parcoursItems = await parcoursAcademiques.ListAsync(cancellationToken);
        var filteredClasses = classeItems.AsEnumerable();

        if (anneeAcademiqueId is not null)
        {
            var parcoursIds = parcoursItems
                .Where(x => x.AnneeAcademiqueId == anneeAcademiqueId)
                .Select(x => x.Id)
                .ToHashSet();
            filteredClasses = filteredClasses.Where(x => parcoursIds.Contains(x.ParcoursAcademiqueId));
        }

        if (parcoursAcademiqueId is not null)
        {
            filteredClasses = filteredClasses
                .Where(x => x.ParcoursAcademiqueId == parcoursAcademiqueId);
        }

        if (cycleFormationId is not null)
        {
            var parcoursIds = parcoursItems
                .Where(x => x.CycleFormationId == cycleFormationId)
                .Select(x => x.Id)
                .ToHashSet();

            filteredClasses = filteredClasses
                .Where(x => parcoursIds.Contains(x.ParcoursAcademiqueId));
        }

        return filteredClasses
            .OrderBy(x => x.Libelle)
            .Select(x => new LookupDto { Id = x.Id, Libelle = x.Libelle })
            .ToList();
    }

    public async Task<List<LookupDto>> GetSemestresLookupAsync(
        long? anneeAcademiqueId = null,
        long? cycleFormationId = null,
        long? classePedagogiqueId = null,
        CancellationToken cancellationToken = default)
    {
        var classeItems = await classesPedagogiques.ListAsync(cancellationToken);
        var parcoursItems = await parcoursAcademiques.ListAsync(cancellationToken);
        var semestreItems = await semestresPedagogiques.ListAsync(cancellationToken);
        var maquetteItems = await maquettesPedagogiques.ListAsync(cancellationToken);
        var affectationItems = await affectationsMaquettes.ListAsync(cancellationToken);

        var parcoursIds = parcoursItems.AsEnumerable();
        if (anneeAcademiqueId is not null)
        {
            parcoursIds = parcoursIds.Where(x => x.AnneeAcademiqueId == anneeAcademiqueId);
        }

        if (cycleFormationId is not null)
        {
            parcoursIds = parcoursIds.Where(x => x.CycleFormationId == cycleFormationId);
        }

        if (classePedagogiqueId is not null)
        {
            var parcoursId = classeItems.FirstOrDefault(x => x.Id == classePedagogiqueId)?.ParcoursAcademiqueId;
            parcoursIds = parcoursId is null
                ? Enumerable.Empty<ParcoursAcademique>()
                : parcoursIds.Where(x => x.Id == parcoursId);
        }

        var parcoursIdSet = parcoursIds.Select(x => x.Id).ToHashSet();
        var maquetteIds = affectationItems
            .Where(x => parcoursIdSet.Contains(x.ParcoursAcademiqueId))
            .GroupBy(x => x.ParcoursAcademiqueId)
            .SelectMany(x => x
                .OrderByDescending(a => a.EstActive)
                .ThenByDescending(a => a.EstPrincipale)
                .ThenByDescending(a => a.Id)
                .Take(1)
                .Select(a => a.MaquettePedagogiqueId))
            .ToHashSet();

        maquetteIds.UnionWith(maquetteItems
            .Where(x => parcoursIdSet.Contains(x.ParcoursAcademiqueId ?? 0))
            .Select(x => x.Id));

        return semestreItems
            .Where(x => maquetteIds.Contains(x.MaquettePedagogiqueId))
            .GroupBy(x => x.NumeroSemestre)
            .OrderBy(x => x.Key)
            .Select(x => new LookupDto { Id = x.Key, Libelle = $"Semestre {x.Key}" })
            .ToList();
    }

    private static List<ProcesVerbal> ApplyFilters(
        List<ProcesVerbal> pvItems,
        IReadOnlyCollection<ParcoursAcademique> parcoursItems,
        IReadOnlyCollection<ClassePedagogique> classeItems,
        IReadOnlyCollection<SemestrePedagogique> semestreItems,
        long? anneeAcademiqueId,
        long? cycleFormationId,
        long? classePedagogiqueId,
        byte? semestreNumero,
        TypeProcesVerbal? type,
        long? parcoursAcademiqueId)
    {
        if (anneeAcademiqueId is not null)
        {
            pvItems = pvItems
                .Where(x => x.AnneeAcademiqueId == anneeAcademiqueId)
                .ToList();
        }

        if (cycleFormationId is not null)
        {
            var parcoursIds = parcoursItems
                .Where(x => x.CycleFormationId == cycleFormationId)
                .Select(x => x.Id)
                .ToHashSet();

            var classeIds = classeItems
                .Where(x => parcoursIds.Contains(x.ParcoursAcademiqueId))
                .Select(x => x.Id)
                .ToHashSet();

            pvItems = pvItems
                .Where(x => classeIds.Contains(x.ClassePedagogiqueId))
                .ToList();
        }

        if (parcoursAcademiqueId is not null)
        {
            var classeIds = classeItems
                .Where(x => x.ParcoursAcademiqueId == parcoursAcademiqueId)
                .Select(x => x.Id)
                .ToHashSet();

            pvItems = pvItems
                .Where(x => classeIds.Contains(x.ClassePedagogiqueId))
                .ToList();
        }

        if (classePedagogiqueId is not null)
        {
            pvItems = pvItems
                .Where(x => x.ClassePedagogiqueId == classePedagogiqueId)
                .ToList();
        }

        if (semestreNumero is not null)
        {
            var semestreIds = semestreItems
                .Where(x => x.NumeroSemestre == semestreNumero)
                .Select(x => x.Id)
                .ToHashSet();

            pvItems = pvItems
                .Where(x => x.SemestrePedagogiqueId is not null
                    && semestreIds.Contains(x.SemestrePedagogiqueId.Value))
                .ToList();
        }

        if (type is not null)
        {
            pvItems = pvItems
                .Where(x => x.Type == type)
                .ToList();
        }

        return pvItems;
    }

    private static ProcesVerbalDto ToDto(
        ProcesVerbal pv,
        IReadOnlyCollection<ProcesVerbalLigne> lignes,
        IReadOnlyCollection<AnneeAcademique> annees,
        IReadOnlyCollection<CycleFormation> cycles,
        IReadOnlyCollection<Filiere> filieres,
        IReadOnlyCollection<ParcoursAcademique> parcours,
        IReadOnlyCollection<ClassePedagogique> classes,
        IReadOnlyCollection<SemestrePedagogique> semestres)
    {
        var annee = annees.FirstOrDefault(x => x.Id == pv.AnneeAcademiqueId);
        var classe = classes.FirstOrDefault(x => x.Id == pv.ClassePedagogiqueId);
        var parcoursItem = parcours.FirstOrDefault(x => x.Id == pv.ParcoursAcademiqueId)
            ?? (classe is null ? null : parcours.FirstOrDefault(x => x.Id == classe.ParcoursAcademiqueId));
        var cycle = parcoursItem is null
            ? null
            : cycles.FirstOrDefault(x => x.Id == parcoursItem.CycleFormationId);
        var filiere = parcoursItem is null
            ? null
            : filieres.FirstOrDefault(x => x.Id == parcoursItem.FiliereId);
        var semestre = pv.SemestrePedagogiqueId is null
            ? null
            : semestres.FirstOrDefault(x => x.Id == pv.SemestrePedagogiqueId);

        var moyennes = lignes
            .Where(x => x.MoyenneGenerale.HasValue)
            .Select(x => x.MoyenneGenerale!.Value)
            .ToList();

        return new ProcesVerbalDto
        {
            Id = pv.Id,
            AnneeAcademiqueId = pv.AnneeAcademiqueId,
            ClassePedagogiqueId = pv.ClassePedagogiqueId,
            SemestrePedagogiqueId = pv.SemestrePedagogiqueId,
            CycleFormationId = cycle?.Id,
            AnneeAcademiqueLibelle = annee?.Libelle ?? string.Empty,
            CycleFormationLibelle = cycle is null ? string.Empty : FormatCodeLibelle(cycle.Code, cycle.Libelle),
            FiliereLibelle = filiere?.Libelle ?? string.Empty,
            ParcoursLibelle = parcoursItem is null ? string.Empty : FormatCodeLibelle(parcoursItem.Code, parcoursItem.Libelle),
            ClassePedagogiqueLibelle = classe?.Libelle ?? string.Empty,
            SemestrePedagogiqueLibelle = semestre is null ? "Annuel" : $"Semestre {semestre.NumeroSemestre}",
            SemestreNumero = semestre?.NumeroSemestre,
            Type = pv.Type,
            CodeSession = pv.CodeSession,
            Titre = pv.Titre,
            DateEditionUtc = pv.DateEditionUtc,
            EstDefinitif = pv.EstDefinitif,
            Observation = pv.Observation,
            NombreLignes = lignes.Count,
            MoyenneMin = moyennes.Count == 0 ? null : moyennes.Min(),
            MoyenneMax = moyennes.Count == 0 ? null : moyennes.Max(),
            NombreValides = lignes.Count(x => x.DecisionJury == DecisionAcademique.Valide),
            NombreRattrapage = lignes.Count(x => x.DecisionJury == DecisionAcademique.AutoriseRattrapage),
            NombreNonDeliberes = lignes.Count(x => x.DecisionJury == DecisionAcademique.NonDeliberee),
            Lignes = lignes
                .OrderBy(x => x.Rang ?? int.MaxValue)
                .ThenBy(x => x.NomCompletSnapshot)
                .Select(ToLigneDto)
                .ToList()
        };
    }

    private static ProcesVerbalLigneDto ToLigneDto(ProcesVerbalLigne ligne)
        => new()
        {
            Id = ligne.Id,
            ProcesVerbalId = ligne.ProcesVerbalId,
            DossierAcademiqueId = ligne.DossierAcademiqueId,
            Matricule = ligne.MatriculeSnapshot,
            EtudiantNomComplet = ligne.NomCompletSnapshot,
            MoyenneGenerale = ligne.MoyenneGenerale,
            CreditsAcquis = ligne.CreditsAcquis,
            Rang = ligne.Rang,
            DecisionJury = ligne.DecisionJury,
            ElementsConstitutifs = ParseDetailsNotes(ligne.DetailsNotesJson)
        };

    private static string FormatCodeLibelle(string code, string libelle)
        => string.IsNullOrWhiteSpace(code) ? libelle : $"{code} - {libelle}";

    private static List<ProcesVerbalElementConstitutifLigneDto> ParseDetailsNotes(string? detailsNotesJson)
    {
        if (string.IsNullOrWhiteSpace(detailsNotesJson))
        {
            return [];
        }

        try
        {
            using var document = JsonDocument.Parse(detailsNotesJson);

            if (!document.RootElement.TryGetProperty("elementsConstitutifs", out var elements)
                || elements.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            return elements
                .EnumerateArray()
                .Select(x => new ProcesVerbalElementConstitutifLigneDto
                {
                    UniteEnseignementCode = GetString(x, "uniteEnseignementCode"),
                    UniteEnseignementLibelle = GetString(x, "uniteEnseignementLibelle"),
                    ElementConstitutifCode = GetString(x, "elementConstitutifCode"),
                    ElementConstitutifLibelle = GetString(x, "elementConstitutifLibelle"),
                     Credits = GetDecimal(x, "credits") ?? 0m,
                     MoyenneCcon = GetDecimal(x, "moyenneCcon"),
                     MoyenneCc = GetDecimal(x, "moyenneCc"),
                     MoyenneSn = GetDecimal(x, "moyenneSn"),
                     MoyenneSr = GetDecimal(x, "moyenneSr"),
                     HasCconNotes = GetBooleanOrFallback(x, "hasCconNotes", GetDecimal(x, "moyenneCcon").HasValue),
                     HasCcNotes = GetBooleanOrFallback(x, "hasCcNotes", GetDecimal(x, "moyenneCc").HasValue),
                     HasSnNotes = GetBooleanOrFallback(x, "hasSnNotes", GetDecimal(x, "moyenneSn").HasValue),
                     HasSrNotes = GetBooleanOrFallback(x, "hasSrNotes", GetDecimal(x, "moyenneSr").HasValue),
                     MoyenneFinale = GetDecimal(x, "moyenneFinale"),
                    CreditsAcquis = GetDecimal(x, "creditsAcquis") ?? 0m,
                    Decision = GetString(x, "decision")
                })
                .ToList();
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static string GetString(JsonElement element, string propertyName)
        => element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString() ?? string.Empty
            : string.Empty;

    private static decimal? GetDecimal(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property)
            || property.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return property.TryGetDecimal(out var value) ? value : null;
    }

    private static bool GetBooleanOrFallback(JsonElement element, string propertyName, bool fallback)
        => element.TryGetProperty(propertyName, out var property)
            && (property.ValueKind == JsonValueKind.True || property.ValueKind == JsonValueKind.False)
            ? property.GetBoolean()
            : fallback;
}
