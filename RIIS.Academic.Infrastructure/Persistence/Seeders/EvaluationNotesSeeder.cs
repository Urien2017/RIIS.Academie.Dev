using Microsoft.EntityFrameworkCore;
using RIIS.Academic.Domain;

namespace RIIS.Academic.Infrastructure.Persistence.Seeders;

public static class EvaluationNotesSeeder
{
    private const string AnneeAcademiqueLibelle = "2025-2026";
    private const string SeederUser = "Seeder notes évaluations S1/S2 2025-2026";
    private const string Source = "Notes générées pour tester les PV et relevés S1/S2 2025-2026";

    public static async Task SeedNotesS1S2_2025_2026Async(
        RiisAcademicDbContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        await EvaluationSeeder.SeedEvaluationsS1S2_2025_2026Async(
            context,
            cancellationToken);

        var anneeAcademique = await context.AnneesAcademiques
            .FirstOrDefaultAsync(x => x.Libelle == AnneeAcademiqueLibelle, cancellationToken)
            ?? throw new InvalidOperationException(
                $"L'année académique '{AnneeAcademiqueLibelle}' est introuvable.");

        var evaluations = await context.EvaluationsAcademiques
            .Include(x => x.ElementConstitutif)
                .ThenInclude(x => x.UniteEnseignement)
                    .ThenInclude(x => x.SemestrePedagogique)
                        .ThenInclude(x => x.MaquettePedagogique)
            .Where(x => x.AnneeAcademiqueId == anneeAcademique.Id
                && (x.ElementConstitutif.UniteEnseignement.SemestrePedagogique.Numero == 1
                    || x.ElementConstitutif.UniteEnseignement.SemestrePedagogique.Numero == 2))
            .ToListAsync(cancellationToken);

        evaluations = evaluations
            .Where(x => IsTargetEvaluation(
                x.ElementConstitutif.UniteEnseignement.SemestrePedagogique.Numero,
                x.Type,
                x.Numero))
            .ToList();

        foreach (var evaluation in evaluations)
        {
            var semestre = evaluation.ElementConstitutif.UniteEnseignement.SemestrePedagogique;
            var maquette = semestre.MaquettePedagogique;

            var inscriptions = await context.Inscriptions
                .Where(x => x.AnneeAcademiqueId == anneeAcademique.Id
                    && x.MaquettePedagogiqueId == maquette.Id
                    && x.NiveauEtudeId == semestre.NiveauEtudeId)
                .ToListAsync(cancellationToken);

            foreach (var inscription in inscriptions)
            {
                var exists = await context.NotesEvaluations.AnyAsync(
                    x => x.EvaluationAcademiqueId == evaluation.Id
                        && x.InscriptionId == inscription.Id,
                    cancellationToken);

                if (exists)
                {
                    continue;
                }

                context.NotesEvaluations.Add(new NoteEvaluation
                {
                    EvaluationAcademiqueId = evaluation.Id,
                    InscriptionId = inscription.Id,
                    Valeur = GenerateNote(
                        inscription.Id,
                        evaluation.ElementConstitutifId,
                        semestre.Numero,
                        evaluation.Type,
                        evaluation.Numero),
                    StatutPresence = StatutPresenceEvaluation.Present,
                    Observation = Source,
                    SaisieLeUtc = DateTime.UtcNow,
                    SaisiePar = SeederUser
                });
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static bool IsTargetEvaluation(byte semestreNumero, TypeEvaluation type, byte numero)
        => semestreNumero switch
        {
            1 => type switch
            {
                TypeEvaluation.ControleContinu => numero is 1 or 2,
                TypeEvaluation.ControleConnaissance => numero is 1 or 2,
                TypeEvaluation.SessionNormale => numero == 1,
                _ => false
            },
            2 => type == TypeEvaluation.SessionNormale && numero == 1,
            _ => false
        };

    private static decimal GenerateNote(
        long inscriptionId,
        long elementConstitutifId,
        byte semestreNumero,
        TypeEvaluation type,
        byte numero)
    {
        var typeOffset = type switch
        {
            TypeEvaluation.ControleContinu => 7,
            TypeEvaluation.ControleConnaissance => 13,
            TypeEvaluation.SessionNormale => 23,
            _ => 31
        };

        var raw = (inscriptionId * 17
            + elementConstitutifId * 31
            + semestreNumero * 11
            + numero * 19
            + typeOffset) % 140;

        return Math.Round(5m + raw / 10m, 2, MidpointRounding.AwayFromZero);
    }
}
