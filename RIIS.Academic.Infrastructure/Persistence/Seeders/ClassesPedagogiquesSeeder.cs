using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RIIS.Academic.Domain;

namespace RIIS.Academic.Infrastructure.Persistence.Seeders;

/// <summary>
/// Garantit la règle actuelle : un parcours académique dispose d'une seule classe.
/// Le seeder ne crée aucune classe supplémentaire si elle existe déjà.
/// </summary>
public static class ClassesPedagogiquesSeeder
{
    public static async Task SeedAsync(
        RiisAcademicDbContext dbContext,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        var parcours = await dbContext.ParcoursAcademiques
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var classes = await dbContext.ClassesPedagogiques
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var parcoursAvecClasse = classes
            .Select(x => x.ParcoursAcademiqueId)
            .ToHashSet();

        var classesACreer = parcours
            .Where(x => !parcoursAvecClasse.Contains(x.Id))
            .Select(x => new ClassePedagogique
            {
                ParcoursAcademiqueId = x.Id,
                Libelle = BuildLibelle(x),
                Effectif = 0
            })
            .ToList();

        if (classesACreer.Count > 0)
        {
            await dbContext.ClassesPedagogiques.AddRangeAsync(classesACreer, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        logger?.LogInformation(
            "Classes pédagogiques synchronisées. Parcours={Parcours}, Classes existantes={Existantes}, Classes créées={Creees}, Total={Total}",
            parcours.Count,
            classes.Count,
            classesACreer.Count,
            classes.Count + classesACreer.Count);
    }

    private static string BuildLibelle(ParcoursAcademique parcours)
    {
        var libelle = string.IsNullOrWhiteSpace(parcours.Libelle)
            ? parcours.Code
            : parcours.Libelle;

        return string.IsNullOrWhiteSpace(libelle)
            ? $"Classe {parcours.Id}"
            : libelle.Length <= 200 ? libelle : libelle[..200];
    }
}
