using Microsoft.EntityFrameworkCore;
using RIIS.Academic.Domain;

namespace RIIS.Academic.Infrastructure.Persistence.Seeders;

/// <summary>
/// Cree les parcours de niveau 2 attendus par les programmes officiels et
/// l'import des etudiants 2026-2027.
/// Ce seeder ne cree ni maquette, ni semestre, ni UE, ni EC.
/// </summary>
public static class NiveauDeuxParcoursSeeder
{
    private const string AnneeLibelle = "2026-2027";

    public static async Task SeedAsync(
        RiisAcademicDbContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var annee = await EnsureAnneeAsync(context, cancellationToken);
        var niveau = await EnsureNiveauAsync(context, cancellationToken);

        var prepa = await EnsureCycleAsync(context, "PREPA", "Préparatoire", 1, cancellationToken);
        var bts = await EnsureCycleAsync(
            context,
            "BTS",
            "Brevet de technicien supérieur",
            2,
            cancellationToken);
        var licence = await EnsureCycleAsync(context, "LICENCE", "Licence", 3, cancellationToken);

        var ingenieur = await EnsureFiliereAsync(context, "INGENIEUR", "Ingénieur", cancellationToken);
        var gestion = await EnsureFiliereAsync(context, "GESTION", "Gestion", cancellationToken);
        var genieInformatique = await EnsureFiliereAsync(
            context,
            "GENIE-INFORMATIQUE",
            "Génie informatique",
            cancellationToken);
        var genieCivil = await EnsureFiliereAsync(context, "GENIE-CIVIL", "Génie civil", cancellationToken);
        var genieElectrique = await EnsureFiliereAsync(
            context,
            "GENIE-ELECTRIQUE",
            "Génie électrique",
            cancellationToken);

        var parcoursDefinitions = new[]
        {
            new ParcoursDefinition(
                prepa,
                ingenieur,
                "ING-GEN",
                "Ingénieur Généraliste",
                "2026-2027-PREPA-N2-ING-GEN",
                "2026-2027 - PREPA Niveau 2 - Ingénieur - Ingénieur Généraliste"),
            new ParcoursDefinition(
                prepa,
                ingenieur,
                "ING-INF",
                "Ingénieur Informatique",
                "2026-2027-PREPA-N2-ING-INF",
                "2026-2027 - PREPA Niveau 2 - Ingénieur - Ingénieur Informatique"),
            new ParcoursDefinition(
                prepa,
                gestion,
                "MEC",
                "Management / Économie",
                "2026-2027-PREPA-N2-MEC",
                "2026-2027 - PREPA Niveau 2 - Gestion - Management / Économie"),
            new ParcoursDefinition(
                bts,
                gestion,
                "BQF",
                "Banque et finance",
                "2026-2027-BTS-N2-BQF",
                "2026-2027 - BTS Niveau 2 - Gestion - Banque et finance"),
            new ParcoursDefinition(
                bts,
                gestion,
                "CGE",
                "Comptabilité et gestion des entreprises",
                "2026-2027-BTS-N2-CGE",
                "2026-2027 - BTS Niveau 2 - Gestion - Comptabilité et gestion des entreprises"),
            new ParcoursDefinition(
                bts,
                gestion,
                "GRH",
                "Gestion des ressources humaines",
                "2026-2027-BTS-N2-GRH",
                "2026-2027 - BTS Niveau 2 - Gestion - Gestion des ressources humaines"),
            new ParcoursDefinition(
                bts,
                genieInformatique,
                "IGL",
                "Génie logiciel",
                "2026-2027-BTS-N2-IGL",
                "2026-2027 - BTS Niveau 2 - Génie informatique - Génie logiciel"),
            new ParcoursDefinition(
                bts,
                genieInformatique,
                "RES",
                "Réseaux et Sécurité",
                "2026-2027-BTS-N2-RES",
                "2026-2027 - BTS Niveau 2 - Génie informatique - Réseaux et Sécurité"),
            new ParcoursDefinition(
                bts,
                genieCivil,
                "BAT",
                "Bâtiment",
                "2026-2027-BTS-N2-BAT",
                "2026-2027 - BTS Niveau 2 - Génie civil - Bâtiment"),
            new ParcoursDefinition(
                bts,
                genieCivil,
                "TPU",
                "Travaux publics",
                "2026-2027-BTS-N2-TPU",
                "2026-2027 - BTS Niveau 2 - Génie civil - Travaux publics"),
            new ParcoursDefinition(
                bts,
                genieCivil,
                "GTO",
                "Géomètre Topographe",
                "2026-2027-BTS-N2-GTO",
                "2026-2027 - BTS Niveau 2 - Génie civil - Géomètre Topographe"),
            new ParcoursDefinition(
                bts,
                genieElectrique,
                "ELT",
                "Electrotechnique",
                "2026-2027-BTS-N2-ELT",
                "2026-2027 - BTS Niveau 2 - Génie électrique - Electrotechnique"),
            new ParcoursDefinition(
                licence,
                gestion,
                "BQF",
                "Banque et finance",
                "2026-2027-LICENCE-N2-BQF",
                "2026-2027 - Licence Niveau 2 - Gestion - Banque et finance"),
            new ParcoursDefinition(
                licence,
                gestion,
                "CGE",
                "Comptabilité et gestion des entreprises",
                "2026-2027-LICENCE-N2-CGE",
                "2026-2027 - Licence Niveau 2 - Gestion - Comptabilité et gestion des entreprises"),
            new ParcoursDefinition(
                licence,
                gestion,
                "GRH",
                "Gestion des ressources humaines",
                "2026-2027-LICENCE-N2-GRH",
                "2026-2027 - Licence Niveau 2 - Gestion - Gestion des ressources humaines"),
            new ParcoursDefinition(
                licence,
                genieInformatique,
                "IGL",
                "Génie logiciel",
                "2026-2027-LICENCE-N2-IGL",
                "2026-2027 - Licence Niveau 2 - Génie informatique - Génie logiciel"),
            new ParcoursDefinition(
                licence,
                genieCivil,
                "BAT",
                "Bâtiment",
                "2026-2027-LICENCE-N2-BAT",
                "2026-2027 - Licence Niveau 2 - Génie civil - Bâtiment")
        };

        foreach (var definition in parcoursDefinitions)
        {
            var specialite = await EnsureSpecialiteAsync(
                context,
                definition.Filiere,
                definition.SpecialiteCode,
                definition.SpecialiteLibelle,
                cancellationToken);

            await EnsureParcoursAsync(
                context,
                annee,
                definition.Cycle,
                niveau,
                definition.Filiere,
                specialite,
                definition,
                cancellationToken);
        }

        await NiveauDeuxMaquetteSplitter.SeedExistingNiveauDeuxMaquettesAsync(
            context,
            annee,
            niveau.Id,
            cancellationToken);
    }

    private static async Task<AnneeAcademique> EnsureAnneeAsync(
        RiisAcademicDbContext context,
        CancellationToken cancellationToken)
    {
        var entity = await context.AnneesAcademiques
            .FirstOrDefaultAsync(x => x.Libelle == AnneeLibelle, cancellationToken);

        if (entity is null)
        {
            entity = new AnneeAcademique { Libelle = AnneeLibelle };
            context.AnneesAcademiques.Add(entity);
        }

        entity.AnneeDebut = 2026;
        entity.AnneeFin = 2027;
        entity.EstActive = true;
        await context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    private static async Task<CycleFormation> EnsureCycleAsync(
        RiisAcademicDbContext context,
        string code,
        string libelle,
        short ordreAffichage,
        CancellationToken cancellationToken)
    {
        var entity = await context.CyclesFormation
            .FirstOrDefaultAsync(x => x.Code == code, cancellationToken);

        if (entity is null)
        {
            entity = new CycleFormation { Code = code, Libelle = libelle };
            context.CyclesFormation.Add(entity);
        }

        entity.Libelle = libelle;
        entity.OrdreAffichage = ordreAffichage;
        entity.EstActif = true;
        await context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    private static async Task<NiveauEtude> EnsureNiveauAsync(
        RiisAcademicDbContext context,
        CancellationToken cancellationToken)
    {
        var entity = await context.NiveauxEtude
            .FirstOrDefaultAsync(x => x.Numero == 2, cancellationToken);

        if (entity is null)
        {
            entity = new NiveauEtude { Numero = 2, Libelle = "Niveau 2" };
            context.NiveauxEtude.Add(entity);
        }

        entity.Libelle = "Niveau 2";
        entity.EstActif = true;
        await context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    private static async Task<Filiere> EnsureFiliereAsync(
        RiisAcademicDbContext context,
        string code,
        string libelle,
        CancellationToken cancellationToken)
    {
        var entity = await context.Filieres
            .FirstOrDefaultAsync(x => x.Code == code, cancellationToken);

        if (entity is null)
        {
            entity = new Filiere { Code = code, Libelle = libelle };
            context.Filieres.Add(entity);
        }

        entity.Libelle = libelle;
        entity.EstActive = true;
        await context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    private static async Task<Specialite> EnsureSpecialiteAsync(
        RiisAcademicDbContext context,
        Filiere filiere,
        string code,
        string libelle,
        CancellationToken cancellationToken)
    {
        var entity = await context.Specialites
            .FirstOrDefaultAsync(
                x => x.FiliereId == filiere.Id && x.Code == code,
                cancellationToken);

        if (entity is null)
        {
            entity = new Specialite
            {
                FiliereId = filiere.Id,
                Code = code,
                Libelle = libelle
            };
            context.Specialites.Add(entity);
        }

        entity.FiliereId = filiere.Id;
        entity.Libelle = libelle;
        entity.EstActive = true;
        await context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    private static async Task EnsureParcoursAsync(
        RiisAcademicDbContext context,
        AnneeAcademique annee,
        CycleFormation cycle,
        NiveauEtude niveau,
        Filiere filiere,
        Specialite specialite,
        ParcoursDefinition definition,
        CancellationToken cancellationToken)
    {
        var entity = await context.ParcoursAcademiques
            .FirstOrDefaultAsync(x => x.Code == definition.Code, cancellationToken);

        if (entity is null)
        {
            entity = new ParcoursAcademique
            {
                Code = definition.Code,
                Libelle = definition.Libelle
            };
            context.ParcoursAcademiques.Add(entity);
        }

        entity.AnneeAcademiqueId = annee.Id;
        entity.CycleFormationId = cycle.Id;
        entity.NiveauEtudeId = niveau.Id;
        entity.FiliereId = filiere.Id;
        entity.SpecialiteId = specialite.Id;
        entity.Libelle = definition.Libelle;
        entity.EstActive = true;
        await context.SaveChangesAsync(cancellationToken);
    }

    private sealed record ParcoursDefinition(
        CycleFormation Cycle,
        Filiere Filiere,
        string SpecialiteCode,
        string SpecialiteLibelle,
        string Code,
        string Libelle);
}
