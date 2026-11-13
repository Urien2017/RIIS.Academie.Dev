using Microsoft.EntityFrameworkCore;
using RIIS.Academic.Application.Dossiers.Dtos;
using RIIS.Academic.Application.Dossiers.Services;
using RIIS.Academic.Application.Etudiants.Dtos;
using RIIS.Academic.Application.Etudiants.Services;
using RIIS.Academic.Domain;

namespace RIIS.Academic.Infrastructure.Persistence.Services;

public class EfDossierEtudiantService(
    RiisAcademicDbContext context,
    IMatriculeGenerator matriculeGenerator) : IDossierEtudiantService
{
    private readonly SemaphoreSlim operationGate = new(1, 1);

    public async Task<List<DossierEtudiantAnnuelDto>> GetDossiersEtudiantAnnuelsAsync(
        long? parcoursAcademiqueId = null,
        CancellationToken cancellationToken = default)
    {
        await operationGate.WaitAsync(cancellationToken);
        try
        {
            return await GetDossiersEtudiantAnnuelsCoreAsync(parcoursAcademiqueId, cancellationToken);
        }
        finally
        {
            operationGate.Release();
        }
    }

    private async Task<List<DossierEtudiantAnnuelDto>> GetDossiersEtudiantAnnuelsCoreAsync(
        long? parcoursAcademiqueId,
        CancellationToken cancellationToken)
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
        await operationGate.WaitAsync(cancellationToken);
        try
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
        finally
        {
            operationGate.Release();
        }
    }

    public async Task<DossierEtudiantAnnuelDto> GetOrCreateDossierEtudiantAnnuelAsync(
        long etudiantId,
        long parcoursAcademiqueId,
        long? classePedagogiqueId = null,
        CancellationToken cancellationToken = default)
    {
        await operationGate.WaitAsync(cancellationToken);
        try
        {
            return await GetOrCreateDossierEtudiantAnnuelCoreAsync(
                etudiantId,
                parcoursAcademiqueId,
                classePedagogiqueId,
                cancellationToken);
        }
        finally
        {
            operationGate.Release();
        }
    }

    public async Task<DossierEtudiantAnnuelDto> ChangeParcoursAcademiqueAsync(
        long etudiantId,
        long ancienParcoursAcademiqueId,
        long nouveauParcoursAcademiqueId,
        CancellationToken cancellationToken = default)
    {
        await operationGate.WaitAsync(cancellationToken);
        try
        {
            if (ancienParcoursAcademiqueId == nouveauParcoursAcademiqueId)
            {
                return await BuildDossierEtudiantAnnuelAsync(
                    etudiantId,
                    ancienParcoursAcademiqueId,
                    cancellationToken);
            }

            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

            var dossierEtudiant = await context.DossiersEtudiants
                .FirstOrDefaultAsync(
                    x => x.EtudiantId == etudiantId
                        && x.ParcoursAcademiqueId == ancienParcoursAcademiqueId,
                    cancellationToken)
                ?? throw new InvalidOperationException("Le dossier étudiant sélectionné est introuvable.");

            var nouveauParcours = await context.ParcoursAcademiques
                .Include(x => x.AnneeAcademique)
                .FirstOrDefaultAsync(x => x.Id == nouveauParcoursAcademiqueId, cancellationToken)
                ?? throw new InvalidOperationException("Le nouveau parcours académique sélectionné est introuvable.");

            if (!nouveauParcours.EstActive)
            {
                throw new InvalidOperationException("Le nouveau parcours académique sélectionné est inactif.");
            }

            var dejaDansLeParcours = await context.DossiersEtudiants
                .AnyAsync(
                    x => x.Id != dossierEtudiant.Id
                        && x.EtudiantId == etudiantId
                        && x.ParcoursAcademiqueId == nouveauParcoursAcademiqueId,
                    cancellationToken);
            if (dejaDansLeParcours)
            {
                throw new InvalidOperationException("Un dossier existe déjà pour cet étudiant et le nouveau parcours sélectionné.");
            }

            var autreDossierMemeAnnee = await context.DossiersEtudiants
                .AnyAsync(
                    x => x.Id != dossierEtudiant.Id
                        && x.EtudiantId == etudiantId
                        && x.ParcoursAcademique.AnneeAcademiqueId == nouveauParcours.AnneeAcademiqueId,
                    cancellationToken);
            if (autreDossierMemeAnnee)
            {
                throw new InvalidOperationException("Cet étudiant possède déjà un dossier pour la nouvelle année académique.");
            }

            var administratif = await context.DossiersAdministratifs
                .FirstOrDefaultAsync(
                    x => x.DossierEtudiantId == dossierEtudiant.Id
                        || (x.EtudiantId == etudiantId && x.ParcoursAcademiqueId == ancienParcoursAcademiqueId),
                    cancellationToken);
            var academique = await context.DossiersAcademiques
                .FirstOrDefaultAsync(
                    x => x.DossierEtudiantId == dossierEtudiant.Id
                        || (x.EtudiantId == etudiantId && x.ParcoursAcademiqueId == ancienParcoursAcademiqueId),
                    cancellationToken);
            var financier = await context.DossiersFinanciers
                .FirstOrDefaultAsync(
                    x => x.DossierEtudiantId == dossierEtudiant.Id
                        || (x.EtudiantId == etudiantId && x.ParcoursAcademiqueId == ancienParcoursAcademiqueId),
                    cancellationToken);

            if (financier is not null)
            {
                var autreFinancier = await context.DossiersFinanciers
                    .AnyAsync(
                        x => x.Id != financier.Id
                            && x.EtudiantId == etudiantId
                            && x.ParcoursAcademiqueId == nouveauParcoursAcademiqueId,
                        cancellationToken);
                if (autreFinancier)
                {
                    throw new InvalidOperationException("Un dossier financier existe déjà pour le nouveau parcours sélectionné.");
                }
            }

            var autreAdministratif = await context.DossiersAdministratifs
                .AnyAsync(
                    x => (administratif == null || x.Id != administratif.Id)
                        && x.EtudiantId == etudiantId
                        && x.ParcoursAcademiqueId == nouveauParcoursAcademiqueId,
                    cancellationToken);
            var autreAcademique = await context.DossiersAcademiques
                .AnyAsync(
                    x => (academique == null || x.Id != academique.Id)
                        && x.EtudiantId == etudiantId
                        && x.ParcoursAcademiqueId == nouveauParcoursAcademiqueId,
                    cancellationToken);
            if (autreAdministratif || autreAcademique)
            {
                throw new InvalidOperationException("Les dossiers liés existent déjà pour le nouveau parcours sélectionné.");
            }

            dossierEtudiant.ParcoursAcademiqueId = nouveauParcoursAcademiqueId;
            if (administratif is not null)
            {
                administratif.ParcoursAcademiqueId = nouveauParcoursAcademiqueId;
            }

            if (academique is not null)
            {
                academique.ParcoursAcademiqueId = nouveauParcoursAcademiqueId;
                academique.ClassePedagogiqueId = await context.ClassesPedagogiques
                    .Where(x => x.ParcoursAcademiqueId == nouveauParcoursAcademiqueId)
                    .Select(x => (long?)x.Id)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            if (financier is not null)
            {
                financier.ParcoursAcademiqueId = nouveauParcoursAcademiqueId;
                await RecalculerDossierFinancierAsync(
                    financier,
                    nouveauParcoursAcademiqueId,
                    DateTime.UtcNow,
                    cancellationToken);
            }

            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return await BuildDossierEtudiantAnnuelAsync(
                etudiantId,
                nouveauParcoursAcademiqueId,
                cancellationToken);
        }
        finally
        {
            operationGate.Release();
        }
    }

    private async Task<DossierEtudiantAnnuelDto> GetOrCreateDossierEtudiantAnnuelCoreAsync(
        long etudiantId,
        long parcoursAcademiqueId,
        long? classePedagogiqueId,
        CancellationToken cancellationToken)
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

        var dossierEtudiant = await context.DossiersEtudiants
            .FirstOrDefaultAsync(
                x => x.EtudiantId == etudiant.Id && x.ParcoursAcademiqueId == parcours.Id,
                cancellationToken);

        if (dossierEtudiant is null)
        {
            dossierEtudiant = new DossierEtudiant
            {
                EtudiantId = etudiant.Id,
                ParcoursAcademiqueId = parcours.Id,
                Matricule = await matriculeGenerator.GenerateMatriculeAsync(
                    parcours.AnneeAcademiqueId,
                    cancellationToken)
            };

            context.DossiersEtudiants.Add(dossierEtudiant);
            await context.SaveChangesAsync(cancellationToken);
        }

        var administratifExistant = await context.DossiersAdministratifs
            .FirstOrDefaultAsync(
                x => x.EtudiantId == etudiant.Id && x.ParcoursAcademiqueId == parcours.Id,
                cancellationToken);
        var academiqueExistant = await context.DossiersAcademiques
            .FirstOrDefaultAsync(
                x => x.EtudiantId == etudiant.Id && x.ParcoursAcademiqueId == parcours.Id,
                cancellationToken);
        var financierExistant = await context.DossiersFinanciers
            .FirstOrDefaultAsync(
                x => x.EtudiantId == etudiant.Id && x.ParcoursAcademiqueId == parcours.Id,
                cancellationToken);

        administratifExistant?.DossierEtudiantId = dossierEtudiant.Id;
        academiqueExistant?.DossierEtudiantId = dossierEtudiant.Id;
        financierExistant?.DossierEtudiantId = dossierEtudiant.Id;

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
                DossierEtudiantId = dossierEtudiant.Id,
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
                DossierEtudiantId = dossierEtudiant.Id,
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

    private async Task RecalculerDossierFinancierAsync(
        DossierFinancier dossierFinancier,
        long parcoursAcademiqueId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var types = await context.TypesElementsScolarite
            .Where(x => x.EstActif && (x.EstDocumentaire || x.EstPayable))
            .OrderBy(x => x.OrdreAffichage)
            .ThenBy(x => x.Libelle)
            .ToListAsync(cancellationToken);
        var tarifs = await context.TarifsScolarite
            .Where(x => x.EstActif && x.ParcoursAcademiqueId == parcoursAcademiqueId)
            .ToListAsync(cancellationToken);
        var elements = await context.ElementsScolariteEtudiants
            .Where(x => x.DossierFinancierId == dossierFinancier.Id)
            .ToListAsync(cancellationToken);
        var elementsByType = elements.ToDictionary(x => x.TypeElementScolariteId);
        var typesById = types.ToDictionary(x => x.Id);

        foreach (var element in elements)
        {
            if (!typesById.TryGetValue(element.TypeElementScolariteId, out var type))
            {
                MarkElementAsNotApplicable(element, now);
                continue;
            }

            var tarif = ResolveTarif(tarifs, type.Id);
            if (type.EstPayable && tarif is null)
            {
                MarkElementAsNotApplicable(element, now);
                continue;
            }

            ApplyElementAmounts(
                element,
                type.Code,
                type.Libelle,
                tarif?.Montant ?? 0,
                now);
        }

        foreach (var type in types)
        {
            var tarif = ResolveTarif(tarifs, type.Id);
            if ((type.EstPayable && tarif is null) || elementsByType.ContainsKey(type.Id))
            {
                continue;
            }

            var element = new ElementScolariteEtudiant
            {
                DossierFinancierId = dossierFinancier.Id,
                TypeElementScolariteId = type.Id,
                Code = type.Code,
                Libelle = type.Libelle,
                MontantAttendu = tarif?.Montant ?? 0,
                MontantAffecte = 0,
                MontantRestant = tarif?.Montant ?? 0,
                Statut = tarif?.Montant > 0
                    ? StatutElementScolarite.NonDemarre
                    : StatutElementScolarite.NonApplicable,
                DateCreationUtc = now,
                DateDernierRecalculUtc = now
            };

            context.ElementsScolariteEtudiants.Add(element);
            elements.Add(element);
        }

        // Les nouvelles lignes doivent exister avant de pouvoir leur rattacher une échéance.
        await context.SaveChangesAsync(cancellationToken);

        var elementIds = elements.Select(x => x.Id).ToHashSet();
        var echeances = await context.EcheancesScolarite
            .Where(x => elementIds.Contains(x.ElementScolariteEtudiantId))
            .ToListAsync(cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.Today);

        foreach (var echeance in echeances)
        {
            var element = elements.FirstOrDefault(x => x.Id == echeance.ElementScolariteEtudiantId);
            if (element is null || element.MontantAttendu > 0)
            {
                continue;
            }

            echeance.MontantAttendu = 0;
            echeance.MontantRestant = 0;
            echeance.Statut = StatutEcheanceScolarite.Annulee;
            echeance.DateDernierRecalculUtc = now;
        }

        foreach (var element in elements.Where(x => x.MontantAttendu > 0))
        {
            var echeance = echeances
                .Where(x => x.ElementScolariteEtudiantId == element.Id)
                .OrderBy(x => x.Numero)
                .FirstOrDefault();

            if (echeance is null)
            {
                context.EcheancesScolarite.Add(new EcheanceScolarite
                {
                    ElementScolariteEtudiantId = element.Id,
                    Numero = 1,
                    Libelle = "Echeance unique",
                    DateExigibilite = today,
                    MontantAttendu = element.MontantAttendu,
                    MontantAffecte = 0,
                    MontantRestant = element.MontantAttendu,
                    Statut = StatutEcheanceScolarite.NonExigible,
                    DateDernierRecalculUtc = now
                });
                continue;
            }

            echeance.MontantAttendu = element.MontantAttendu;
            echeance.MontantRestant = Math.Max(0, echeance.MontantAttendu - echeance.MontantAffecte);
            echeance.Statut = ResolveEcheanceStatut(echeance.MontantAttendu, echeance.MontantAffecte);
            echeance.DateDernierRecalculUtc = now;
        }

        var aUnPaiementActif = await context.PaiementsScolarite
            .AnyAsync(
                x => x.DossierFinancierId == dossierFinancier.Id
                    && x.Statut != StatutPaiementScolarite.Annule,
                cancellationToken);
        dossierFinancier.StatutFinancier = aUnPaiementActif
            ? StatutDossierScolarite.EnCours
            : StatutDossierScolarite.EnPreparation;
        dossierFinancier.DateDernierRecalculUtc = now;
        await context.SaveChangesAsync(cancellationToken);
    }

    private static void ApplyElementAmounts(
        ElementScolariteEtudiant element,
        string code,
        string libelle,
        decimal montantAttendu,
        DateTime now)
    {
        element.Code = code;
        element.Libelle = libelle;
        element.MontantAttendu = montantAttendu;
        element.MontantRestant = Math.Max(0, montantAttendu - element.MontantAffecte);
        element.Statut = montantAttendu <= 0
            ? StatutElementScolarite.NonApplicable
            : element.MontantAffecte >= montantAttendu
                ? StatutElementScolarite.Solde
                : element.MontantAffecte > 0
                    ? StatutElementScolarite.Partiel
                    : StatutElementScolarite.NonDemarre;
        element.DateDernierRecalculUtc = now;
    }

    private static void MarkElementAsNotApplicable(ElementScolariteEtudiant element, DateTime now)
    {
        element.MontantAttendu = 0;
        element.MontantRestant = 0;
        element.Statut = StatutElementScolarite.NonApplicable;
        element.DateDernierRecalculUtc = now;
    }

    private static StatutEcheanceScolarite ResolveEcheanceStatut(
        decimal montantAttendu,
        decimal montantAffecte)
        => montantAttendu <= 0
            ? StatutEcheanceScolarite.Annulee
            : montantAffecte >= montantAttendu
                ? StatutEcheanceScolarite.Payee
                : montantAffecte > 0
                    ? StatutEcheanceScolarite.Partielle
                    : StatutEcheanceScolarite.NonExigible;

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
        var aUnPaiement = financier is not null && await context.PaiementsScolarite
            .AsNoTracking()
            .AnyAsync(
                x => x.DossierFinancierId == financier.Id
                    && x.Statut != StatutPaiementScolarite.Annule,
                cancellationToken);
        var dossierEtudiant = await context.DossiersEtudiants
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.EtudiantId == etudiantId && x.ParcoursAcademiqueId == parcoursAcademiqueId,
                cancellationToken);
        var classe = academique?.ClassePedagogiqueId is null
            ? null
            : await context.ClassesPedagogiques
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == academique.ClassePedagogiqueId.Value, cancellationToken);

        return new DossierEtudiantAnnuelDto
        {
            EtudiantId = etudiantId,
            ParcoursAcademiqueId = parcoursAcademiqueId,
            Dossier = dossierEtudiant is null ? null : DossierEtudiantDto.FromEntity(dossierEtudiant),
            Etudiant = etudiant is null ? null : ToEtudiantDto(etudiant),
            Administratif = administratif is null
                ? null
                : DossierAdministratifDto.FromEntity(administratif, etudiant, parcours, parcours?.AnneeAcademique, dossierEtudiant?.Matricule),
            Academique = academique is null
                ? null
                : DossierAcademiqueDto.FromEntity(academique, etudiant, parcours, parcours?.AnneeAcademique, classe, dossierEtudiant?.Matricule),
            Financier = financier is null
                ? null
                : DossierFinancierDto.FromEntity(financier, etudiant, parcours, parcours?.AnneeAcademique, dossierEtudiant?.Matricule),
            AUnPaiement = aUnPaiement
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
