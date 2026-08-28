using Microsoft.EntityFrameworkCore;
using RIIS.Academic.Domain;

namespace RIIS.Academic.Infrastructure.Persistence.Seeders;

public static class PrepaTestNotesSeeder
{
    private const string AnneeAcademiqueLibelle = "2025-2026";
    private const string SeederUser = "Seeder test notes PREPA S1/S2 2025-2026";
    private const string SourceDocument = "Données de test PREPA S1/S2";

    private static readonly string[] ParcoursCodes =
    [
        "PREPA-MANAGEMENT-ECONOMIE",
        "PREPA-INGENIEUR"
    ];

    public static async Task SeedPrepaS1S2TestNotesAsync(
        RiisAcademicDbContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        await NotesSeeder.SeedCcCcon2025_2026Async(context, cancellationToken);

        var anneeAcademique = await context.AnneesAcademiques
            .FirstOrDefaultAsync(x => x.Libelle == AnneeAcademiqueLibelle, cancellationToken)
            ?? throw new InvalidOperationException($"L'année académique '{AnneeAcademiqueLibelle}' est introuvable.");

        var niveau1 = await context.NiveauxEtude
            .FirstOrDefaultAsync(x => x.Numero == 1, cancellationToken)
            ?? throw new InvalidOperationException("Le niveau 1 est introuvable.");

        foreach (var parcoursCode in ParcoursCodes)
        {
            var ouverture = await context.ParcoursAcademiques
                .Include(x => x.CycleFormation)
                .FirstOrDefaultAsync(
                    x => x.Code == parcoursCode && x.CycleFormation.Code == "PREPA",
                    cancellationToken)
                ?? throw new InvalidOperationException($"Le parcours PREPA '{parcoursCode}' est introuvable.");

            var maquette = await context.MaquettesPedagogiques
                .Include(x => x.Semestres)
                    .ThenInclude(x => x.UnitesEnseignement)
                        .ThenInclude(x => x.ElementsConstitutifs)
                .FirstOrDefaultAsync(
                    x => x.ParcoursAcademiqueId == ouverture.Id
                        && x.Version == AnneeAcademiqueLibelle,
                    cancellationToken)
                ?? throw new InvalidOperationException(
                    $"Aucune maquette PREPA '{parcoursCode}' trouvée pour {AnneeAcademiqueLibelle}.");

            var semestre2 = GetSemestreWithEc(maquette, 2)
                ?? throw new InvalidOperationException(
                    $"Le semestre 2 du parcours '{parcoursCode}' doit contenir des EC avant de générer les notes de test.");

            var semestre1 = GetSemestreWithEc(maquette, 1)
                ?? throw new InvalidOperationException(
                    $"Le semestre 1 du parcours '{parcoursCode}' doit contenir ses UE et EC officiels avant de générer les notes de test.");

            var inscriptions = await context.Inscriptions
                .Where(x => x.AnneeAcademiqueId == anneeAcademique.Id
                    && x.ParcoursAcademiqueId == ouverture.Id
                    && x.NiveauEtudeId == niveau1.Id)
                .OrderBy(x => x.Id)
                .ToListAsync(cancellationToken);

            if (inscriptions.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Aucune inscription PREPA niveau 1 trouvée pour le parcours '{parcoursCode}' en {AnneeAcademiqueLibelle}.");
            }

            foreach (var inscription in inscriptions.Where(x => x.MaquettePedagogiqueId is null))
            {
                inscription.MaquettePedagogiqueId = maquette.Id;
            }

            await SeedSemesterNotesAsync(
                context,
                anneeAcademique.Id,
                semestre1,
                inscriptions,
                [
                    new EvaluationDefinition(TypeEvaluation.ControleContinu, 1, "CCON1", "CCON 1", 20m),
                    new EvaluationDefinition(TypeEvaluation.ControleConnaissance, 1, "CC1", "CC 1", 10m),
                    new EvaluationDefinition(TypeEvaluation.SessionNormale, 1, "SN1", "Session normale", 70m)
                ],
                cancellationToken);

            await SeedSemesterNotesAsync(
                context,
                anneeAcademique.Id,
                semestre2,
                inscriptions,
                [
                    new EvaluationDefinition(TypeEvaluation.ControleContinu, 1, "CCON1", "CCON 1", 20m),
                    new EvaluationDefinition(TypeEvaluation.ControleContinu, 2, "CCON2", "CCON 2", 20m),
                    new EvaluationDefinition(TypeEvaluation.ControleConnaissance, 1, "CC1", "CC 1", 10m),
                    new EvaluationDefinition(TypeEvaluation.ControleConnaissance, 2, "CC2", "CC 2", 10m),
                    new EvaluationDefinition(TypeEvaluation.SessionNormale, 1, "SN1", "Session normale", 70m)
                ],
                cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static SemestrePedagogique? GetSemestreWithEc(MaquettePedagogique maquette, byte numero)
        => maquette.Semestres
            .Where(x => x.Numero == numero)
            .OrderBy(x => x.Id)
            .FirstOrDefault(x => x.UnitesEnseignement.SelectMany(ue => ue.ElementsConstitutifs).Any());

    private static async Task SeedSemesterNotesAsync(
        RiisAcademicDbContext context,
        long anneeAcademiqueId,
        SemestrePedagogique semestre,
        IReadOnlyCollection<Inscription> inscriptions,
        IReadOnlyCollection<EvaluationDefinition> evaluationDefinitions,
        CancellationToken cancellationToken)
    {
        var elementsConstitutifs = await context.ElementsConstitutifs
            .Include(x => x.UniteEnseignement)
            .Where(x => x.UniteEnseignement.SemestrePedagogiqueId == semestre.Id)
            .OrderBy(x => x.UniteEnseignement.OrdreAffichage)
            .ThenBy(x => x.OrdreAffichage)
            .ThenBy(x => x.Libelle)
            .ToListAsync(cancellationToken);

        foreach (var elementConstitutif in elementsConstitutifs)
        {
            foreach (var definition in evaluationDefinitions)
            {
                var evaluation = await EnsureEvaluationAsync(
                    context,
                    anneeAcademiqueId,
                    elementConstitutif,
                    semestre.Numero,
                    definition,
                    cancellationToken);

                foreach (var inscription in inscriptions)
                {
                    await AddMissingNoteAsync(
                        context,
                        evaluation.Id,
                        inscription.Id,
                        GenerateNote(inscription.Id, elementConstitutif.Id, semestre.Numero, definition),
                        cancellationToken);
                }
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task<EvaluationAcademique> EnsureEvaluationAsync(
        RiisAcademicDbContext context,
        long anneeAcademiqueId,
        ElementConstitutif elementConstitutif,
        byte semestreNumero,
        EvaluationDefinition definition,
        CancellationToken cancellationToken)
    {
        var evaluation = await context.EvaluationsAcademiques
            .FirstOrDefaultAsync(
                x => x.AnneeAcademiqueId == anneeAcademiqueId
                    && x.ElementConstitutifId == elementConstitutif.Id
                    && x.Type == definition.Type
                    && x.Numero == definition.Numero,
                cancellationToken);

        var ecToken = elementConstitutif.Code ?? $"EC{elementConstitutif.Id}";
        var code = BuildEvaluationCode(definition.CodePrefix, ecToken);
        var libelle = $"{definition.LibellePrefix} - S{semestreNumero} - {elementConstitutif.Libelle}";

        if (evaluation is null)
        {
            evaluation = new EvaluationAcademique
            {
                AnneeAcademiqueId = anneeAcademiqueId,
                ElementConstitutifId = elementConstitutif.Id,
                Type = definition.Type,
                Numero = definition.Numero,
                Code = code,
                Libelle = Truncate(libelle, 200),
                Bareme = 20m,
                PonderationPourcentage = definition.PonderationPourcentage,
                Observation = SourceDocument
            };

            context.EvaluationsAcademiques.Add(evaluation);
            await context.SaveChangesAsync(cancellationToken);
        }
        else
        {
            evaluation.Code = code;
            evaluation.Libelle = Truncate(libelle, 200);
            evaluation.Bareme = 20m;
            evaluation.PonderationPourcentage = definition.PonderationPourcentage;
            evaluation.Observation ??= SourceDocument;
        }

        return evaluation;
    }

    private static async Task AddMissingNoteAsync(
        RiisAcademicDbContext context,
        long evaluationAcademiqueId,
        long inscriptionId,
        decimal valeur,
        CancellationToken cancellationToken)
    {
        var exists = await context.NotesEvaluations.AnyAsync(
            x => x.EvaluationAcademiqueId == evaluationAcademiqueId && x.InscriptionId == inscriptionId,
            cancellationToken);

        if (exists)
        {
            return;
        }

        context.NotesEvaluations.Add(new NoteEvaluation
        {
            EvaluationAcademiqueId = evaluationAcademiqueId,
            InscriptionId = inscriptionId,
            Valeur = valeur,
            StatutPresence = StatutPresenceEvaluation.Present,
            Observation = $"{SourceDocument}; valeur générée automatiquement pour tester les formules.",
            SaisieLeUtc = DateTime.UtcNow,
            SaisiePar = SeederUser
        });
    }

    private static decimal GenerateNote(
        long inscriptionId,
        long elementConstitutifId,
        byte semestreNumero,
        EvaluationDefinition definition)
    {
        var typeOffset = definition.Type switch
        {
            TypeEvaluation.ControleContinu => 7,
            TypeEvaluation.ControleConnaissance => 13,
            TypeEvaluation.SessionNormale => 23,
            _ => 31
        };

        var raw = (inscriptionId * 17
            + elementConstitutifId * 31
            + semestreNumero * 11
            + definition.Numero * 19
            + typeOffset) % 140;

        return Math.Round(5m + raw / 10m, 2, MidpointRounding.AwayFromZero);
    }

    private static string BuildEvaluationCode(string prefix, string ecToken)
    {
        var normalized = NormalizeCodeToken(ecToken);
        var code = $"{prefix}-{normalized}";

        return Truncate(code, 50);
    }

    private static string NormalizeCodeToken(string value)
    {
        var chars = value
            .ToUpperInvariant()
            .Select(x => char.IsLetterOrDigit(x) ? x : '-')
            .ToArray();

        var normalized = string.Join(
            '-',
            new string(chars).Split('-', StringSplitOptions.RemoveEmptyEntries));

        return string.IsNullOrWhiteSpace(normalized) ? "EC" : normalized;
    }

    private static string Truncate(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength];

    private readonly record struct EvaluationDefinition(
        TypeEvaluation Type,
        byte Numero,
        string CodePrefix,
        string LibellePrefix,
        decimal PonderationPourcentage);
}
