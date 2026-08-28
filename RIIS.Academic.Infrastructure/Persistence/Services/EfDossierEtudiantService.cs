using Microsoft.EntityFrameworkCore;
using RIIS.Academic.Application.Dossiers.Dtos;
using RIIS.Academic.Application.Dossiers.Services;
using RIIS.Academic.Application.Etudiants.Dtos;
using RIIS.Academic.Domain;

namespace RIIS.Academic.Infrastructure.Persistence.Services;

public class EfDossierEtudiantService(RiisAcademicDbContext context) : IDossierEtudiantService
{
    public async Task<List<DossierEtudiantAnnuelDto>> GetDossiersEtudiantAnnuelsAsync(
        long? parcoursAcademiqueId = null,
        CancellationToken cancellationToken = default)
    {
        var keys = await context.DossiersAcademiques
            .AsNoTracking()
            .Where(x => parcoursAcademiqueId == null || x.ParcoursAcademiqueId == parcoursAcademiqueId.Value)
            .Where(x => context.DossiersAdministratifs.Any(a =>
                a.EtudiantId == x.EtudiantId
                && a.ParcoursAcademiqueId == x.ParcoursAcademiqueId))
            .Select(x => new { x.EtudiantId, x.ParcoursAcademiqueId })
            .ToListAsync(cancellationToken);

        var dossiers = new List<DossierEtudiantAnnuelDto>();
        foreach (var key in keys)
        {
            dossiers.Add(await BuildDossierEtudiantAnnuelAsync(key.EtudiantId, key.ParcoursAcademiqueId, cancellationToken));
        }

        return dossiers
            .OrderByDescending(GetDateCreation)
            .ThenBy(x => x.Etudiant?.NomComplet)
            .ToList();
    }

    public async Task<DossierEtudiantAnnuelDto?> GetDossierEtudiantAnnuelAsync(
        long etudiantId,
        long parcoursAcademiqueId,
        CancellationToken cancellationToken = default)
    {
        var etudiant = await context.Etudiants
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == etudiantId, cancellationToken);

        if (etudiant is null)
        {
            return null;
        }

        return await BuildDossierEtudiantAnnuelAsync(etudiantId, parcoursAcademiqueId, cancellationToken);
    }

    public async Task<DossierEtudiantAnnuelDto> GetOrCreateDossierEtudiantAnnuelAsync(
        long etudiantId,
        long parcoursAcademiqueId,
        long? classePedagogiqueId = null,
        CancellationToken cancellationToken = default)
    {
        var existing = await BuildDossierEtudiantAnnuelAsync(etudiantId, parcoursAcademiqueId, cancellationToken);
        if (existing.EstComplet)
        {
            return existing;
        }

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        var etudiant = await context.Etudiants
            .FirstOrDefaultAsync(x => x.Id == etudiantId, cancellationToken)
            ?? throw new InvalidOperationException("L'etudiant selectionne est introuvable.");

        var parcours = await context.ParcoursAcademiques
            .Include(x => x.AnneeAcademique)
            .FirstOrDefaultAsync(x => x.Id == parcoursAcademiqueId, cancellationToken)
            ?? throw new InvalidOperationException("Le parcours academique selectionne est introuvable.");

        if (!parcours.EstActive)
        {
            throw new InvalidOperationException("Le parcours academique selectionne est inactif.");
        }

        if (existing.Academique is null)
        {
            await EnsureNoDossierForSameAcademicYearAsync(etudiant.Id, parcours, cancellationToken);
        }

        ClassePedagogique? classe = null;
        if (classePedagogiqueId is not null)
        {
            classe = await context.ClassesPedagogiques
                .FirstOrDefaultAsync(x => x.Id == classePedagogiqueId.Value, cancellationToken)
                ?? throw new InvalidOperationException("La classe pedagogique selectionnee est introuvable.");

            if (classe.ParcoursAcademiqueId != parcours.Id)
            {
                throw new InvalidOperationException("La classe pedagogique selectionnee n'appartient pas au parcours choisi.");
            }
        }

        var now = DateTime.UtcNow;
        if (existing.Administratif is null)
        {
            context.DossiersAdministratifs.Add(new DossierAdministratif
            {
                EtudiantId = etudiant.Id,
                ParcoursAcademiqueId = parcours.Id,
                Statut = StatutDossierAdministratif.EnPreparation,
                CreeLeUtc = now
            });
        }

        if (existing.Academique is null)
        {
            context.DossiersAcademiques.Add(new DossierAcademique
            {
                EtudiantId = etudiant.Id,
                ParcoursAcademiqueId = parcours.Id,
                ClassePedagogiqueId = classe?.Id,
                Statut = StatutDossierAcademique.EnCours,
                CreeLeUtc = now
            });
        }

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await BuildDossierEtudiantAnnuelAsync(etudiant.Id, parcours.Id, cancellationToken);
    }

    private async Task GenererElementsFinanciersAsync(
        DossierFinancier dossierFinancier,
        long parcoursAcademiqueId,
        DateTime now,
        DateOnly dateExigibilite,
        CancellationToken cancellationToken)
    {
        var tarifs = await context.TarifsScolarite
            .Where(x => x.EstActif && x.ParcoursAcademiqueId == parcoursAcademiqueId)
            .ToListAsync(cancellationToken);
        var types = await context.TypesElementsScolarite
            .Where(x => x.EstActif && (x.EstDocumentaire || x.EstPayable))
            .OrderBy(x => x.OrdreAffichage)
            .ThenBy(x => x.Libelle)
            .ToListAsync(cancellationToken);

        GenererElementsFinanciers(
            dossierFinancier,
            types,
            tarifs,
            now,
            dateExigibilite,
            [],
            new Dictionary<long, EcheanceScolarite>());
    }

    private void GenererElementsFinanciers(
        DossierFinancier dossierFinancier,
        IEnumerable<TypeElementScolarite> types,
        IEnumerable<TarifScolarite> tarifs,
        DateTime now,
        DateOnly dateExigibilite,
        ICollection<ElementScolariteEtudiant> elements,
        IDictionary<long, EcheanceScolarite> echeancesByType)
    {
        foreach (var type in types)
        {
            var tarif = ResolveTarif(tarifs, type.Id);
            if (type.EstPayable && tarif is null)
            {
                continue;
            }

            var montantAttendu = tarif?.Montant ?? 0;
            var element = new ElementScolariteEtudiant
            {
                DossierFinancier = dossierFinancier,
                TypeElementScolariteId = type.Id,
                Code = type.Code,
                Libelle = type.Libelle,
                MontantAttendu = montantAttendu,
                MontantAffecte = 0,
                MontantRestant = montantAttendu,
                Statut = StatutElementScolarite.NonDemarre,
                DateCreationUtc = now
            };

            elements.Add(element);
            context.ElementsScolariteEtudiants.Add(element);

            if (type.EstPayable && montantAttendu > 0)
            {
                var echeance = new EcheanceScolarite
                {
                    ElementScolariteEtudiant = element,
                    Numero = 1,
                    Libelle = "Echeance unique",
                    DateExigibilite = dateExigibilite,
                    MontantAttendu = montantAttendu,
                    MontantAffecte = 0,
                    MontantRestant = montantAttendu,
                    Statut = StatutEcheanceScolarite.NonExigible
                };

                echeancesByType[type.Id] = echeance;
                context.EcheancesScolarite.Add(echeance);
            }
        }
    }

    private async Task EnsureNoDossierForSameAcademicYearAsync(
        long etudiantId,
        ParcoursAcademique parcours,
        CancellationToken cancellationToken)
    {
        var existing = await context.DossiersAcademiques
            .AsNoTracking()
            .Include(x => x.ParcoursAcademique)
            .ThenInclude(x => x.AnneeAcademique)
            .FirstOrDefaultAsync(
                x => x.EtudiantId == etudiantId
                    && x.ParcoursAcademique.AnneeAcademiqueId == parcours.AnneeAcademiqueId,
                cancellationToken);

        if (existing is not null)
        {
            var annee = existing.ParcoursAcademique.AnneeAcademique?.Libelle ?? "cette annee academique";
            throw new InvalidOperationException(
                $"Cet etudiant a deja un parcours pour {annee}. Un etudiant ne peut pas avoir deux parcours la meme annee.");
        }
    }

    private async Task EnsureNoExistingDossierForParcoursAsync(
        long etudiantId,
        long parcoursAcademiqueId,
        CancellationToken cancellationToken)
    {
        var hasAdministratif = await context.DossiersAdministratifs
            .AsNoTracking()
            .AnyAsync(
                x => x.EtudiantId == etudiantId && x.ParcoursAcademiqueId == parcoursAcademiqueId,
                cancellationToken);
        var hasAcademique = await context.DossiersAcademiques
            .AsNoTracking()
            .AnyAsync(
                x => x.EtudiantId == etudiantId && x.ParcoursAcademiqueId == parcoursAcademiqueId,
                cancellationToken);
        var hasFinancier = await context.DossiersFinanciers
            .AsNoTracking()
            .AnyAsync(
                x => x.EtudiantId == etudiantId && x.ParcoursAcademiqueId == parcoursAcademiqueId,
                cancellationToken);

        if (hasAdministratif || hasAcademique || hasFinancier)
        {
            throw new InvalidOperationException("Un dossier existe deja pour cet etudiant et ce parcours.");
        }
    }

    private async Task<DossierEtudiantAnnuelDto> BuildDossierEtudiantAnnuelAsync(
        long etudiantId,
        long parcoursAcademiqueId,
        CancellationToken cancellationToken)
    {
        var etudiant = await context.Etudiants
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == etudiantId, cancellationToken);
        var parcours = await context.ParcoursAcademiques
            .AsNoTracking()
            .Include(x => x.AnneeAcademique)
            .FirstOrDefaultAsync(x => x.Id == parcoursAcademiqueId, cancellationToken);
        var administratif = await context.DossiersAdministratifs
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.EtudiantId == etudiantId && x.ParcoursAcademiqueId == parcoursAcademiqueId, cancellationToken);
        var academique = await context.DossiersAcademiques
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.EtudiantId == etudiantId && x.ParcoursAcademiqueId == parcoursAcademiqueId, cancellationToken);
        var financier = await context.DossiersFinanciers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.EtudiantId == etudiantId && x.ParcoursAcademiqueId == parcoursAcademiqueId, cancellationToken);
        var classe = academique?.ClassePedagogiqueId is null
            ? null
            : await context.ClassesPedagogiques
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == academique.ClassePedagogiqueId.Value, cancellationToken);

        return new DossierEtudiantAnnuelDto
        {
            EtudiantId = etudiantId,
            ParcoursAcademiqueId = parcoursAcademiqueId,
            Etudiant = etudiant is null ? null : ToEtudiantDto(etudiant),
            Administratif = administratif is null
                ? null
                : DossierAdministratifDto.FromEntity(administratif, etudiant, parcours, parcours?.AnneeAcademique),
            Academique = academique is null
                ? null
                : DossierAcademiqueDto.FromEntity(academique, etudiant, parcours, parcours?.AnneeAcademique, classe),
            Financier = financier is null
                ? null
                : DossierFinancierDto.FromEntity(financier, etudiant, parcours, parcours?.AnneeAcademique)
        };
    }

    private static TarifScolarite? ResolveTarif(IEnumerable<TarifScolarite> tarifs, long typeElementScolariteId)
        => tarifs
            .Where(x => x.TypeElementScolariteId == typeElementScolariteId)
            .OrderByDescending(x => x.Priorite)
            .ThenByDescending(x => x.Id)
            .FirstOrDefault();

    private static DateTime GetDateCreation(DossierEtudiantAnnuelDto dossier)
        => dossier.Academique?.CreeLeUtc
            ?? dossier.Administratif?.CreeLeUtc
            ?? dossier.Financier?.CreeLeUtc
            ?? DateTime.MinValue;

    private static EtudiantDto ToEtudiantDto(Etudiant etudiant)
        => new()
        {
            Id = etudiant.Id,
            Matricule = etudiant.Matricule,
            Nom = etudiant.Nom,
            Prenoms = etudiant.Prenoms,
            DateNaissance = etudiant.DateNaissance,
            LieuNaissance = etudiant.LieuNaissance,
            Sexe = etudiant.Sexe,
            AptitudeMedicale = etudiant.AptitudeMedicale,
            Nationalite = etudiant.Nationalite,
            RegionOrigine = etudiant.RegionOrigine,
            TelephonePrincipal = etudiant.TelephonePrincipal,
            TelephoneSecondaire = etudiant.TelephoneSecondaire,
            Email = etudiant.Email,
            NomPere = etudiant.NomPere,
            NomMere = etudiant.NomMere,
            LieuResidence = etudiant.LieuResidence,
            PhotoUrl = etudiant.PhotoUrl
        };

}
