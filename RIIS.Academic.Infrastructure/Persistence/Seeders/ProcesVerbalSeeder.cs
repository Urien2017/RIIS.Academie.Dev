using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RIIS.Academic.Domain;

namespace RIIS.Academic.Infrastructure.Persistence.Seeders;

public static class ProcesVerbalSeeder
{
    private const string AnneeAcademiqueLibelle = "2025-2026";
    private const byte SemestreNumero = 1;
    private const string CodeSession = "S1";
    private const string Observation =
        "PV semestriel S1 généré à partir des notes CCON, CC et SN 2025-2026.";

    private static readonly string[] TargetParcoursCodes =
    [
        "LICENCE-GESTION",
        "LICENCE-IGL",
        "PREPA-INGENIEUR",
        "PREPA-MANAGEMENT-ECONOMIE"
    ];

    public static async Task SeedProcesVerbauxS1_2025_2026Async(
        RiisAcademicDbContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        await EvaluationNotesSeeder.SeedNotesS1S2_2025_2026Async(
            context,
            cancellationToken);

        var anneeAcademique = await context.AnneesAcademiques
            .FirstOrDefaultAsync(x => x.Libelle == AnneeAcademiqueLibelle, cancellationToken)
            ?? throw new InvalidOperationException(
                $"L'année académique '{AnneeAcademiqueLibelle}' est introuvable.");

        var classes = await context.ClassesPedagogiques
            .Include(x => x.ParcoursAcademique)
            .Include(x => x.NiveauEtude)
            .Include(x => x.MaquettePedagogique)
                .ThenInclude(x => x!.Semestres)
                    .ThenInclude(x => x.UnitesEnseignement)
                        .ThenInclude(x => x.ElementsConstitutifs)
            .Where(x => x.AnneeAcademiqueId == anneeAcademique.Id
                && TargetParcoursCodes.Contains(x.ParcoursAcademique.Code)
                && x.NiveauEtude.Numero == 1
                && x.MaquettePedagogiqueId != null)
            .OrderBy(x => x.ParcoursAcademique.Code)
            .ToListAsync(cancellationToken);

        foreach (var classe in classes)
        {
            var semestre = classe.MaquettePedagogique?.Semestres
                .Where(x => x.Numero == SemestreNumero)
                .OrderBy(x => x.Id)
                .FirstOrDefault();

            if (semestre is null)
            {
                throw new InvalidOperationException(
                    $"Aucun semestre {SemestreNumero} trouvé pour la classe '{classe.Code}'.");
            }

            await SeedProcesVerbalClasseAsync(
                context,
                anneeAcademique,
                classe,
                semestre,
                cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedProcesVerbalClasseAsync(
        RiisAcademicDbContext context,
        AnneeAcademique anneeAcademique,
        ClassePedagogique classe,
        SemestrePedagogique semestre,
        CancellationToken cancellationToken)
    {
        var inscriptions = await context.Inscriptions
            .Include(x => x.Etudiant)
            .Where(x => x.AnneeAcademiqueId == anneeAcademique.Id
                && x.ClassePedagogiqueId == classe.Id
                && x.MaquettePedagogiqueId == classe.MaquettePedagogiqueId
                && x.NiveauEtudeId == classe.NiveauEtudeId)
            .OrderBy(x => x.Etudiant.Nom)
            .ThenBy(x => x.Etudiant.Prenoms)
            .ToListAsync(cancellationToken);

        if (inscriptions.Count == 0)
        {
            return;
        }

        var elementsConstitutifs = semestre.UnitesEnseignement
            .OrderBy(x => x.OrdreAffichage)
            .ThenBy(x => x.Code)
            .SelectMany(ue => ue.ElementsConstitutifs
                .OrderBy(ec => ec.OrdreAffichage)
                .ThenBy(ec => ec.Libelle)
                .Select(ec => new ElementConstitutifContext(ue, ec)))
            .ToList();

        if (elementsConstitutifs.Count == 0)
        {
            throw new InvalidOperationException(
                $"Aucun EC trouvé pour le semestre {semestre.Numero} de la classe '{classe.Code}'.");
        }

        var elementConstitutifIds = elementsConstitutifs
            .Select(x => x.ElementConstitutif.Id)
            .ToHashSet();

        var inscriptionIds = inscriptions
            .Select(x => x.Id)
            .ToHashSet();

        var evaluations = await context.EvaluationsAcademiques
            .Where(x => x.AnneeAcademiqueId == anneeAcademique.Id
                && elementConstitutifIds.Contains(x.ElementConstitutifId))
            .ToListAsync(cancellationToken);

        var evaluationIds = evaluations
            .Select(x => x.Id)
            .ToHashSet();

        var notes = await context.NotesEvaluations
            .Where(x => evaluationIds.Contains(x.EvaluationAcademiqueId)
                && inscriptionIds.Contains(x.InscriptionId))
            .ToListAsync(cancellationToken);

        var pv = await context.ProcesVerbaux
            .Include(x => x.Lignes)
            .FirstOrDefaultAsync(
                x => x.AnneeAcademiqueId == anneeAcademique.Id
                    && x.ClassePedagogiqueId == classe.Id
                    && x.SemestrePedagogiqueId == semestre.Id
                    && x.Type == TypeProcesVerbal.SessionNormale
                    && x.CodeSession == CodeSession,
                cancellationToken);

        if (pv is null)
        {
            pv = new ProcesVerbal
            {
                AnneeAcademiqueId = anneeAcademique.Id,
                ParcoursAcademiqueId = classe.ParcoursAcademiqueId,
                ClassePedagogiqueId = classe.Id,
                SemestrePedagogiqueId = semestre.Id,
                Type = TypeProcesVerbal.SessionNormale,
                CodeSession = CodeSession,
                Titre = BuildTitre(classe),
                EstDefinitif = false,
                Observation = Observation
            };

            context.ProcesVerbaux.Add(pv);
        }
        else
        {
            pv.ParcoursAcademiqueId = classe.ParcoursAcademiqueId;
            pv.Titre = BuildTitre(classe);
            pv.DateEditionUtc = DateTime.UtcNow;
            pv.EstDefinitif = false;
            pv.Observation = Observation;
        }

        var computedLines = inscriptions
            .Select(inscription => BuildLine(
                inscription,
                semestre,
                elementsConstitutifs,
                evaluations,
                notes))
            .OrderByDescending(x => x.MoyenneGenerale ?? -1m)
            .ThenByDescending(x => x.CreditsAcquis ?? -1m)
            .ThenBy(x => x.NomCompletSnapshot)
            .ToList();

        ApplyRanks(computedLines);

        foreach (var computedLine in computedLines)
        {
            var existingLine = pv.Lignes
                .FirstOrDefault(x => x.InscriptionId == computedLine.InscriptionId);

            if (existingLine is null)
            {
                pv.Lignes.Add(computedLine);
                continue;
            }

            existingLine.MatriculeSnapshot = computedLine.MatriculeSnapshot;
            existingLine.NomCompletSnapshot = computedLine.NomCompletSnapshot;
            existingLine.MoyenneGenerale = computedLine.MoyenneGenerale;
            existingLine.CreditsAcquis = computedLine.CreditsAcquis;
            existingLine.Rang = computedLine.Rang;
            existingLine.DecisionJury = computedLine.DecisionJury;
            existingLine.DetailsNotesJson = computedLine.DetailsNotesJson;
        }
    }

    private static ProcesVerbalLigne BuildLine(
        Inscription inscription,
        SemestrePedagogique semestre,
        IReadOnlyCollection<ElementConstitutifContext> elementsConstitutifs,
        IReadOnlyCollection<EvaluationAcademique> evaluations,
        IReadOnlyCollection<NoteEvaluation> notes)
    {
        var details = new List<ProcesVerbalElementDetail>();

        foreach (var item in elementsConstitutifs)
        {
            var ec = item.ElementConstitutif;
            var moyenneCcon = CalculerMoyenneType(
                inscription.Id,
                ec.Id,
                TypeEvaluation.ControleContinu,
                evaluations,
                notes);
            var moyenneCc = CalculerMoyenneType(
                inscription.Id,
                ec.Id,
                TypeEvaluation.ControleConnaissance,
                evaluations,
                notes);
            var moyenneSn = CalculerMoyenneType(
                inscription.Id,
                ec.Id,
                TypeEvaluation.SessionNormale,
                evaluations,
                notes);
            var moyenneFinale = CalculerMoyenneElementConstitutif(
                moyenneCcon,
                moyenneCc,
                moyenneSn);
            var creditsAcquis = moyenneFinale >= 10m ? ec.Credits : 0m;

            details.Add(new ProcesVerbalElementDetail(
                item.UniteEnseignement.Code,
                item.UniteEnseignement.Libelle,
                ec.Code ?? string.Empty,
                ec.Libelle,
                ec.Credits,
                moyenneCcon,
                moyenneCc,
                moyenneSn,
                moyenneFinale,
                creditsAcquis,
                moyenneFinale >= 10m ? "V" : "NV"));
        }

        var notesFinales = details
            .Where(x => x.MoyenneFinale.HasValue)
            .Select(x => x.MoyenneFinale!.Value)
            .ToList();

        decimal? moyenneGenerale = notesFinales.Count == 0
            ? null
            : Truncate2(notesFinales.Average());
        var creditsAcquisTotal = details.Sum(x => x.CreditsAcquis);

        return new ProcesVerbalLigne
        {
            InscriptionId = inscription.Id,
            MatriculeSnapshot = inscription.Etudiant.Matricule ?? string.Empty,
            NomCompletSnapshot = $"{inscription.Etudiant.Nom} {inscription.Etudiant.Prenoms}".Trim(),
            MoyenneGenerale = moyenneGenerale,
            CreditsAcquis = creditsAcquisTotal,
            DecisionJury = ResolveDecision(moyenneGenerale, creditsAcquisTotal, semestre.CreditsAttendus),
            DetailsNotesJson = JsonSerializer.Serialize(
                new ProcesVerbalDetails(
                    semestre.Numero,
                    semestre.CreditsAttendus,
                    details),
                JsonOptions)
        };
    }

    private static decimal? CalculerMoyenneType(
        long inscriptionId,
        long elementConstitutifId,
        TypeEvaluation type,
        IReadOnlyCollection<EvaluationAcademique> evaluations,
        IReadOnlyCollection<NoteEvaluation> notes)
    {
        var evaluationIds = evaluations
            .Where(x => x.ElementConstitutifId == elementConstitutifId
                && x.Type == type)
            .Select(x => x.Id)
            .ToHashSet();

        var valeurs = notes
            .Where(x => evaluationIds.Contains(x.EvaluationAcademiqueId)
                && x.InscriptionId == inscriptionId
                && x.StatutPresence == StatutPresenceEvaluation.Present
                && x.Valeur.HasValue)
            .Select(x => x.Valeur!.Value)
            .ToList();

        return valeurs.Count == 0 ? null : Truncate2(valeurs.Average());
    }

    private static decimal? CalculerMoyenneElementConstitutif(
        decimal? moyenneCcon,
        decimal? moyenneCc,
        decimal? moyenneSn)
    {
        if (moyenneCcon is null || moyenneCc is null || moyenneSn is null)
        {
            return null;
        }

        return Math.Round(
            (moyenneCcon.Value * 0.20m)
            + (moyenneCc.Value * 0.10m)
            + (moyenneSn.Value * 0.70m),
            2,
            MidpointRounding.AwayFromZero);
    }

    private static DecisionAcademique ResolveDecision(
        decimal? moyenneGenerale,
        decimal creditsAcquis,
        decimal creditsAttendus)
    {
        if (moyenneGenerale is null)
        {
            return DecisionAcademique.NonDeliberee;
        }

        return moyenneGenerale >= 10m && creditsAcquis >= creditsAttendus
            ? DecisionAcademique.Valide
            : DecisionAcademique.AutoriseRattrapage;
    }

    private static void ApplyRanks(IReadOnlyList<ProcesVerbalLigne> lignes)
    {
        decimal? previousAverage = null;
        int previousRank = 0;

        for (var index = 0; index < lignes.Count; index++)
        {
            var currentAverage = lignes[index].MoyenneGenerale;
            var rank = previousAverage == currentAverage
                ? previousRank
                : index + 1;

            lignes[index].Rang = currentAverage is null ? null : rank;
            previousAverage = currentAverage;
            previousRank = rank;
        }
    }

    private static string BuildTitre(ClassePedagogique classe)
        => $"PV DES NOTES DU SEMESTRE 1 - {classe.ParcoursAcademique.Libelle}";

    private static decimal Truncate2(decimal value)
        => Math.Truncate(value * 100m) / 100m;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly record struct ElementConstitutifContext(
        UniteEnseignement UniteEnseignement,
        ElementConstitutif ElementConstitutif);

    private record ProcesVerbalDetails(
        byte SemestreNumero,
        decimal CreditsAttendus,
        IReadOnlyCollection<ProcesVerbalElementDetail> ElementsConstitutifs);

    private record ProcesVerbalElementDetail(
        string UniteEnseignementCode,
        string UniteEnseignementLibelle,
        string ElementConstitutifCode,
        string ElementConstitutifLibelle,
        decimal Credits,
        decimal? MoyenneCcon,
        decimal? MoyenneCc,
        decimal? MoyenneSn,
        decimal? MoyenneFinale,
        decimal CreditsAcquis,
        string Decision);
}
