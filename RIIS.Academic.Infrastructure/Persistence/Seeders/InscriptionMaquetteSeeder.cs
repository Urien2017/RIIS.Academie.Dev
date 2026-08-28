using Microsoft.EntityFrameworkCore;
using RIIS.Academic.Domain;

namespace RIIS.Academic.Infrastructure.Persistence.Seeders;

public static class InscriptionMaquetteSeeder
{
    private const string AnneeAcademiqueLibelle = "2025-2026";

    public static async Task AttachInscriptionsToMaquettes2025_2026Async(
        RiisAcademicDbContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        await OfficialProgrammeSeeder.SeedOfficialProgrammes2025_2026Async(
            context,
            cancellationToken);

        var anneeAcademique = await context.AnneesAcademiques
            .FirstOrDefaultAsync(x => x.Libelle == AnneeAcademiqueLibelle, cancellationToken)
            ?? throw new InvalidOperationException(
                $"L'année académique '{AnneeAcademiqueLibelle}' est introuvable.");

        var maquettes = await context.MaquettesPedagogiques
            .Where(x => x.Version == AnneeAcademiqueLibelle)
            .ToListAsync(cancellationToken);

        var parcoursAcademiques = await context.ParcoursAcademiques
            .Where(x => x.AnneeAcademiqueId == anneeAcademique.Id)
            .ToListAsync(cancellationToken);

        var classes = await context.ClassesPedagogiques
            .Where(x => x.AnneeAcademiqueId == anneeAcademique.Id)
            .ToListAsync(cancellationToken);

        foreach (var classe in classes)
        {
            var parcours = parcoursAcademiques.FirstOrDefault(x => x.Id == classe.ParcoursAcademiqueId);
            var maquette = parcours is null ? null : ResolveMaquette(maquettes, parcours);

            if (maquette is not null
                && classe.MaquettePedagogiqueId != maquette.Id)
            {
                classe.MaquettePedagogiqueId = maquette.Id;
            }
        }

        var inscriptions = await context.Inscriptions
            .Where(x => x.AnneeAcademiqueId == anneeAcademique.Id)
            .ToListAsync(cancellationToken);

        foreach (var inscription in inscriptions)
        {
            var parcours = parcoursAcademiques.FirstOrDefault(x => x.Id == inscription.ParcoursAcademiqueId);
            var maquette = parcours is null ? null : ResolveMaquette(maquettes, parcours);

            if (maquette is not null
                && inscription.MaquettePedagogiqueId != maquette.Id)
            {
                inscription.MaquettePedagogiqueId = maquette.Id;
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static MaquettePedagogique? ResolveMaquette(
        IReadOnlyCollection<MaquettePedagogique> maquettes,
        ParcoursAcademique parcours)
        => maquettes
            .Where(maquette =>
                maquette.CycleFormationId == parcours.CycleFormationId
                && maquette.NiveauEtudeId == parcours.NiveauEtudeId
                && maquette.FiliereId == parcours.FiliereId
                && maquette.SpecialiteId == parcours.SpecialiteId)
            .OrderByDescending(x => x.DateDebutValidite)
            .ThenByDescending(x => x.Id)
            .FirstOrDefault();
}
