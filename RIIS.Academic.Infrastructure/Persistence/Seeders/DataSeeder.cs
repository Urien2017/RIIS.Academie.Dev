using Microsoft.EntityFrameworkCore;
using RIIS.Academic.Domain;

namespace RIIS.Academic.Infrastructure.Persistence.Seeders;

public static class DataSeeder
{
    private const short CurrentAnneeDebut = 2025;
    private const short CurrentAnneeFin = 2026;
    private const string CurrentAnneeLibelle = "2025-2026";
    private const string LegacyAnneeLibelle = "2026-2027";

    public static async Task SeedAsync(
        RiisAcademicDbContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var anneeAcademique = await EnsureAnneeAcademiqueAsync(context, cancellationToken);
        await CorrectLegacyAcademicYearAsync(context, anneeAcademique, cancellationToken);
        await EnsureCyclesFormationAsync(context, cancellationToken);
        await SeedFilieresEtSpecialitesAsync(context, cancellationToken);
    }

    public static async Task SeedFilieresEtSpecialitesAsync(
        RiisAcademicDbContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        await EnsureFilieresAsync(context, cancellationToken);
        await EnsureSpecialitesAsync(context, cancellationToken);
    }

    private static async Task<AnneeAcademique> EnsureAnneeAcademiqueAsync(
        RiisAcademicDbContext context,
        CancellationToken cancellationToken)
    {
        var anneeAcademique = await context.AnneesAcademiques
            .FirstOrDefaultAsync(x => x.Libelle == CurrentAnneeLibelle, cancellationToken);

        if (anneeAcademique is not null)
        {
            anneeAcademique.AnneeDebut = CurrentAnneeDebut;
            anneeAcademique.AnneeFin = CurrentAnneeFin;
            anneeAcademique.EstActive = true;
            await context.SaveChangesAsync(cancellationToken);

            return anneeAcademique;
        }

        anneeAcademique = new AnneeAcademique
        {
            Libelle = CurrentAnneeLibelle,
            AnneeDebut = CurrentAnneeDebut,
            AnneeFin = CurrentAnneeFin,
            EstActive = true
        };

        context.AnneesAcademiques.Add(anneeAcademique);
        await context.SaveChangesAsync(cancellationToken);

        return anneeAcademique;
    }

    private static async Task CorrectLegacyAcademicYearAsync(
        RiisAcademicDbContext context,
        AnneeAcademique currentAnneeAcademique,
        CancellationToken cancellationToken)
    {
        var legacyAnneeAcademique = await context.AnneesAcademiques
            .FirstOrDefaultAsync(x => x.Libelle == LegacyAnneeLibelle, cancellationToken);

        if (legacyAnneeAcademique is null || legacyAnneeAcademique.Id == currentAnneeAcademique.Id)
        {
            return;
        }

        foreach (var classe in await context.ClassesPedagogiques
            .Where(x => x.AnneeAcademiqueId == legacyAnneeAcademique.Id)
            .ToListAsync(cancellationToken))
        {
            var duplicate = await context.ClassesPedagogiques.AnyAsync(
                x => x.Id != classe.Id
                    && x.AnneeAcademiqueId == currentAnneeAcademique.Id
                    && x.ParcoursAcademiqueId == classe.ParcoursAcademiqueId
                    && x.NiveauEtudeId == classe.NiveauEtudeId
                    && x.Code == classe.Code,
                cancellationToken);

            if (!duplicate)
            {
                classe.AnneeAcademiqueId = currentAnneeAcademique.Id;
            }
        }

        foreach (var inscription in await context.Inscriptions
            .Where(x => x.AnneeAcademiqueId == legacyAnneeAcademique.Id)
            .ToListAsync(cancellationToken))
        {
            var duplicate = await context.Inscriptions.AnyAsync(
                x => x.Id != inscription.Id
                    && x.AnneeAcademiqueId == currentAnneeAcademique.Id
                    && x.EtudiantId == inscription.EtudiantId,
                cancellationToken);

            if (!duplicate)
            {
                inscription.AnneeAcademiqueId = currentAnneeAcademique.Id;
            }
        }

        foreach (var evaluation in await context.EvaluationsAcademiques
            .Where(x => x.AnneeAcademiqueId == legacyAnneeAcademique.Id)
            .ToListAsync(cancellationToken))
        {
            var duplicate = await context.EvaluationsAcademiques.AnyAsync(
                x => x.Id != evaluation.Id
                    && x.AnneeAcademiqueId == currentAnneeAcademique.Id
                    && x.ElementConstitutifId == evaluation.ElementConstitutifId
                    && x.Type == evaluation.Type
                    && x.Numero == evaluation.Numero,
                cancellationToken);

            if (!duplicate)
            {
                evaluation.AnneeAcademiqueId = currentAnneeAcademique.Id;
            }
        }

        foreach (var resultatAnnuel in await context.ResultatsAnnuels
            .Where(x => x.AnneeAcademiqueId == legacyAnneeAcademique.Id)
            .ToListAsync(cancellationToken))
        {
            var duplicate = await context.ResultatsAnnuels.AnyAsync(
                x => x.Id != resultatAnnuel.Id
                    && x.AnneeAcademiqueId == currentAnneeAcademique.Id
                    && x.InscriptionId == resultatAnnuel.InscriptionId
                    && x.NiveauEtudeId == resultatAnnuel.NiveauEtudeId,
                cancellationToken);

            if (!duplicate)
            {
                resultatAnnuel.AnneeAcademiqueId = currentAnneeAcademique.Id;
            }
        }

        foreach (var procesVerbal in await context.ProcesVerbaux
            .Where(x => x.AnneeAcademiqueId == legacyAnneeAcademique.Id)
            .ToListAsync(cancellationToken))
        {
            procesVerbal.AnneeAcademiqueId = currentAnneeAcademique.Id;
        }

        legacyAnneeAcademique.EstActive = false;
        currentAnneeAcademique.EstActive = true;

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureCyclesFormationAsync(
        RiisAcademicDbContext context,
        CancellationToken cancellationToken)
    {
        var cycles = new[]
        {
            new CycleDefinition("PREPA", "Préparatoire", 1),
            new CycleDefinition("BTS", "Brevet de technicien supérieur", 2),
            new CycleDefinition("LICENCE", "Licence", 3),
            new CycleDefinition("MASTER", "Master", 4)
        };

        foreach (var cycle in cycles)
        {
            var exists = await context.CyclesFormation
                .AnyAsync(x => x.Code == cycle.Code, cancellationToken);

            if (exists)
            {
                continue;
            }

            context.CyclesFormation.Add(new CycleFormation
            {
                Code = cycle.Code,
                Libelle = cycle.Libelle,
                OrdreAffichage = cycle.OrdreAffichage,
                EstActif = true
            });
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureFilieresAsync(
        RiisAcademicDbContext context,
        CancellationToken cancellationToken)
    {
        foreach (var filiere in Filieres)
        {
            var exists = await context.Filieres
                .AnyAsync(x => x.Code == filiere.Code, cancellationToken);

            if (exists)
            {
                continue;
            }

            context.Filieres.Add(new Filiere
            {
                Code = filiere.Code,
                Libelle = filiere.Libelle,
                EstActive = true
            });
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureSpecialitesAsync(
        RiisAcademicDbContext context,
        CancellationToken cancellationToken)
    {
        var filieres = await context.Filieres
            .ToDictionaryAsync(x => x.Code, cancellationToken);

        foreach (var specialite in Specialites)
        {
            if (!filieres.TryGetValue(specialite.FiliereCode, out var filiere))
            {
                throw new InvalidOperationException(
                    $"La filière '{specialite.FiliereCode}' est introuvable pour la spécialité '{specialite.Code}'.");
            }

            var exists = await context.Specialites
                .AnyAsync(
                    x => x.FiliereId == filiere.Id && x.Code == specialite.Code,
                    cancellationToken);

            if (exists)
            {
                continue;
            }

            context.Specialites.Add(new Specialite
            {
                FiliereId = filiere.Id,
                Code = specialite.Code,
                Libelle = specialite.Libelle,
                EstActive = true
            });
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static readonly FiliereDefinition[] Filieres =
    [
        new("MANAGEMENT-ECONOMIE", "Management / Économie"),
        new("INGENIEUR", "Ingénieur"),
        new("TERTIAIRE", "Tertiaire"),
        new("INFORMATIQUE", "Informatique"),
        new("GENIE-CIVIL", "Génie civil"),
        new("GENIE-ELECTRIQUE", "Génie électrique")
    ];

    private static readonly SpecialiteDefinition[] Specialites =
    [
        new("TERTIAIRE", "BF", "Banque et finance"),
        new("TERTIAIRE", "CGE", "Comptabilité et gestion des entreprises"),
        new("TERTIAIRE", "GRH", "Gestion des ressources humaines"),
        new("INFORMATIQUE", "GL", "Génie logiciel"),
        new("INFORMATIQUE", "RS", "Réseaux et Sécurité"),
        new("GENIE-CIVIL", "BAT", "Bâtiment"),
        new("GENIE-CIVIL", "TP", "Travaux publics"),
        new("GENIE-CIVIL", "GT", "Géomètre Topographe"),
        new("GENIE-ELECTRIQUE", "ELT", "Electrotechnique"),
        new("INGENIEUR", "ING", "Ingénierie"),
        new("MANAGEMENT-ECONOMIE", "MG", "Management généraliste")
    ];

    private readonly record struct CycleDefinition(
        string Code,
        string Libelle,
        short OrdreAffichage);

    private readonly record struct FiliereDefinition(
        string Code,
        string Libelle);

    private readonly record struct SpecialiteDefinition(
        string FiliereCode,
        string Code,
        string Libelle);

}
