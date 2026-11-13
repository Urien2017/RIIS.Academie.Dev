using Microsoft.EntityFrameworkCore;
using RIIS.Academic.Domain;

namespace RIIS.Academic.Infrastructure.Persistence.Seeders;

/// <summary>
/// Sépare une maquette officielle qui contient initialement les quatre semestres
/// en deux maquettes : N1 (S1-S2) et N2 (S3-S4).
/// Les semestres existants sont déplacés sans changer leurs identifiants afin de
/// conserver les évaluations, les notes, les résultats et les PV déjà liés.
/// </summary>
internal static class NiveauDeuxMaquetteSplitter
{
    public static async Task SeedExistingNiveauDeuxMaquettesAsync(
        RiisAcademicDbContext context,
        AnneeAcademique annee,
        long niveau2Id,
        CancellationToken cancellationToken)
    {
        var maquettes = await context.MaquettesPedagogiques
            .Where(x => x.Code.StartsWith("MAQ-BTS1-") || x.Code.StartsWith("MAQ-PREPA1-"))
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

        var parcours = await context.ParcoursAcademiques
            .ToListAsync(cancellationToken);

        var affectations = await context.AffectationsMaquettes
            .ToListAsync(cancellationToken);

        foreach (var maquetteNiveau1 in maquettes)
        {
            var maquetteNiveau2 = await EnsureNiveauDeuxMaquetteAsync(
                context,
                maquetteNiveau1,
                niveau2Id,
                cancellationToken);

            var parcoursNiveau1 = affectations
                .Where(x => x.MaquettePedagogiqueId == maquetteNiveau1.Id)
                .Select(x => parcours.FirstOrDefault(p => p.Id == x.ParcoursAcademiqueId))
                .Where(x => x is not null && x.Code.Contains("-N1-", StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var parcoursN1 in parcoursNiveau1)
            {
                var parcoursN2 = parcours.FirstOrDefault(
                    x => string.Equals(
                        x.Code,
                        parcoursN1!.Code.Replace("-N1-", "-N2-", StringComparison.OrdinalIgnoreCase),
                        StringComparison.OrdinalIgnoreCase));

                if (parcoursN2 is not null)
                {
                    await EnsureNiveauDeuxAffectationAsync(
                        context,
                        parcoursN2,
                        maquetteNiveau2,
                        annee,
                        cancellationToken);
                }
            }
        }
    }

    public static async Task<MaquettePedagogique> EnsureNiveauDeuxMaquetteAsync(
        RiisAcademicDbContext context,
        MaquettePedagogique maquetteNiveau1,
        long niveau2Id,
        CancellationToken cancellationToken)
    {
        var code = BuildNiveauDeuxCode(maquetteNiveau1.Code);
        var libelle = BuildNiveauDeuxLibelle(maquetteNiveau1.Libelle);

        var maquetteNiveau2 = await context.MaquettesPedagogiques
            .FirstOrDefaultAsync(
                x => x.Code == code && x.Version == maquetteNiveau1.Version,
                cancellationToken);

        if (maquetteNiveau2 is null)
        {
            maquetteNiveau2 = new MaquettePedagogique
            {
                Code = code,
                Libelle = libelle,
                Version = maquetteNiveau1.Version
            };
            context.MaquettesPedagogiques.Add(maquetteNiveau2);
        }

        maquetteNiveau2.Libelle = libelle;
        maquetteNiveau2.Statut = maquetteNiveau1.Statut;
        maquetteNiveau2.DateDebutValidite = maquetteNiveau1.DateDebutValidite;
        maquetteNiveau2.DateFinValidite = maquetteNiveau1.DateFinValidite;
        maquetteNiveau2.SourceDocument = maquetteNiveau1.SourceDocument;
        maquetteNiveau2.Observation = maquetteNiveau1.Observation;
        maquetteNiveau2.ParcoursAcademiqueId = null;

        await context.SaveChangesAsync(cancellationToken);
        await MoveSemestresAndUnitsAsync(
            context,
            maquetteNiveau1,
            maquetteNiveau2,
            niveau2Id,
            cancellationToken);

        return maquetteNiveau2;
    }

    private static async Task MoveSemestresAndUnitsAsync(
        RiisAcademicDbContext context,
        MaquettePedagogique maquetteNiveau1,
        MaquettePedagogique maquetteNiveau2,
        long niveau2Id,
        CancellationToken cancellationToken)
    {
        var semestresNiveau1 = await context.SemestresPedagogiques
            .Where(x => x.MaquettePedagogiqueId == maquetteNiveau1.Id && x.NumeroSemestre >= 3)
            .OrderBy(x => x.NumeroSemestre)
            .ToListAsync(cancellationToken);

        var semestresNiveau2 = await context.SemestresPedagogiques
            .Where(x => x.MaquettePedagogiqueId == maquetteNiveau2.Id)
            .ToListAsync(cancellationToken);

        foreach (var semestreNiveau1 in semestresNiveau1)
        {
            var existingNiveau2 = semestresNiveau2.FirstOrDefault(
                x => x.NumeroSemestre == semestreNiveau1.NumeroSemestre);

            if (existingNiveau2 is not null)
            {
                throw new InvalidOperationException(
                    $"La séparation de la maquette '{maquetteNiveau1.Code}' est ambiguë : " +
                    $"le semestre {semestreNiveau1.NumeroSemestre} existe déjà dans '{maquetteNiveau2.Code}'.");
            }

            semestreNiveau1.MaquettePedagogiqueId = maquetteNiveau2.Id;
            semestreNiveau1.NiveauEtudeId = niveau2Id;
            semestresNiveau2.Add(semestreNiveau1);
        }

        if (semestresNiveau1.Count == 0)
        {
            return;
        }

        var semestresNiveau1Ids = await context.SemestresPedagogiques
            .Where(x => x.MaquettePedagogiqueId == maquetteNiveau1.Id && x.NumeroSemestre <= 2)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var semestresNiveau2Ids = semestresNiveau2
            .Where(x => x.NumeroSemestre >= 3)
            .Select(x => x.Id)
            .ToList();

        var elementsNiveau2 = await context.MaquetteElementsConstitutifs
            .Where(x => semestresNiveau2Ids.Contains(x.SemestrePedagogiqueId))
            .ToListAsync(cancellationToken);

        var elementsNiveau1 = await context.MaquetteElementsConstitutifs
            .Where(x => semestresNiveau1Ids.Contains(x.SemestrePedagogiqueId))
            .ToListAsync(cancellationToken);

        var elementsConstitutifsNiveau2Ids = elementsNiveau2
            .Select(x => x.ElementConstitutifId)
            .Distinct()
            .ToList();

        var elementsConstitutifsNiveau1Ids = elementsNiveau1
            .Select(x => x.ElementConstitutifId)
            .Distinct()
            .ToHashSet();

        var unitesNiveau2Ids = await context.ElementsConstitutifs
            .Where(x => elementsConstitutifsNiveau2Ids.Contains(x.Id))
            .Select(x => x.UniteEnseignementId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var unitesUtiliseesDansLesDeuxNiveaux = await context.ElementsConstitutifs
            .Where(x => elementsConstitutifsNiveau1Ids.Contains(x.Id))
            .Where(x => unitesNiveau2Ids.Contains(x.UniteEnseignementId))
            .Select(x => x.UniteEnseignementId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (unitesUtiliseesDansLesDeuxNiveaux.Count > 0)
        {
            throw new InvalidOperationException(
                $"La séparation de la maquette '{maquetteNiveau1.Code}' est impossible : " +
                "une UE est utilisée à la fois par les semestres N1 et N2.");
        }

        var unitesNiveau2 = await context.UnitesEnseignement
            .Where(x => unitesNiveau2Ids.Contains(x.Id))
            .ToListAsync(cancellationToken);

        foreach (var unite in unitesNiveau2)
        {
            if (unite.MaquettePedagogiqueId is not null
                && unite.MaquettePedagogiqueId != maquetteNiveau1.Id
                && unite.MaquettePedagogiqueId != maquetteNiveau2.Id)
            {
                throw new InvalidOperationException(
                    $"L'UE '{unite.Code}' est déjà rattachée à une autre maquette.");
            }

            unite.MaquettePedagogiqueId = maquetteNiveau2.Id;
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureNiveauDeuxAffectationAsync(
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
        affectation.Observation = "Affectation N2 créée par séparation des semestres officiels.";

        await context.SaveChangesAsync(cancellationToken);
    }

    private static string BuildNiveauDeuxCode(string code)
    {
        if (code.Contains("BTS1", StringComparison.OrdinalIgnoreCase))
        {
            return code.Replace("BTS1", "BTS2", StringComparison.OrdinalIgnoreCase);
        }

        if (code.Contains("PREPA1", StringComparison.OrdinalIgnoreCase))
        {
            return code.Replace("PREPA1", "PREPA2", StringComparison.OrdinalIgnoreCase);
        }

        return $"{code}-N2";
    }

    private static string BuildNiveauDeuxLibelle(string libelle)
    {
        var niveauDeuxLibelle = libelle.Replace(
            "Niveau 1",
            "Niveau 2",
            StringComparison.OrdinalIgnoreCase);

        return string.Equals(niveauDeuxLibelle, libelle, StringComparison.Ordinal)
            ? $"{libelle} - Niveau 2"
            : niveauDeuxLibelle;
    }
}
