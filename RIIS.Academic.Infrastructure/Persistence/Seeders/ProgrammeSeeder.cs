using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RIIS.Academic.Domain;

namespace RIIS.Academic.Infrastructure.Persistence.Seeders;

public static class ProgrammeSeeder
{
    private const string BtsProgrammeResourceName = "programme-bts1.json";

    public static async Task SeedBtsUeEcAsync(
        RiisAcademicDbContext context,
        string version = "2025-2026",
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        await DataSeeder.SeedAsync(context, cancellationToken);

        var lignes = await LoadBtsProgrammeAsync(cancellationToken);

        foreach (var groupeSpecialite in lignes.GroupBy(x => x.SpecialiteCode))
        {
            var parcoursAcademique = await context.ParcoursAcademiques
                .Include(x => x.CycleFormation)
                .Include(x => x.Specialite)
                .FirstOrDefaultAsync(
                    x => x.CycleFormation.Code == "BTS"
                        && x.Specialite != null
                        && x.Specialite.Code == groupeSpecialite.Key,
                    cancellationToken)
                ?? throw new InvalidOperationException(
                    $"Aucun rattachement BTS trouvé pour la spécialité '{groupeSpecialite.Key}'.");

            var specialiteLibelle = groupeSpecialite.First().SpecialiteLibelle;
            var maquette = await EnsureMaquetteAsync(
                context,
                parcoursAcademique,
                specialiteLibelle,
                version,
                cancellationToken);

            foreach (var groupeSemestre in groupeSpecialite.GroupBy(x => x.SemestreNumero).OrderBy(x => x.Key))
            {
                var semestre = await EnsureSemestreAsync(
                    context,
                    maquette,
                    groupeSemestre.Key,
                    groupeSemestre,
                    cancellationToken);

                foreach (var groupeUe in groupeSemestre.GroupBy(x => x.UeCode).OrderBy(x => x.First().UeOrdreAffichage))
                {
                    var uniteEnseignement = await EnsureUniteEnseignementAsync(
                        context,
                        semestre,
                        groupeUe,
                        cancellationToken);

                    await EnsureElementsConstitutifsAsync(
                        context,
                        uniteEnseignement,
                        groupeUe,
                        cancellationToken);
                }
            }
        }
    }

    private static async Task<MaquettePedagogique> EnsureMaquetteAsync(
        RiisAcademicDbContext context,
        ParcoursAcademique parcoursAcademique,
        string specialiteLibelle,
        string version,
        CancellationToken cancellationToken)
    {
        var code = $"MAQ-BTS-{parcoursAcademique.Specialite?.Code}";

        var maquette = await context.MaquettesPedagogiques
            .FirstOrDefaultAsync(
                x => x.CycleFormationId == parcoursAcademique.CycleFormationId
                    && x.NiveauEtudeId == parcoursAcademique.NiveauEtudeId
                    && x.FiliereId == parcoursAcademique.FiliereId
                    && x.SpecialiteId == parcoursAcademique.SpecialiteId
                    && x.Code == code
                    && x.Version == version,
                cancellationToken);

        if (maquette is not null)
        {
            maquette.Libelle = $"Programme BTS - {specialiteLibelle}";
            maquette.Statut = StatutMaquettePedagogique.Active;
            maquette.SourceDocument = "PROGRAMME DES SPÉCIALITÉS EN BTS1.docx";

            parcoursAcademique.MaquettePedagogiqueId = maquette.Id;
            parcoursAcademique.MaquettePedagogiqueId = maquette.Id;
            await context.SaveChangesAsync(cancellationToken);
            return maquette;
        }

        maquette = await context.MaquettesPedagogiques
            .FirstOrDefaultAsync(
                x => x.CycleFormationId == parcoursAcademique.CycleFormationId
                    && x.NiveauEtudeId == parcoursAcademique.NiveauEtudeId
                    && x.FiliereId == parcoursAcademique.FiliereId
                    && x.SpecialiteId == parcoursAcademique.SpecialiteId
                    && x.Code == code
                    && x.Version == "2026-2027",
                cancellationToken);

        if (maquette is not null)
        {
            maquette.Libelle = $"Programme BTS - {specialiteLibelle}";
            maquette.Version = version;
            maquette.Statut = StatutMaquettePedagogique.Active;
            maquette.DateDebutValidite = new DateOnly(2025, 9, 1);
            maquette.DateFinValidite ??= new DateOnly(2026, 8, 31);
            maquette.SourceDocument = "PROGRAMME DES SPÃ‰CIALITÃ‰S EN BTS1.docx";

            await context.SaveChangesAsync(cancellationToken);
            return maquette;
        }

        maquette = new MaquettePedagogique
        {
            CycleFormationId = parcoursAcademique.CycleFormationId,
            NiveauEtudeId = parcoursAcademique.NiveauEtudeId,
            FiliereId = parcoursAcademique.FiliereId,
            SpecialiteId = parcoursAcademique.SpecialiteId,
            Code = code,
            Libelle = $"Programme BTS - {specialiteLibelle}",
            Version = version,
            Statut = StatutMaquettePedagogique.Active,
            DateDebutValidite = new DateOnly(2025, 9, 1),
            DateFinValidite = new DateOnly(2026, 8, 31),
            SourceDocument = "PROGRAMME DES SPÉCIALITÉS EN BTS1.docx",
            Observation = "Maquette générée depuis le document Word BTS1 fourni."
        };

        context.MaquettesPedagogiques.Add(maquette);
        await context.SaveChangesAsync(cancellationToken);

        parcoursAcademique.MaquettePedagogiqueId = maquette.Id;
        await context.SaveChangesAsync(cancellationToken);

        return maquette;
    }

    private static async Task<SemestrePedagogique> EnsureSemestreAsync(
        RiisAcademicDbContext context,
        MaquettePedagogique maquette,
        byte numero,
        IEnumerable<BtsProgrammeLine> lignesSemestre,
        CancellationToken cancellationToken)
    {
        var semestre = await context.SemestresPedagogiques
            .FirstOrDefaultAsync(
                x => x.MaquettePedagogiqueId == maquette.Id && x.Numero == numero,
                cancellationToken);

        var creditsAttendus = lignesSemestre
            .GroupBy(x => x.UeCode)
            .Sum(x => x.First().UeCredits);

        var volumeHoraireAttendu = lignesSemestre
            .GroupBy(x => x.UeCode)
            .Sum(x => x.First().UeVolumeHoraire);

        if (semestre is not null)
        {
            semestre.Libelle = $"Semestre {numero}";
            semestre.CreditsAttendus = creditsAttendus;
            semestre.VolumeHoraireAttendu = ToShort(volumeHoraireAttendu);
            semestre.OrdreAffichage = numero;

            await context.SaveChangesAsync(cancellationToken);
            return semestre;
        }

        semestre = new SemestrePedagogique
        {
            MaquettePedagogiqueId = maquette.Id,
            Numero = numero,
            Libelle = $"Semestre {numero}",
            CreditsAttendus = creditsAttendus,
            VolumeHoraireAttendu = ToShort(volumeHoraireAttendu),
            OrdreAffichage = numero
        };

        context.SemestresPedagogiques.Add(semestre);
        await context.SaveChangesAsync(cancellationToken);

        return semestre;
    }

    private static async Task<UniteEnseignement> EnsureUniteEnseignementAsync(
        RiisAcademicDbContext context,
        SemestrePedagogique semestre,
        IGrouping<string, BtsProgrammeLine> groupeUe,
        CancellationToken cancellationToken)
    {
        var premiereLigne = groupeUe.First();

        var uniteEnseignement = await context.UnitesEnseignement
            .FirstOrDefaultAsync(
                x => x.SemestrePedagogiqueId == semestre.Id && x.Code == premiereLigne.UeCode,
                cancellationToken);

        if (uniteEnseignement is not null)
        {
            uniteEnseignement.Libelle = premiereLigne.UeLibelle;
            uniteEnseignement.Credits = premiereLigne.UeCredits;
            uniteEnseignement.VolumeHoraire = ToShort(premiereLigne.UeVolumeHoraire);
            uniteEnseignement.OrdreAffichage = ToShort(premiereLigne.UeOrdreAffichage);
            uniteEnseignement.EstObligatoire = true;

            await context.SaveChangesAsync(cancellationToken);
            return uniteEnseignement;
        }

        uniteEnseignement = new UniteEnseignement
        {
            SemestrePedagogiqueId = semestre.Id,
            Code = premiereLigne.UeCode,
            Libelle = premiereLigne.UeLibelle,
            Credits = premiereLigne.UeCredits,
            VolumeHoraire = ToShort(premiereLigne.UeVolumeHoraire),
            OrdreAffichage = ToShort(premiereLigne.UeOrdreAffichage),
            EstObligatoire = true
        };

        context.UnitesEnseignement.Add(uniteEnseignement);
        await context.SaveChangesAsync(cancellationToken);

        return uniteEnseignement;
    }

    private static async Task EnsureElementsConstitutifsAsync(
        RiisAcademicDbContext context,
        UniteEnseignement uniteEnseignement,
        IEnumerable<BtsProgrammeLine> lignesUe,
        CancellationToken cancellationToken)
    {
        foreach (var ligne in lignesUe.OrderBy(x => x.EcOrdreAffichage))
        {
            var ordreAffichage = ToShort(ligne.EcOrdreAffichage);

            var elementConstitutif = await context.ElementsConstitutifs
                .FirstOrDefaultAsync(
                    x => x.UniteEnseignementId == uniteEnseignement.Id
                        && x.OrdreAffichage == ordreAffichage,
                    cancellationToken);

            if (elementConstitutif is not null)
            {
                elementConstitutif.Libelle = ligne.EcLibelle;
                elementConstitutif.Type = TypeElementConstitutif.Cours;
                elementConstitutif.Credits = ligne.EcCredits;
                elementConstitutif.Coefficient = 1m;
                elementConstitutif.VolumeHoraire = ToShort(ligne.EcVolumeHoraire);
                elementConstitutif.EstObligatoire = true;

                continue;
            }

            context.ElementsConstitutifs.Add(new ElementConstitutif
            {
                UniteEnseignementId = uniteEnseignement.Id,
                Code = null,
                Libelle = ligne.EcLibelle,
                Type = TypeElementConstitutif.Cours,
                Credits = ligne.EcCredits,
                Coefficient = 1m,
                VolumeHoraire = ToShort(ligne.EcVolumeHoraire),
                OrdreAffichage = ordreAffichage,
                EstObligatoire = true
            });
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task<IReadOnlyList<BtsProgrammeLine>> LoadBtsProgrammeAsync(
        CancellationToken cancellationToken)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly
            .GetManifestResourceNames()
            .SingleOrDefault(x => x.EndsWith(BtsProgrammeResourceName, StringComparison.OrdinalIgnoreCase))
            ?? throw new FileNotFoundException(
                $"La ressource embarquée '{BtsProgrammeResourceName}' est introuvable.");

        await using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new FileNotFoundException(
                $"Impossible d'ouvrir la ressource embarquée '{resourceName}'.");

        var lignes = await JsonSerializer.DeserializeAsync<List<BtsProgrammeLine>>(
            stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
            cancellationToken);

        return lignes ?? [];
    }

    private static short ToShort(int value)
    {
        if (value is < short.MinValue or > short.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "La valeur dépasse la capacité d'un short.");
        }

        return (short)value;
    }

    private class BtsProgrammeLine
    {
        public required string SpecialiteCode { get; set; }
        public required string SpecialiteLibelle { get; set; }
        public byte SemestreNumero { get; set; }
        public required string UeCode { get; set; }
        public required string UeLibelle { get; set; }
        public decimal UeCredits { get; set; }
        public int UeVolumeHoraire { get; set; }
        public int UeOrdreAffichage { get; set; }
        public required string EcLibelle { get; set; }
        public decimal EcCredits { get; set; }
        public int EcVolumeHoraire { get; set; }
        public int EcOrdreAffichage { get; set; }
    }
}
