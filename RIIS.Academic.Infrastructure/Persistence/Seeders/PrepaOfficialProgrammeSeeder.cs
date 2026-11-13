using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RIIS.Academic.Domain;

namespace RIIS.Academic.Infrastructure.Persistence.Seeders;

/// <summary>
/// Importe les programmes PREPA officiels depuis le fichier JSON issu des documents Word.
/// Les codes techniques des UE sont propres à chaque maquette lorsque le document source
/// réutilise un même code officiel pour deux spécialités différentes.
/// </summary>
public static class PrepaOfficialProgrammeSeeder
{
    private const string ResourceName = "programme-officiel-prepa-2026-2027.json";

    public static async Task SeedAsync(
        RiisAcademicDbContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var data = await LoadAsync(cancellationToken);

        foreach (var programmeData in data.Programmes)
        {
            var annee = await EnsureAnneeAsync(context, programmeData.AnneeAcademique, cancellationToken);
            var cycle = await EnsureCycleAsync(context, programmeData.Cycle, cancellationToken);
            var niveau = await EnsureNiveauAsync(context, programmeData.Niveau, cancellationToken);
            var niveau2 = await EnsureNiveauAsync(
                context,
                new NiveauData { Numero = 2, Libelle = "Niveau 2" },
                cancellationToken);
            var filiere = await EnsureFiliereAsync(context, programmeData.Filiere, cancellationToken);
            var specialite = await EnsureSpecialiteAsync(context, programmeData.Specialite, filiere, cancellationToken);
            var parcours = await EnsureParcoursAsync(
                context,
                programmeData.Programme,
                annee,
                cycle,
                niveau,
                filiere,
                specialite,
                cancellationToken);
            var maquette = await EnsureMaquetteAsync(
                context,
                programmeData,
                programmeData.Programme,
                annee,
                cancellationToken);
            var maquetteNiveau2 = await NiveauDeuxMaquetteSplitter.EnsureNiveauDeuxMaquetteAsync(
                context,
                maquette,
                niveau2.Id,
                cancellationToken);

            await EnsureAffectationAsync(context, parcours, maquette, annee, cancellationToken);

            var parcoursNiveau2 = await context.ParcoursAcademiques
                .FirstOrDefaultAsync(
                    x => x.Code == programmeData.Programme.ParcoursCodeForNiveau2,
                    cancellationToken);

            if (parcoursNiveau2 is not null)
            {
                await EnsureAffectationAsync(
                    context,
                    parcoursNiveau2,
                    maquetteNiveau2,
                    annee,
                    cancellationToken);
            }

            foreach (var semestreData in programmeData.Programme.Semestres.OrderBy(x => x.Numero))
            {
                var maquetteSemestre = semestreData.Numero <= 2
                    ? maquette
                    : maquetteNiveau2;
                var semestre = await EnsureSemestreAsync(
                    context,
                    maquetteSemestre,
                    semestreData,
                    semestreData.Numero <= 2 ? niveau.Id : niveau2.Id,
                    cancellationToken);

                foreach (var uniteData in semestreData.UnitesEnseignement.OrderBy(x => x.OrdreAffichage))
                {
                    var unite = await EnsureUniteAsync(context, maquetteSemestre, uniteData, cancellationToken);

                    foreach (var elementData in uniteData.ElementsConstitutifs.OrderBy(x => x.OrdreAffichage))
                    {
                        var element = await EnsureElementAsync(context, unite, elementData, cancellationToken);
                        await EnsureMaquetteElementAsync(context, semestre, element, elementData, cancellationToken);
                    }
                }
            }
        }
    }

    private static async Task<AnneeAcademique> EnsureAnneeAsync(
        RiisAcademicDbContext context,
        AnneeData data,
        CancellationToken cancellationToken)
    {
        var entity = await context.AnneesAcademiques
            .FirstOrDefaultAsync(x => x.Libelle == data.Libelle, cancellationToken);

        if (entity is null)
        {
            entity = new AnneeAcademique { Libelle = data.Libelle };
            context.AnneesAcademiques.Add(entity);
        }

        entity.AnneeDebut = data.AnneeDebut;
        entity.AnneeFin = data.AnneeFin;
        entity.EstActive = true;
        await context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    private static async Task<CycleFormation> EnsureCycleAsync(
        RiisAcademicDbContext context,
        CycleData data,
        CancellationToken cancellationToken)
    {
        var entity = await context.CyclesFormation
            .FirstOrDefaultAsync(x => x.Code == data.Code, cancellationToken);

        if (entity is null)
        {
            entity = new CycleFormation { Code = data.Code, Libelle = data.Libelle };
            context.CyclesFormation.Add(entity);
        }

        entity.Libelle = data.Libelle;
        entity.OrdreAffichage = data.OrdreAffichage;
        entity.EstActif = true;
        await context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    private static async Task<NiveauEtude> EnsureNiveauAsync(
        RiisAcademicDbContext context,
        NiveauData data,
        CancellationToken cancellationToken)
    {
        var entity = await context.NiveauxEtude
            .FirstOrDefaultAsync(x => x.Numero == data.Numero, cancellationToken);

        if (entity is null)
        {
            entity = new NiveauEtude { Numero = data.Numero, Libelle = data.Libelle };
            context.NiveauxEtude.Add(entity);
        }

        entity.Libelle = data.Libelle;
        entity.EstActif = true;
        await context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    private static async Task<Filiere> EnsureFiliereAsync(
        RiisAcademicDbContext context,
        FiliereData data,
        CancellationToken cancellationToken)
    {
        var entity = await context.Filieres
            .FirstOrDefaultAsync(x => x.Code == data.Code, cancellationToken);

        if (entity is null)
        {
            entity = new Filiere { Code = data.Code, Libelle = data.Libelle };
            context.Filieres.Add(entity);
        }

        entity.Libelle = data.Libelle;
        entity.EstActive = true;
        await context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    private static async Task<Specialite> EnsureSpecialiteAsync(
        RiisAcademicDbContext context,
        SpecialiteData data,
        Filiere filiere,
        CancellationToken cancellationToken)
    {
        var entity = await context.Specialites
            .FirstOrDefaultAsync(
                x => x.FiliereId == filiere.Id && x.Code == data.Code,
                cancellationToken);

        if (entity is null)
        {
            entity = new Specialite
            {
                FiliereId = filiere.Id,
                Code = data.Code,
                Libelle = data.Libelle
            };
            context.Specialites.Add(entity);
        }

        entity.Libelle = data.Libelle;
        entity.EstActive = true;
        await context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    private static async Task<ParcoursAcademique> EnsureParcoursAsync(
        RiisAcademicDbContext context,
        ProgrammeData data,
        AnneeAcademique annee,
        CycleFormation cycle,
        NiveauEtude niveau,
        Filiere filiere,
        Specialite specialite,
        CancellationToken cancellationToken)
    {
        var entity = await context.ParcoursAcademiques
            .FirstOrDefaultAsync(x => x.Code == data.ParcoursCode, cancellationToken);

        if (entity is null)
        {
            entity = new ParcoursAcademique
            {
                Code = data.ParcoursCode,
                Libelle = data.ParcoursLibelle
            };
            context.ParcoursAcademiques.Add(entity);
        }

        entity.AnneeAcademiqueId = annee.Id;
        entity.CycleFormationId = cycle.Id;
        entity.NiveauEtudeId = niveau.Id;
        entity.FiliereId = filiere.Id;
        entity.SpecialiteId = specialite.Id;
        entity.Libelle = data.ParcoursLibelle;
        entity.EstActive = true;
        await context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    private static async Task<MaquettePedagogique> EnsureMaquetteAsync(
        RiisAcademicDbContext context,
        ProgrammeDocumentData document,
        ProgrammeData data,
        AnneeAcademique annee,
        CancellationToken cancellationToken)
    {
        var entity = await context.MaquettesPedagogiques
            .FirstOrDefaultAsync(
                x => x.Code == data.MaquetteCode && x.Version == document.Version,
                cancellationToken);

        if (entity is null)
        {
            entity = new MaquettePedagogique
            {
                Code = data.MaquetteCode,
                Libelle = data.MaquetteLibelle,
                Version = document.Version
            };
            context.MaquettesPedagogiques.Add(entity);
        }

        entity.Libelle = data.MaquetteLibelle;
        entity.Version = document.Version;
        entity.Statut = StatutMaquettePedagogique.Active;
        entity.DateDebutValidite = new DateOnly(annee.AnneeDebut, 9, 1);
        entity.DateFinValidite = new DateOnly(annee.AnneeFin, 8, 1);
        entity.SourceDocument = document.SourceDocument;
        entity.ParcoursAcademiqueId = null;
        await context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    private static async Task EnsureAffectationAsync(
        RiisAcademicDbContext context,
        ParcoursAcademique parcours,
        MaquettePedagogique maquette,
        AnneeAcademique annee,
        CancellationToken cancellationToken)
    {
        var affectation = await context.AffectationsMaquettes
            .FirstOrDefaultAsync(
                x => x.ParcoursAcademiqueId == parcours.Id
                    && x.MaquettePedagogiqueId == maquette.Id,
                cancellationToken);

        var autres = await context.AffectationsMaquettes
            .Where(x => x.ParcoursAcademiqueId == parcours.Id && x.MaquettePedagogiqueId != maquette.Id)
            .ToListAsync(cancellationToken);

        foreach (var autre in autres.Where(x => x.EstActive && x.EstPrincipale))
        {
            autre.EstPrincipale = false;
        }

        if (affectation is null)
        {
            affectation = new AffectationMaquette
            {
                ParcoursAcademiqueId = parcours.Id,
                MaquettePedagogiqueId = maquette.Id
            };
            context.AffectationsMaquettes.Add(affectation);
        }

        affectation.EstActive = true;
        affectation.EstPrincipale = true;
        affectation.DateDebut = new DateOnly(annee.AnneeDebut, 9, 1);
        affectation.DateFin = new DateOnly(annee.AnneeFin, 8, 1);
        affectation.Observation = "Affectation créée par import du programme officiel PREPA.";
        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task<SemestrePedagogique> EnsureSemestreAsync(
        RiisAcademicDbContext context,
        MaquettePedagogique maquette,
        SemestreData data,
        long niveauEtudeId,
        CancellationToken cancellationToken)
    {
        var entity = await context.SemestresPedagogiques
            .FirstOrDefaultAsync(
                x => x.MaquettePedagogiqueId == maquette.Id && x.NumeroSemestre == data.Numero,
                cancellationToken);

        if (entity is null)
        {
            entity = new SemestrePedagogique
            {
                MaquettePedagogiqueId = maquette.Id,
                NumeroSemestre = data.Numero,
                Libelle = data.Libelle
            };
            context.SemestresPedagogiques.Add(entity);
        }

        entity.Libelle = data.Libelle;
        entity.NiveauEtudeId = niveauEtudeId;
        entity.OrdreAffichage = ToShort(data.OrdreAffichage);
        await context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    private static async Task<UniteEnseignement> EnsureUniteAsync(
        RiisAcademicDbContext context,
        MaquettePedagogique maquette,
        UniteData data,
        CancellationToken cancellationToken)
    {
        var entity = await context.UnitesEnseignement
            .FirstOrDefaultAsync(x => x.Code == data.Code, cancellationToken);

        if (entity is null)
        {
            entity = new UniteEnseignement { Code = data.Code, Libelle = data.Libelle };
            context.UnitesEnseignement.Add(entity);
        }
        else if (entity.MaquettePedagogiqueId is not null && entity.MaquettePedagogiqueId != maquette.Id)
        {
            throw new InvalidOperationException(
                $"L'UE technique '{data.Code}' est déjà rattachée à une autre maquette.");
        }

        entity.MaquettePedagogiqueId = maquette.Id;
        entity.Libelle = data.Libelle;
        entity.Type = TypeElementConstitutif.CoursMagistraux;
        await context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    private static async Task<ElementConstitutif> EnsureElementAsync(
        RiisAcademicDbContext context,
        UniteEnseignement unite,
        ElementData data,
        CancellationToken cancellationToken)
    {
        var entity = await context.ElementsConstitutifs
            .FirstOrDefaultAsync(
                x => x.UniteEnseignementId == unite.Id && x.Code == data.Code,
                cancellationToken);

        if (entity is null)
        {
            entity = new ElementConstitutif
            {
                UniteEnseignementId = unite.Id,
                Code = data.Code,
                Libelle = data.Libelle
            };
            context.ElementsConstitutifs.Add(entity);
        }

        entity.Libelle = data.Libelle;
        entity.Type = TypeElementConstitutif.CoursMagistraux;
        entity.Credits = data.Credits;
        entity.VolumeHoraire = ToShort(data.VolumeHoraire);
        await context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    private static async Task EnsureMaquetteElementAsync(
        RiisAcademicDbContext context,
        SemestrePedagogique semestre,
        ElementConstitutif element,
        ElementData data,
        CancellationToken cancellationToken)
    {
        var entity = await context.MaquetteElementsConstitutifs
            .FirstOrDefaultAsync(
                x => x.SemestrePedagogiqueId == semestre.Id
                    && x.ElementConstitutifId == element.Id,
                cancellationToken);

        if (entity is null)
        {
            entity = new MaquetteElementConstitutif
            {
                SemestrePedagogiqueId = semestre.Id,
                ElementConstitutifId = element.Id
            };
            context.MaquetteElementsConstitutifs.Add(entity);
        }

        entity.Credits = data.Credits;
        entity.VolumeHoraire = ToShort(data.VolumeHoraire);
        entity.Coefficient = 1m;
        entity.OrdreAffichage = ToShort(data.OrdreAffichage);
        entity.EstObligatoire = true;
        await context.SaveChangesAsync(cancellationToken);
    }

    private static short ToShort(int value)
    {
        if (value is < short.MinValue or > short.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "La valeur dépasse la capacité d'un Int16.");
        }

        return (short)value;
    }

    private static async Task<SeedData> LoadAsync(CancellationToken cancellationToken)
    {
        var assembly = typeof(PrepaOfficialProgrammeSeeder).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .SingleOrDefault(x => x.EndsWith(ResourceName, StringComparison.OrdinalIgnoreCase))
            ?? throw new FileNotFoundException($"La ressource '{ResourceName}' est introuvable.");

        await using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new FileNotFoundException($"Impossible d'ouvrir la ressource '{resourceName}'.");

        return await JsonSerializer.DeserializeAsync<SeedData>(
                   stream,
                   new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                   cancellationToken)
               ?? throw new InvalidOperationException($"La ressource '{resourceName}' est vide ou invalide.");
    }

    private sealed class SeedData
    {
        public required List<ProgrammeDocumentData> Programmes { get; set; }
    }

    private sealed class ProgrammeDocumentData
    {
        public required string SourceDocument { get; set; }
        public required string Version { get; set; }
        public required AnneeData AnneeAcademique { get; set; }
        public required CycleData Cycle { get; set; }
        public required NiveauData Niveau { get; set; }
        public required FiliereData Filiere { get; set; }
        public required SpecialiteData Specialite { get; set; }
        public required ProgrammeData Programme { get; set; }
    }

    private sealed class AnneeData
    {
        public required string Libelle { get; set; }
        public short AnneeDebut { get; set; }
        public short AnneeFin { get; set; }
    }

    private sealed class CycleData
    {
        public required string Code { get; set; }
        public required string Libelle { get; set; }
        public short OrdreAffichage { get; set; }
    }

    private sealed class NiveauData
    {
        public byte Numero { get; set; }
        public required string Libelle { get; set; }
    }

    private sealed class FiliereData
    {
        public required string Code { get; set; }
        public required string Libelle { get; set; }
    }

    private sealed class SpecialiteData
    {
        public required string Code { get; set; }
        public required string Libelle { get; set; }
    }

    private sealed class ProgrammeData
    {
        public required string ParcoursCode { get; set; }
        public required string ParcoursLibelle { get; set; }
        public required string MaquetteCode { get; set; }
        public required string MaquetteLibelle { get; set; }
        public List<SemestreData> Semestres { get; set; } = [];

        public string ParcoursCodeForNiveau2
            => ParcoursCode.Replace("-N1-", "-N2-", StringComparison.OrdinalIgnoreCase);
    }

    private sealed class SemestreData
    {
        public byte Numero { get; set; }
        public required string Libelle { get; set; }
        public int OrdreAffichage { get; set; }
        public List<UniteData> UnitesEnseignement { get; set; } = [];
    }

    private sealed class UniteData
    {
        public required string Code { get; set; }
        public string? CodeSource { get; set; }
        public required string Libelle { get; set; }
        public int OrdreAffichage { get; set; }
        public decimal Credits { get; set; }
        public int VolumeHoraire { get; set; }
        public List<ElementData> ElementsConstitutifs { get; set; } = [];
    }

    private sealed class ElementData
    {
        public required string Code { get; set; }
        public string? CodeSource { get; set; }
        public required string Libelle { get; set; }
        public decimal Credits { get; set; }
        public int VolumeHoraire { get; set; }
        public int OrdreAffichage { get; set; }
    }
}
