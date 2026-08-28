using System.Globalization;
using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RIIS.Academic.Domain;

namespace RIIS.Academic.Infrastructure.Persistence.Seeders;

public static class OfficialProgrammeSeeder
{
    private const string ResourceName = "official-programmes-2025-2026.json";

    public static Task SeedOfficialProgrammes2025_2026Async(
        RiisAcademicDbContext context,
        CancellationToken cancellationToken = default)
        => SeedOfficialProgrammesAsync(context, cancellationToken);

    public static async Task SeedOfficialProgrammesAsync(
        RiisAcademicDbContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        await DataSeeder.SeedAsync(context, cancellationToken);

        var data = await LoadSeedDataAsync(cancellationToken);
        var dateDebutValidite = DateOnly.Parse(data.DateDebutValidite, CultureInfo.InvariantCulture);
        var dateFinValidite = DateOnly.Parse(data.DateFinValidite, CultureInfo.InvariantCulture);

        foreach (var programme in data.Programmes)
        {
            var parcoursAcademique = await ResolveParcoursAcademiqueAsync(
                context,
                programme,
                cancellationToken);

            var maquette = await EnsureMaquetteAsync(
                context,
                parcoursAcademique,
                programme,
                data.Version,
                dateDebutValidite,
                dateFinValidite,
                cancellationToken);

            foreach (var semestreData in programme.Semestres.OrderBy(x => x.Numero))
            {
                var semestre = await EnsureSemestreAsync(
                    context,
                    maquette,
                    semestreData,
                    cancellationToken);

                foreach (var uniteData in semestreData.UnitesEnseignement.OrderBy(x => x.OrdreAffichage))
                {
                    var uniteEnseignement = await EnsureUniteEnseignementAsync(
                        context,
                        semestre,
                        uniteData,
                        cancellationToken);

                    await EnsureElementsConstitutifsAsync(
                        context,
                        uniteEnseignement,
                        uniteData.ElementsConstitutifs,
                        cancellationToken);
                }
            }
        }
    }

    private static async Task<ParcoursAcademique> ResolveParcoursAcademiqueAsync(
        RiisAcademicDbContext context,
        OfficialProgrammeData programme,
        CancellationToken cancellationToken)
    {
        var parcoursAcademique = await context.ParcoursAcademiques
            .Include(x => x.CycleFormation)
            .Include(x => x.Filiere)
            .Include(x => x.Specialite)
            .FirstOrDefaultAsync(x => x.Code == programme.ParcoursAcademiqueCode, cancellationToken);

        if (parcoursAcademique is null)
        {
            throw new InvalidOperationException(
                $"Le parcours '{programme.ParcoursAcademiqueCode}' est introuvable. Exécutez d'abord le seeder des référentiels.");
        }

        if (!string.Equals(parcoursAcademique.CycleFormation.Code, programme.CycleCode, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Le parcours '{programme.ParcoursAcademiqueCode}' n'appartient pas au cycle attendu '{programme.CycleCode}'.");
        }

        if (!string.IsNullOrWhiteSpace(programme.SpecialiteCode)
            && !string.Equals(parcoursAcademique.Specialite?.Code, programme.SpecialiteCode, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Le parcours '{programme.ParcoursAcademiqueCode}' n'est pas rattaché à la spécialité attendue '{programme.SpecialiteCode}'.");
        }

        return parcoursAcademique;
    }

    private static async Task<MaquettePedagogique> EnsureMaquetteAsync(
        RiisAcademicDbContext context,
        ParcoursAcademique parcoursAcademique,
        OfficialProgrammeData programme,
        string version,
        DateOnly dateDebutValidite,
        DateOnly dateFinValidite,
        CancellationToken cancellationToken)
    {
        var maquette = await context.MaquettesPedagogiques
            .FirstOrDefaultAsync(
                x => x.CycleFormationId == parcoursAcademique.CycleFormationId
                    && x.NiveauEtudeId == parcoursAcademique.NiveauEtudeId
                    && x.FiliereId == parcoursAcademique.FiliereId
                    && x.SpecialiteId == parcoursAcademique.SpecialiteId
                    && x.Code == programme.MaquetteCode
                    && x.Version == version,
                cancellationToken);

        if (maquette is null)
        {
            maquette = new MaquettePedagogique
            {
                CycleFormationId = parcoursAcademique.CycleFormationId,
                NiveauEtudeId = parcoursAcademique.NiveauEtudeId,
                FiliereId = parcoursAcademique.FiliereId,
                SpecialiteId = parcoursAcademique.SpecialiteId,
                Code = programme.MaquetteCode,
                Libelle = programme.MaquetteLibelle,
                Version = version,
                Statut = StatutMaquettePedagogique.Active,
                DateDebutValidite = dateDebutValidite,
                DateFinValidite = dateFinValidite,
                SourceDocument = programme.SourceDocument,
                Observation = programme.Observation
            };

            context.MaquettesPedagogiques.Add(maquette);
        }
        else
        {
            maquette.Libelle = programme.MaquetteLibelle;
            maquette.Statut = StatutMaquettePedagogique.Active;
            maquette.DateDebutValidite = dateDebutValidite;
            maquette.DateFinValidite = dateFinValidite;
            maquette.SourceDocument = programme.SourceDocument;
            maquette.Observation = programme.Observation;
        }

        await context.SaveChangesAsync(cancellationToken);
        parcoursAcademique.MaquettePedagogiqueId = maquette.Id;
        await context.SaveChangesAsync(cancellationToken);

        return maquette;
    }

    private static async Task<SemestrePedagogique> EnsureSemestreAsync(
        RiisAcademicDbContext context,
        MaquettePedagogique maquette,
        OfficialSemestreData semestreData,
        CancellationToken cancellationToken)
    {
        var semestre = await context.SemestresPedagogiques
            .FirstOrDefaultAsync(
                x => x.MaquettePedagogiqueId == maquette.Id && x.Numero == semestreData.Numero,
                cancellationToken);

        var niveauEtudeId = await ResolveNiveauEtudeIdAsync(context, semestreData.Numero, cancellationToken);

        if (semestre is null)
        {
            semestre = new SemestrePedagogique
            {
                MaquettePedagogiqueId = maquette.Id,
                NiveauEtudeId = niveauEtudeId,
                Numero = semestreData.Numero,
                Libelle = semestreData.Libelle,
                CreditsAttendus = semestreData.CreditsAttendus,
                VolumeHoraireAttendu = ToShort(semestreData.VolumeHoraireAttendu),
                OrdreAffichage = ToShort(semestreData.OrdreAffichage)
            };

            context.SemestresPedagogiques.Add(semestre);
        }
        else
        {
            semestre.NiveauEtudeId = niveauEtudeId;
            semestre.Libelle = semestreData.Libelle;
            semestre.CreditsAttendus = semestreData.CreditsAttendus;
            semestre.VolumeHoraireAttendu = ToShort(semestreData.VolumeHoraireAttendu);
            semestre.OrdreAffichage = ToShort(semestreData.OrdreAffichage);
        }

        await context.SaveChangesAsync(cancellationToken);
        return semestre;
    }

    private static async Task<long?> ResolveNiveauEtudeIdAsync(
        RiisAcademicDbContext context,
        byte semestreNumero,
        CancellationToken cancellationToken)
    {
        var niveauNumero = ((semestreNumero - 1) / 2) + 1;

        var niveau = await context.NiveauxEtude
            .FirstOrDefaultAsync(x => x.Numero == niveauNumero, cancellationToken);

        return niveau?.Id;
    }

    private static async Task<UniteEnseignement> EnsureUniteEnseignementAsync(
        RiisAcademicDbContext context,
        SemestrePedagogique semestre,
        OfficialUniteEnseignementData uniteData,
        CancellationToken cancellationToken)
    {
        var uniteEnseignement = await context.UnitesEnseignement
            .FirstOrDefaultAsync(
                x => x.SemestrePedagogiqueId == semestre.Id && x.Code == uniteData.Code,
                cancellationToken);

        if (uniteEnseignement is null)
        {
            uniteEnseignement = new UniteEnseignement
            {
                SemestrePedagogiqueId = semestre.Id,
                Code = uniteData.Code,
                Libelle = uniteData.Libelle,
                Credits = uniteData.Credits,
                VolumeHoraire = ToShort(uniteData.VolumeHoraire),
                OrdreAffichage = ToShort(uniteData.OrdreAffichage),
                EstObligatoire = uniteData.EstObligatoire
            };

            context.UnitesEnseignement.Add(uniteEnseignement);
        }
        else
        {
            uniteEnseignement.Libelle = uniteData.Libelle;
            uniteEnseignement.Credits = uniteData.Credits;
            uniteEnseignement.VolumeHoraire = ToShort(uniteData.VolumeHoraire);
            uniteEnseignement.OrdreAffichage = ToShort(uniteData.OrdreAffichage);
            uniteEnseignement.EstObligatoire = uniteData.EstObligatoire;
        }

        await context.SaveChangesAsync(cancellationToken);
        return uniteEnseignement;
    }

    private static async Task EnsureElementsConstitutifsAsync(
        RiisAcademicDbContext context,
        UniteEnseignement uniteEnseignement,
        IReadOnlyCollection<OfficialElementConstitutifData> elementsData,
        CancellationToken cancellationToken)
    {
        var elementsExistants = await context.ElementsConstitutifs
            .Where(x => x.UniteEnseignementId == uniteEnseignement.Id)
            .ToListAsync(cancellationToken);

        foreach (var elementData in elementsData.OrderBy(x => x.OrdreAffichage))
        {
            var elementConstitutif = ResolveElementConstitutif(elementsExistants, elementData);

            if (elementConstitutif is null)
            {
                elementConstitutif = new ElementConstitutif
                {
                    UniteEnseignementId = uniteEnseignement.Id,
                    Code = NormalizeNullableCode(elementData.Code),
                    Libelle = elementData.Libelle,
                    Type = ResolveTypeElementConstitutif(elementData.Type),
                    Credits = elementData.Credits,
                    Coefficient = elementData.Coefficient,
                    VolumeHoraire = ToShort(elementData.VolumeHoraire),
                    OrdreAffichage = ToShort(elementData.OrdreAffichage),
                    EstObligatoire = elementData.EstObligatoire
                };

                context.ElementsConstitutifs.Add(elementConstitutif);
                elementsExistants.Add(elementConstitutif);
            }
            else
            {
                elementConstitutif.Code = NormalizeNullableCode(elementData.Code);
                elementConstitutif.Libelle = elementData.Libelle;
                elementConstitutif.Type = ResolveTypeElementConstitutif(elementData.Type);
                elementConstitutif.Credits = elementData.Credits;
                elementConstitutif.Coefficient = elementData.Coefficient;
                elementConstitutif.VolumeHoraire = ToShort(elementData.VolumeHoraire);
                elementConstitutif.OrdreAffichage = ToShort(elementData.OrdreAffichage);
                elementConstitutif.EstObligatoire = elementData.EstObligatoire;
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static ElementConstitutif? ResolveElementConstitutif(
        IReadOnlyCollection<ElementConstitutif> elementsExistants,
        OfficialElementConstitutifData elementData)
    {
        var code = NormalizeNullableCode(elementData.Code);

        if (code is not null)
        {
            var byCode = elementsExistants.FirstOrDefault(
                x => string.Equals(x.Code, code, StringComparison.OrdinalIgnoreCase));

            if (byCode is not null)
            {
                return byCode;
            }
        }

        return elementsExistants.FirstOrDefault(x => x.OrdreAffichage == elementData.OrdreAffichage)
            ?? elementsExistants.FirstOrDefault(x => SameText(x.Libelle, elementData.Libelle));
    }

    private static TypeElementConstitutif ResolveTypeElementConstitutif(string? value)
        => Enum.TryParse<TypeElementConstitutif>(value, ignoreCase: true, out var type)
            ? type
            : TypeElementConstitutif.Cours;

    private static string? NormalizeNullableCode(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool SameText(string left, string right)
        => string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);

    private static short ToShort(int value)
    {
        if (value is < short.MinValue or > short.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "La valeur dépasse la capacité d'un short.");
        }

        return (short)value;
    }

    private static async Task<OfficialProgrammesSeedData> LoadSeedDataAsync(
        CancellationToken cancellationToken)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly
            .GetManifestResourceNames()
            .SingleOrDefault(x => x.EndsWith(ResourceName, StringComparison.OrdinalIgnoreCase))
            ?? throw new FileNotFoundException(
                $"La ressource embarquée '{ResourceName}' est introuvable.");

        await using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new FileNotFoundException(
                $"Impossible d'ouvrir la ressource embarquée '{resourceName}'.");

        var data = await JsonSerializer.DeserializeAsync<OfficialProgrammesSeedData>(
            stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
            cancellationToken);

        return data ?? throw new InvalidOperationException(
            $"La ressource '{ResourceName}' est vide ou invalide.");
    }

    private class OfficialProgrammesSeedData
    {
        public required string Version { get; set; }
        public required string DateDebutValidite { get; set; }
        public required string DateFinValidite { get; set; }
        public required List<OfficialProgrammeData> Programmes { get; set; }
    }

    private class OfficialProgrammeData
    {
        public required string ParcoursAcademiqueCode { get; set; }
        public required string CycleCode { get; set; }
        public string? SpecialiteCode { get; set; }
        public required string MaquetteCode { get; set; }
        public required string MaquetteLibelle { get; set; }
        public string? SourceDocument { get; set; }
        public string? Observation { get; set; }
        public required List<OfficialSemestreData> Semestres { get; set; }
    }

    private class OfficialSemestreData
    {
        public byte Numero { get; set; }
        public required string Libelle { get; set; }
        public decimal CreditsAttendus { get; set; }
        public int VolumeHoraireAttendu { get; set; }
        public int OrdreAffichage { get; set; }
        public required List<OfficialUniteEnseignementData> UnitesEnseignement { get; set; }
    }

    private class OfficialUniteEnseignementData
    {
        public required string Code { get; set; }
        public required string Libelle { get; set; }
        public decimal Credits { get; set; }
        public int VolumeHoraire { get; set; }
        public int OrdreAffichage { get; set; }
        public bool EstObligatoire { get; set; } = true;
        public required List<OfficialElementConstitutifData> ElementsConstitutifs { get; set; }
    }

    private class OfficialElementConstitutifData
    {
        public string? Code { get; set; }
        public required string Libelle { get; set; }
        public string? Type { get; set; }
        public decimal Credits { get; set; }
        public decimal Coefficient { get; set; } = 1m;
        public int VolumeHoraire { get; set; }
        public int OrdreAffichage { get; set; }
        public bool EstObligatoire { get; set; } = true;
    }
}
