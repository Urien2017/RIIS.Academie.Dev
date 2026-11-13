using Microsoft.EntityFrameworkCore;
using RIIS.Academic.Domain;

namespace RIIS.Academic.Infrastructure.Persistence.Seeders;

/// <summary>
/// Initialise les donnees de reference de la scolarite.
///
/// FinancesScolarite est une interface de caisse, pas une entite de persistance.
/// Les dossiers financiers et les paiements sont donc crees par le premier
/// paiement libre, et ne sont pas fabriques par ce seeder.
/// </summary>
public static class ScolariteSeeder
{
    private static readonly TypeElementDefinition[] TypeElements =
    [
        new("EDD", "Etude de dossier", 1, 25_000m),
        new("INS", "Inscription", 2, 50_000m),
        new("SCO", "Scolarite", 3, 1_500_000m)
    ];

    private static readonly ModePaiementDefinition[] ModesPaiement =
    [
        new("ESP", "Especes", 1),
        new("VIR", "Virement bancaire", 2),
        new("MOB", "Mobile Money", 3),
        new("CHQ", "Cheque", 4)
    ];

    public static async Task SeedAsync(
        RiisAcademicDbContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var types = await EnsureTypeElementsAsync(context, cancellationToken);
        await EnsureModesPaiementAsync(context, cancellationToken);
        await EnsureTarifsAsync(context, types, cancellationToken);
    }

    private static async Task<IReadOnlyDictionary<string, TypeElementScolarite>> EnsureTypeElementsAsync(
        RiisAcademicDbContext context,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, TypeElementScolarite>(StringComparer.OrdinalIgnoreCase);

        foreach (var definition in TypeElements)
        {
            var entity = await context.TypesElementsScolarite
                .FirstOrDefaultAsync(x => x.Code == definition.Code, cancellationToken);

            if (entity is null)
            {
                entity = new TypeElementScolarite
                {
                    Code = definition.Code,
                    Libelle = definition.Libelle,
                    Categorie = CategorieTypeElementScolarite.Frais,
                    EstPayable = true,
                    EstDocumentaire = false,
                    EstSoumisValidation = false,
                    EstObligatoire = true,
                    OrdreAffichage = definition.OrdreAffichage,
                    EstActif = true
                };

                context.TypesElementsScolarite.Add(entity);
            }
            else
            {
                entity.Libelle = definition.Libelle;
                entity.Categorie = CategorieTypeElementScolarite.Frais;
                entity.EstPayable = true;
                entity.EstDocumentaire = false;
                entity.EstSoumisValidation = false;
                entity.EstObligatoire = true;
                entity.OrdreAffichage = definition.OrdreAffichage;
                entity.EstActif = true;
            }

            result[definition.Code] = entity;
        }

        await context.SaveChangesAsync(cancellationToken);
        return result;
    }

    private static async Task EnsureModesPaiementAsync(
        RiisAcademicDbContext context,
        CancellationToken cancellationToken)
    {
        foreach (var definition in ModesPaiement)
        {
            var entity = await context.ModesPaiementScolarite
                .FirstOrDefaultAsync(x => x.Code == definition.Code, cancellationToken);

            if (entity is null)
            {
                context.ModesPaiementScolarite.Add(new ModePaiementScolarite
                {
                    Code = definition.Code,
                    Libelle = definition.Libelle,
                    OrdreAffichage = definition.OrdreAffichage,
                    EstActif = true
                });
            }
            else
            {
                entity.Libelle = definition.Libelle;
                entity.OrdreAffichage = definition.OrdreAffichage;
                entity.EstActif = true;
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureTarifsAsync(
        RiisAcademicDbContext context,
        IReadOnlyDictionary<string, TypeElementScolarite> types,
        CancellationToken cancellationToken)
    {
        var parcours = await context.ParcoursAcademiques
            .Where(x => x.EstActive)
            .ToListAsync(cancellationToken);

        if (parcours.Count == 0)
        {
            return;
        }

        var typeIds = types.Values.Select(x => x.Id).ToArray();
        var tarifsExistants = await context.TarifsScolarite
            .Where(x => typeIds.Contains(x.TypeElementScolariteId))
            .ToListAsync(cancellationToken);

        foreach (var parcoursItem in parcours)
        {
            foreach (var definition in TypeElements)
            {
                var type = types[definition.Code];
                var tarif = tarifsExistants.FirstOrDefault(x =>
                    x.TypeElementScolariteId == type.Id
                    && x.ParcoursAcademiqueId == parcoursItem.Id);

                if (tarif is not null)
                {
                    // Ne pas ecraser un montant ou une date modifies manuellement.
                    continue;
                }

                context.TarifsScolarite.Add(new TarifScolarite
                {
                    Code = $"{type.Code}-{parcoursItem.Code}",
                    TypeElementScolariteId = type.Id,
                    ParcoursAcademiqueId = parcoursItem.Id,
                    Montant = definition.Montant,
                    Devise = "XAF",
                    Priorite = 0,
                    EstActif = true
                });
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private readonly record struct TypeElementDefinition(
        string Code,
        string Libelle,
        int OrdreAffichage,
        decimal Montant);

    private readonly record struct ModePaiementDefinition(
        string Code,
        string Libelle,
        int OrdreAffichage);
}
