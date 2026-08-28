using Microsoft.EntityFrameworkCore;
using RIIS.Academic.Domain;

namespace RIIS.Academic.Infrastructure.Persistence.Seeders;

public static class EvaluationSeeder
{
    private const string AnneeAcademiqueLibelle = "2025-2026";
    private const string Source = "Seeder évaluations S1/S2 2025-2026";

    public static async Task SeedEvaluationsS1S2_2025_2026Async(
        RiisAcademicDbContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        await InscriptionMaquetteSeeder.AttachInscriptionsToMaquettes2025_2026Async(
            context,
            cancellationToken);

        var anneeAcademique = await context.AnneesAcademiques
            .FirstOrDefaultAsync(x => x.Libelle == AnneeAcademiqueLibelle, cancellationToken)
            ?? throw new InvalidOperationException(
                $"L'année académique '{AnneeAcademiqueLibelle}' est introuvable.");

        var semestres = await context.SemestresPedagogiques
            .Include(x => x.MaquettePedagogique)
                .ThenInclude(x => x.Inscriptions)
            .Include(x => x.UnitesEnseignement)
                .ThenInclude(x => x.ElementsConstitutifs)
            .Where(x => x.MaquettePedagogique.Version == AnneeAcademiqueLibelle
                && (x.Numero == 1 || x.Numero == 2)
                && x.MaquettePedagogique.Inscriptions.Any(inscription =>
                    inscription.AnneeAcademiqueId == anneeAcademique.Id
                    && inscription.NiveauEtudeId == x.NiveauEtudeId))
            .ToListAsync(cancellationToken);

        foreach (var semestre in semestres)
        {
            var definitions = GetDefinitions(semestre.Numero);

            foreach (var elementConstitutif in semestre.UnitesEnseignement
                .OrderBy(x => x.OrdreAffichage)
                .SelectMany(x => x.ElementsConstitutifs.OrderBy(ec => ec.OrdreAffichage)))
            {
                foreach (var definition in definitions)
                {
                    await EnsureEvaluationAsync(
                        context,
                        anneeAcademique.Id,
                        semestre.Numero,
                        elementConstitutif,
                        definition,
                        cancellationToken);
                }
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static IReadOnlyList<EvaluationDefinition> GetDefinitions(byte semestreNumero)
        => semestreNumero switch
        {
            1 =>
            [
                new(TypeEvaluation.ControleContinu, 1, "CCON1", "CCON 1", 20m),
                new(TypeEvaluation.ControleContinu, 2, "CCON2", "CCON 2", 20m),
                new(TypeEvaluation.ControleConnaissance, 1, "CC1", "CC 1", 10m),
                new(TypeEvaluation.ControleConnaissance, 2, "CC2", "CC 2", 10m),
                new(TypeEvaluation.SessionNormale, 1, "SN1", "Session normale", 70m)
            ],
            2 =>
            [
                new(TypeEvaluation.SessionNormale, 1, "SN1", "Session normale", 70m)
            ],
            _ => []
        };

    private static async Task EnsureEvaluationAsync(
        RiisAcademicDbContext context,
        long anneeAcademiqueId,
        byte semestreNumero,
        ElementConstitutif elementConstitutif,
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
            context.EvaluationsAcademiques.Add(new EvaluationAcademique
            {
                AnneeAcademiqueId = anneeAcademiqueId,
                ElementConstitutifId = elementConstitutif.Id,
                Type = definition.Type,
                Numero = definition.Numero,
                Code = code,
                Libelle = Truncate(libelle, 200),
                Bareme = 20m,
                PonderationPourcentage = definition.PonderationPourcentage,
                Observation = Source
            });

            return;
        }

        evaluation.Code = code;
        evaluation.Libelle = Truncate(libelle, 200);
        evaluation.Bareme = 20m;
        evaluation.PonderationPourcentage = definition.PonderationPourcentage;
        evaluation.Observation ??= Source;
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
