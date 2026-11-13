using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RIIS.Academic.Domain;

namespace RIIS.Academic.Infrastructure.Persistence.Seeders;

public static class BtsOfficialProgrammeSeeder
{
    private const string ResourceName = "programme-officiel-bts1.json";

    public static async Task SeedAsync(
        RiisAcademicDbContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var data = await LoadAsync(cancellationToken);
        var annee = await EnsureAnneeAsync(context, data.AnneeAcademique, cancellationToken);
        var cycle = await EnsureCycleAsync(context, data.Cycle, cancellationToken);
        var niveau = await EnsureNiveauAsync(context, data.Niveau, cancellationToken);
        var niveau2 = await EnsureNiveauAsync(
            context,
            new NiveauData { Numero = 2, Libelle = "Niveau 2" },
            cancellationToken);
        var specialites = await EnsureFilieresEtSpecialitesAsync(context, data.Filieres, cancellationToken);

        foreach (var filiereData in data.Filieres)
        {
            foreach (var specialiteData in filiereData.Specialites)
            {
                var specialite = specialites[specialiteData.Code];
                var programme = specialiteData.Programme;

                var parcours = await EnsureParcoursAsync(
                    context,
                    programme,
                    annee,
                    cycle,
                    niveau,
                    specialite.FiliereId,
                    specialite.Id,
                    cancellationToken);

                var maquette = await EnsureMaquetteAsync(
                    context,
                    programme,
                    annee,
                    cancellationToken);
                var maquetteNiveau2 = await NiveauDeuxMaquetteSplitter.EnsureNiveauDeuxMaquetteAsync(
                    context,
                    maquette,
                    niveau2.Id,
                    cancellationToken);

                await EnsureAffectationAsync(
                    context,
                    parcours,
                    maquette,
                    annee,
                    cancellationToken);

                var parcoursNiveau2 = await context.ParcoursAcademiques
                    .FirstOrDefaultAsync(
                        x => x.Code == programme.ParcoursCodeForNiveau2,
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

                foreach (var semestreData in programme.Semestres.OrderBy(x => x.Numero))
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
                        var unite = await EnsureUniteAsync(
                            context,
                            maquetteSemestre,
                            uniteData,
                            cancellationToken);

                        foreach (var elementData in uniteData.ElementsConstitutifs.OrderBy(x => x.OrdreAffichage))
                        {
                            var element = await EnsureElementAsync(
                                context,
                                unite,
                                elementData,
                                cancellationToken);

                            await EnsureMaquetteElementAsync(
                                context,
                                semestre,
                                element,
                                elementData,
                                cancellationToken);
                        }
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
        var annee = await context.AnneesAcademiques
            .FirstOrDefaultAsync(x => x.Libelle == data.Libelle, cancellationToken);

        if (annee is null)
        {
            annee = new AnneeAcademique { Libelle = data.Libelle };
            context.AnneesAcademiques.Add(annee);
        }

        annee.AnneeDebut = data.AnneeDebut;
        annee.AnneeFin = data.AnneeFin;
        annee.EstActive = true;

        await context.SaveChangesAsync(cancellationToken);
        return annee;
    }

    private static async Task<CycleFormation> EnsureCycleAsync(
        RiisAcademicDbContext context,
        CycleData data,
        CancellationToken cancellationToken)
    {
        var cycle = await context.CyclesFormation
            .FirstOrDefaultAsync(x => x.Code == data.Code, cancellationToken);

        if (cycle is null)
        {
            cycle = new CycleFormation { Code = data.Code, Libelle = data.Libelle };
            context.CyclesFormation.Add(cycle);
        }

        cycle.Libelle = data.Libelle;
        cycle.OrdreAffichage = data.OrdreAffichage;
        cycle.EstActif = true;

        await context.SaveChangesAsync(cancellationToken);
        return cycle;
    }

    private static async Task<NiveauEtude> EnsureNiveauAsync(
        RiisAcademicDbContext context,
        NiveauData data,
        CancellationToken cancellationToken)
    {
        var niveau = await context.NiveauxEtude
            .FirstOrDefaultAsync(x => x.Numero == data.Numero, cancellationToken);

        if (niveau is null)
        {
            niveau = new NiveauEtude { Numero = data.Numero, Libelle = data.Libelle };
            context.NiveauxEtude.Add(niveau);
        }

        niveau.Libelle = data.Libelle;
        niveau.EstActif = true;

        await context.SaveChangesAsync(cancellationToken);
        return niveau;
    }

    private static async Task<Dictionary<string, Specialite>> EnsureFilieresEtSpecialitesAsync(
        RiisAcademicDbContext context,
        IReadOnlyCollection<FiliereData> data,
        CancellationToken cancellationToken)
    {
        var filieres = new Dictionary<string, Filiere>(StringComparer.OrdinalIgnoreCase);

        foreach (var filiereData in data)
        {
            var filiere = await context.Filieres
                .FirstOrDefaultAsync(x => x.Code == filiereData.Code, cancellationToken);

            if (filiere is null)
            {
                filiere = new Filiere { Code = filiereData.Code, Libelle = filiereData.Libelle };
                context.Filieres.Add(filiere);
            }

            filiere.Libelle = filiereData.Libelle;
            filiere.EstActive = true;
            filieres[filiereData.Code] = filiere;
        }

        await context.SaveChangesAsync(cancellationToken);

        var specialites = new Dictionary<string, Specialite>(StringComparer.OrdinalIgnoreCase);
        foreach (var filiereData in data)
        {
            var filiere = filieres[filiereData.Code];

            foreach (var specialiteData in filiereData.Specialites)
            {
                var specialite = await context.Specialites
                    .FirstOrDefaultAsync(x => x.Code == specialiteData.Code, cancellationToken);

                if (specialite is null)
                {
                    specialite = new Specialite
                    {
                        FiliereId = filiere.Id,
                        Code = specialiteData.Code,
                        Libelle = specialiteData.Libelle
                    };
                    context.Specialites.Add(specialite);
                }

                specialite.FiliereId = filiere.Id;
                specialite.Libelle = specialiteData.Libelle;
                specialite.EstActive = true;
                specialites[specialiteData.Code] = specialite;
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        return specialites;
    }

    private static async Task<ParcoursAcademique> EnsureParcoursAsync(
        RiisAcademicDbContext context,
        ProgrammeData data,
        AnneeAcademique annee,
        CycleFormation cycle,
        NiveauEtude niveau,
        long filiereId,
        long specialiteId,
        CancellationToken cancellationToken)
    {
        var parcours = await context.ParcoursAcademiques
            .FirstOrDefaultAsync(x => x.Code == data.ParcoursCode, cancellationToken);

        if (parcours is null)
        {
            parcours = new ParcoursAcademique { Code = data.ParcoursCode, Libelle = data.ParcoursLibelle };
            context.ParcoursAcademiques.Add(parcours);
        }

        parcours.AnneeAcademiqueId = annee.Id;
        parcours.CycleFormationId = cycle.Id;
        parcours.NiveauEtudeId = niveau.Id;
        parcours.FiliereId = filiereId;
        parcours.SpecialiteId = specialiteId;
        parcours.Libelle = data.ParcoursLibelle;
        parcours.EstActive = true;

        await context.SaveChangesAsync(cancellationToken);
        return parcours;
    }

    private static async Task<MaquettePedagogique> EnsureMaquetteAsync(
        RiisAcademicDbContext context,
        ProgrammeData data,
        AnneeAcademique annee,
        CancellationToken cancellationToken)
    {
        var maquette = await context.MaquettesPedagogiques
            .FirstOrDefaultAsync(
                x => x.Code == data.MaquetteCode && x.Version == data.Version,
                cancellationToken);

        if (maquette is null)
        {
            maquette = new MaquettePedagogique
            {
                Code = data.MaquetteCode,
                Libelle = data.MaquetteLibelle,
                Version = data.Version
            };
            context.MaquettesPedagogiques.Add(maquette);
        }

        maquette.Libelle = data.MaquetteLibelle;
        maquette.Statut = StatutMaquettePedagogique.Active;
        maquette.DateDebutValidite = new DateOnly(annee.AnneeDebut, 9, 1);
        maquette.DateFinValidite = new DateOnly(annee.AnneeFin, 8, 1);
        maquette.SourceDocument = data.SourceDocument;
        maquette.ParcoursAcademiqueId = null;

        await context.SaveChangesAsync(cancellationToken);
        return maquette;
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

        var autresAffectations = await context.AffectationsMaquettes
            .Where(x => x.ParcoursAcademiqueId == parcours.Id && x.MaquettePedagogiqueId != maquette.Id)
            .ToListAsync(cancellationToken);

        foreach (var autre in autresAffectations.Where(x => x.EstPrincipale && x.EstActive))
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
        affectation.Observation = "Affectation créée par import du programme officiel.";

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task<SemestrePedagogique> EnsureSemestreAsync(
        RiisAcademicDbContext context,
        MaquettePedagogique maquette,
        SemestreData data,
        long niveauEtudeId,
        CancellationToken cancellationToken)
    {
        var semestre = await context.SemestresPedagogiques
            .FirstOrDefaultAsync(
                x => x.MaquettePedagogiqueId == maquette.Id && x.NumeroSemestre == data.Numero,
                cancellationToken);

        if (semestre is null)
        {
            semestre = new SemestrePedagogique
            {
                MaquettePedagogiqueId = maquette.Id,
                NumeroSemestre = data.Numero,
                Libelle = data.Libelle
            };
            context.SemestresPedagogiques.Add(semestre);
        }

        semestre.Libelle = data.Libelle;
        semestre.NiveauEtudeId = niveauEtudeId;
        semestre.OrdreAffichage = ToShort(data.OrdreAffichage);

        await context.SaveChangesAsync(cancellationToken);
        return semestre;
    }

    private static async Task<UniteEnseignement> EnsureUniteAsync(
        RiisAcademicDbContext context,
        MaquettePedagogique maquette,
        UniteData data,
        CancellationToken cancellationToken)
    {
        var unite = await context.UnitesEnseignement
            .FirstOrDefaultAsync(x => x.Code == data.Code, cancellationToken);

        if (unite is null)
        {
            unite = new UniteEnseignement { Code = data.Code, Libelle = data.Libelle };
            context.UnitesEnseignement.Add(unite);
        }
        else if (unite.MaquettePedagogiqueId is not null && unite.MaquettePedagogiqueId != maquette.Id)
        {
            throw new InvalidOperationException(
                $"L'UE '{data.Code}' est déjà rattachée à une autre maquette.");
        }

        unite.MaquettePedagogiqueId = maquette.Id;
        unite.Libelle = data.Libelle;
        unite.Type = TypeElementConstitutif.CoursMagistraux;

        await context.SaveChangesAsync(cancellationToken);
        return unite;
    }

    private static async Task<ElementConstitutif> EnsureElementAsync(
        RiisAcademicDbContext context,
        UniteEnseignement unite,
        ElementData data,
        CancellationToken cancellationToken)
    {
        var element = await context.ElementsConstitutifs
            .FirstOrDefaultAsync(
                x => x.UniteEnseignementId == unite.Id && x.Code == data.Code,
                cancellationToken);

        if (element is null)
        {
            element = new ElementConstitutif
            {
                UniteEnseignementId = unite.Id,
                Code = data.Code,
                Libelle = data.Libelle
            };
            context.ElementsConstitutifs.Add(element);
        }

        element.Libelle = data.Libelle;
        element.Type = TypeElementConstitutif.CoursMagistraux;
        element.Credits = data.Credits;
        element.VolumeHoraire = ToShort(data.VolumeHoraire);

        await context.SaveChangesAsync(cancellationToken);
        return element;
    }

    private static async Task EnsureMaquetteElementAsync(
        RiisAcademicDbContext context,
        SemestrePedagogique semestre,
        ElementConstitutif element,
        ElementData data,
        CancellationToken cancellationToken)
    {
        var maquetteElement = await context.MaquetteElementsConstitutifs
            .FirstOrDefaultAsync(
                x => x.SemestrePedagogiqueId == semestre.Id
                    && x.ElementConstitutifId == element.Id,
                cancellationToken);

        if (maquetteElement is null)
        {
            maquetteElement = new MaquetteElementConstitutif
            {
                SemestrePedagogiqueId = semestre.Id,
                ElementConstitutifId = element.Id
            };
            context.MaquetteElementsConstitutifs.Add(maquetteElement);
        }

        maquetteElement.Credits = data.Credits;
        maquetteElement.VolumeHoraire = ToShort(data.VolumeHoraire);
        maquetteElement.Coefficient = 1m;
        maquetteElement.OrdreAffichage = ToShort(data.OrdreAffichage);
        maquetteElement.EstObligatoire = true;

        await context.SaveChangesAsync(cancellationToken);
    }

    private static short ToShort(int value)
    {
        if (value is < short.MinValue or > short.MaxValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                $"La valeur '{value}' dépasse la capacité d'un Int16.");
        }

        return (short)value;
    }

    private static async Task<SeedData> LoadAsync(CancellationToken cancellationToken)
    {
        var assembly = typeof(BtsOfficialProgrammeSeeder).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .SingleOrDefault(x => x.EndsWith(ResourceName, StringComparison.OrdinalIgnoreCase))
            ?? throw new FileNotFoundException($"La ressource '{ResourceName}' est introuvable.");

        await using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new FileNotFoundException($"Impossible d'ouvrir la ressource '{resourceName}'.");

        return await JsonSerializer.DeserializeAsync<SeedData>(
                   stream,
                   new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                   cancellationToken)
               ?? throw new InvalidOperationException($"La ressource '{ResourceName}' est vide.");
    }

    private sealed class SeedData
    {
        public required string SourceDocument { get; set; }
        public required string Version { get; set; }
        public required AnneeData AnneeAcademique { get; set; }
        public required CycleData Cycle { get; set; }
        public required NiveauData Niveau { get; set; }
        public required List<FiliereData> Filieres { get; set; }
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
        public required List<SpecialiteData> Specialites { get; set; }
    }

    private sealed class SpecialiteData
    {
        public required string Code { get; set; }
        public required string Libelle { get; set; }
        public required ProgrammeData Programme { get; set; }
    }

    private sealed class ProgrammeData
    {
        public required string Code { get; set; }
        public required string Libelle { get; set; }
        public required string FiliereCode { get; set; }
        public required string ParcoursCode { get; set; }
        public required string ParcoursLibelle { get; set; }
        public required string MaquetteCode { get; set; }
        public required string MaquetteLibelle { get; set; }
        public List<SemestreData> Semestres { get; set; } = [];

        public string Version { get; set; } = "2026-2027";
        public string SourceDocument { get; set; } = "PROGRAMME DES SPÉCIALITÉS EN BTS1.docx";

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
        public required string Libelle { get; set; }
        public int OrdreAffichage { get; set; }
        public decimal Credits { get; set; }
        public int VolumeHoraire { get; set; }
        public List<ElementData> ElementsConstitutifs { get; set; } = [];
    }

    private sealed class ElementData
    {
        public required string Code { get; set; }
        public required string Libelle { get; set; }
        public decimal Credits { get; set; }
        public int VolumeHoraire { get; set; }
        public int OrdreAffichage { get; set; }
    }
}
