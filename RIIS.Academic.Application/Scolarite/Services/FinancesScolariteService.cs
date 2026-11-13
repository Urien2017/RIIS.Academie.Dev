using RIIS.Academic.Application.Abstractions.Persistence;
using RIIS.Academic.Application.Etudiants.Services;
using RIIS.Academic.Application.Scolarite.Dtos;
using RIIS.Academic.Domain;

namespace RIIS.Academic.Application.Scolarite.Services;

public class FinancesScolariteService(
    IRepository<DossierFinancier> dossiersFinanciers,
    IRepository<DossierEtudiant> dossiersEtudiants,
    IRepository<Etudiant> etudiants,
    IRepository<ParcoursAcademique> parcoursAcademiques,
    IRepository<TypeElementScolarite> typesElementsScolarite,
    IRepository<ElementScolariteEtudiant> elementsScolariteEtudiants,
    IRepository<EcheanceScolarite> echeancesScolarite,
    IRepository<PaiementScolarite> paiementsScolarite,
    IRepository<ModePaiementScolarite> modesPaiementScolarite,
    IRepository<AffectationPaiementEcheance> affectationsPaiementsEcheances,
    ITarifsScolariteService tarifsScolariteService,
    IMatriculeGenerator matriculeGenerator) : IFinancesScolariteService
{
    public async Task<FinanceDossierScolariteDto?> GetFinanceDossierScolariteAsync(
        long dossierScolariteId,
        CancellationToken cancellationToken = default)
    {
        var dossier = await dossiersFinanciers.GetByIdAsync(dossierScolariteId, cancellationToken);

        return dossier is null
            ? null
            : await BuildFinanceDtoAsync(dossierScolariteId, cancellationToken);
    }

    public async Task<List<PaiementTypeElementTarifOptionDto>> GetOptionsTypesElementsTarifsPaiementAsync(
        long dossierScolariteId,
        CancellationToken cancellationToken = default)
    {
        var dossier = await dossiersFinanciers.GetByIdAsync(dossierScolariteId, cancellationToken)
            ?? throw new InvalidOperationException("Le dossier financier selectionne est introuvable.");

        return await GetOptionsTypesElementsTarifsPaiementParParcoursAsync(dossier.ParcoursAcademiqueId, cancellationToken);
    }

    public async Task<List<PaiementTypeElementTarifOptionDto>> GetOptionsTypesElementsTarifsPaiementParParcoursAsync(
        long parcoursAcademiqueId,
        CancellationToken cancellationToken = default)
    {
        var types = await typesElementsScolarite.ListAsync(cancellationToken);
        var options = new List<PaiementTypeElementTarifOptionDto>();

        foreach (var type in types.Where(x => x.EstActif && x.EstPayable).OrderBy(x => x.OrdreAffichage).ThenBy(x => x.Libelle))
        {
            var tarif = await ResolveTarifForParcoursAsync(parcoursAcademiqueId, type.Id, cancellationToken);
            if (tarif is null)
            {
                continue;
            }

            options.Add(new PaiementTypeElementTarifOptionDto
            {
                TypeElementScolariteId = type.Id,
                TypeElementScolariteLibelle = FormatCodeLibelle(type.Code, type.Libelle),
                TarifScolariteId = tarif.Id,
                TarifMontant = tarif.Montant,
                Devise = tarif.Devise
            });
        }

        return options;
    }

    public async Task<FinanceDossierScolariteDto> SavePaiementLibreAsync(
        PaiementLibreDossierDto dto,
        CancellationToken cancellationToken = default)
    {
        if (dto.Montant <= 0)
        {
            throw new InvalidOperationException("Le montant du paiement doit etre superieur a zero.");
        }

        if (dto.TypeElementScolariteId <= 0)
        {
            throw new InvalidOperationException("Le type d'element concerne par le paiement est obligatoire.");
        }

        if (dto.TarifScolariteId <= 0)
        {
            throw new InvalidOperationException("Le tarif concerne par le paiement est obligatoire.");
        }

        var mode = await modesPaiementScolarite.GetByIdAsync(dto.ModePaiementScolariteId, cancellationToken)
            ?? throw new InvalidOperationException("Le mode de paiement selectionne est introuvable.");

        if (!mode.EstActif)
        {
            throw new InvalidOperationException("Le mode de paiement selectionne est inactif.");
        }

        var dossier = await GetOrCreateDossierFinancierPourPaiementAsync(dto, cancellationToken);
        await EnsureElementsFinanciersAsync(dossier, DateTime.UtcNow, DateOnly.FromDateTime(DateTime.Today), cancellationToken);

        var tarif = await ResolveTarifForDossierAsync(dossier, dto.TypeElementScolariteId, cancellationToken)
            ?? throw new InvalidOperationException("Aucun tarif actif ne correspond a ce dossier pour le type d'element selectionne.");

        if (tarif.Id != dto.TarifScolariteId)
        {
            throw new InvalidOperationException("Le tarif selectionne ne correspond plus au tarif applicable pour ce dossier.");
        }

        if (dto.Id == 0)
        {
            await paiementsScolarite.AddAsync(new PaiementScolarite
            {
                DossierFinancierId = dossier.Id,
                TypeElementScolariteId = dto.TypeElementScolariteId,
                TarifScolariteId = dto.TarifScolariteId,
                ModePaiementScolariteId = dto.ModePaiementScolariteId,
                DatePaiement = dto.DatePaiement,
                Montant = dto.Montant,
                ModePaiement = mode.Libelle,
                ReferencePaiement = NormalizeText(dto.ReferencePaiement),
                EncaissePar = NormalizeText(dto.EncaissePar),
                Observation = NormalizeText(dto.Observation),
                MontantAffecte = 0,
                MontantNonAffecte = dto.Montant,
                Statut = StatutPaiementScolarite.NonAffecte,
                DateCreationUtc = DateTime.UtcNow
            }, cancellationToken);
        }
        else
        {
            var paiement = await paiementsScolarite.GetByIdAsync(dto.Id, cancellationToken)
                ?? throw new InvalidOperationException("Le paiement selectionne est introuvable.");

            if (paiement.DossierFinancierId != dossier.Id)
            {
                throw new InvalidOperationException("Le paiement selectionne n'appartient pas au dossier financier selectionne.");
            }

            if (paiement.MontantAffecte > dto.Montant)
            {
                throw new InvalidOperationException("Le montant du paiement ne peut pas devenir inferieur au montant deja affecte.");
            }

            paiement.DatePaiement = dto.DatePaiement;
            paiement.Montant = dto.Montant;
            paiement.TypeElementScolariteId = dto.TypeElementScolariteId;
            paiement.TarifScolariteId = dto.TarifScolariteId;
            paiement.ModePaiementScolariteId = dto.ModePaiementScolariteId;
            paiement.ModePaiement = mode.Libelle;
            paiement.ReferencePaiement = NormalizeText(dto.ReferencePaiement);
            paiement.EncaissePar = NormalizeText(dto.EncaissePar);
            paiement.Observation = NormalizeText(dto.Observation);
            paiement.MontantNonAffecte = dto.Montant - paiement.MontantAffecte;
            paiement.Statut = ResolvePaiementStatut(paiement.Montant, paiement.MontantAffecte);
        }

        await paiementsScolarite.SaveChangesAsync(cancellationToken);
        return await BuildFinanceDtoAsync(dossier.Id, cancellationToken);
    }

    public async Task<FinanceDossierScolariteDto> AnnulerPaiementLibreAsync(
        long dossierFinancierId,
        long paiementScolariteId,
        CancellationToken cancellationToken = default)
    {
        var paiement = await paiementsScolarite.GetByIdAsync(paiementScolariteId, cancellationToken)
            ?? throw new InvalidOperationException("Le paiement selectionne est introuvable.");

        if (paiement.DossierFinancierId != dossierFinancierId)
        {
            throw new InvalidOperationException("Le paiement selectionne n'appartient pas au dossier financier selectionne.");
        }

        if (paiement.MontantAffecte > 0)
        {
            throw new InvalidOperationException("Un paiement deja affecte ne peut pas etre supprime directement.");
        }

        paiement.Statut = StatutPaiementScolarite.Annule;
        paiement.MontantNonAffecte = 0;

        await paiementsScolarite.SaveChangesAsync(cancellationToken);
        return await BuildFinanceDtoAsync(dossierFinancierId, cancellationToken);
    }

    private async Task<DossierFinancier> GetOrCreateDossierFinancierPourPaiementAsync(
        PaiementLibreDossierDto dto,
        CancellationToken cancellationToken)
    {
        if (dto.DossierFinancierId > 0)
        {
            var dossierExistant = await dossiersFinanciers.GetByIdAsync(dto.DossierFinancierId, cancellationToken)
                ?? throw new InvalidOperationException("Le dossier financier selectionne est introuvable.");

            var dossierEtudiantExistant = await EnsureDossierEtudiantAsync(
                dossierExistant.EtudiantId,
                dossierExistant.ParcoursAcademiqueId,
                cancellationToken);
            dossierExistant.DossierEtudiantId = dossierEtudiantExistant.Id;
            await dossiersFinanciers.SaveChangesAsync(cancellationToken);
            return dossierExistant;
        }

        if (dto.EtudiantId <= 0)
        {
            throw new InvalidOperationException("L'etudiant du paiement est obligatoire.");
        }

        if (dto.ParcoursAcademiqueId <= 0)
        {
            throw new InvalidOperationException("Le parcours academique du paiement est obligatoire.");
        }

        _ = await etudiants.GetByIdAsync(dto.EtudiantId, cancellationToken)
            ?? throw new InvalidOperationException("L'etudiant selectionne est introuvable.");

        var parcours = await parcoursAcademiques.GetByIdAsync(dto.ParcoursAcademiqueId, cancellationToken)
            ?? throw new InvalidOperationException("Le parcours academique selectionne est introuvable.");

        if (!parcours.EstActive)
        {
            throw new InvalidOperationException("Le parcours academique selectionne est inactif.");
        }

        var existing = (await dossiersFinanciers.ListAsync(cancellationToken))
            .FirstOrDefault(x => x.EtudiantId == dto.EtudiantId && x.ParcoursAcademiqueId == dto.ParcoursAcademiqueId);

        if (existing is not null)
        {
            var dossierEtudiantExistant = await EnsureDossierEtudiantAsync(
                existing.EtudiantId,
                existing.ParcoursAcademiqueId,
                cancellationToken);
            existing.DossierEtudiantId = dossierEtudiantExistant.Id;
            await dossiersFinanciers.SaveChangesAsync(cancellationToken);
            return existing;
        }

        var dossierEtudiant = await EnsureDossierEtudiantAsync(
            dto.EtudiantId,
            dto.ParcoursAcademiqueId,
            cancellationToken);
        var now = DateTime.UtcNow;
        var dossier = new DossierFinancier
        {
            EtudiantId = dto.EtudiantId,
            ParcoursAcademiqueId = dto.ParcoursAcademiqueId,
            DossierEtudiantId = dossierEtudiant.Id,
            StatutAdministratif = StatutDossierScolarite.EnPreparation,
            StatutFinancier = StatutDossierScolarite.EnPreparation,
            StatutGlobal = StatutDossierScolarite.EnPreparation,
            CreeLeUtc = now,
            DateDernierRecalculUtc = now
        };

        await dossiersFinanciers.AddAsync(dossier, cancellationToken);
        await dossiersFinanciers.SaveChangesAsync(cancellationToken);

        return dossier;
    }

    private async Task<DossierEtudiant> EnsureDossierEtudiantAsync(
        long etudiantId,
        long parcoursAcademiqueId,
        CancellationToken cancellationToken)
    {
        var dossier = (await dossiersEtudiants.ListAsync(cancellationToken))
            .FirstOrDefault(x => x.EtudiantId == etudiantId && x.ParcoursAcademiqueId == parcoursAcademiqueId);

        if (dossier is not null)
        {
            return dossier;
        }

        var parcours = await parcoursAcademiques.GetByIdAsync(parcoursAcademiqueId, cancellationToken)
            ?? throw new InvalidOperationException("Le parcours academique selectionne est introuvable.");

        dossier = new DossierEtudiant
        {
            EtudiantId = etudiantId,
            ParcoursAcademiqueId = parcoursAcademiqueId,
            Matricule = await matriculeGenerator.GenerateMatriculeAsync(
                parcours.AnneeAcademiqueId,
                cancellationToken)
        };

        await dossiersEtudiants.AddAsync(dossier, cancellationToken);
        await dossiersEtudiants.SaveChangesAsync(cancellationToken);

        return dossier;
    }

    private async Task EnsureElementsFinanciersAsync(
        DossierFinancier dossier,
        DateTime now,
        DateOnly dateExigibilite,
        CancellationToken cancellationToken)
    {
        var elements = await elementsScolariteEtudiants.ListAsync(cancellationToken);
        var existingTypeIds = elements
            .Where(x => x.DossierFinancierId == dossier.Id)
            .Select(x => x.TypeElementScolariteId)
            .ToHashSet();
        var types = await typesElementsScolarite.ListAsync(cancellationToken);

        foreach (var type in types.Where(x => x.EstActif && (x.EstDocumentaire || x.EstPayable)).OrderBy(x => x.OrdreAffichage).ThenBy(x => x.Libelle))
        {
            if (existingTypeIds.Contains(type.Id))
            {
                continue;
            }

            var tarif = await ResolveTarifForDossierAsync(dossier, type.Id, cancellationToken);
            if (type.EstPayable && tarif is null)
            {
                continue;
            }

            var montantAttendu = tarif?.Montant ?? 0;
            var element = new ElementScolariteEtudiant
            {
                DossierFinancierId = dossier.Id,
                TypeElementScolariteId = type.Id,
                Code = type.Code,
                Libelle = type.Libelle,
                MontantAttendu = montantAttendu,
                MontantAffecte = 0,
                MontantRestant = montantAttendu,
                Statut = StatutElementScolarite.NonDemarre,
                DateCreationUtc = now
            };

            await elementsScolariteEtudiants.AddAsync(element, cancellationToken);
            await elementsScolariteEtudiants.SaveChangesAsync(cancellationToken);

            if (type.EstPayable && montantAttendu > 0)
            {
                await echeancesScolarite.AddAsync(new EcheanceScolarite
                {
                    ElementScolariteEtudiantId = element.Id,
                    Numero = 1,
                    Libelle = "Echeance unique",
                    DateExigibilite = dateExigibilite,
                    MontantAttendu = montantAttendu,
                    MontantAffecte = 0,
                    MontantRestant = montantAttendu,
                    Statut = StatutEcheanceScolarite.NonExigible
                }, cancellationToken);
            }
        }

        await echeancesScolarite.SaveChangesAsync(cancellationToken);
    }

    private async Task<FinanceDossierScolariteDto> BuildFinanceDtoAsync(
        long dossierScolariteId,
        CancellationToken cancellationToken)
    {
        var elements = (await elementsScolariteEtudiants.ListAsync(cancellationToken))
            .Where(x => x.DossierFinancierId == dossierScolariteId)
            .ToList();
        var echeances = await echeancesScolarite.ListAsync(cancellationToken);
        var paiements = (await paiementsScolarite.ListAsync(cancellationToken))
            .Where(x => x.DossierFinancierId == dossierScolariteId)
            .ToList();
        var types = await typesElementsScolarite.ListAsync(cancellationToken);
        var tarifs = await GetTarifsForPaiementsAsync(paiements, cancellationToken);
        var affectations = await affectationsPaiementsEcheances.ListAsync(cancellationToken);
        var elementIds = elements.Select(x => x.Id).ToHashSet();
        var echeanceItems = echeances
            .Where(x => elementIds.Contains(x.ElementScolariteEtudiantId))
            .ToList();
        var echeanceIds = echeanceItems.Select(x => x.Id).ToHashSet();
        var paiementIds = paiements.Select(x => x.Id).ToHashSet();
        var affectationItems = affectations
            .Where(x => paiementIds.Contains(x.PaiementScolariteId) && echeanceIds.Contains(x.EcheanceScolariteId))
            .ToList();

        return new FinanceDossierScolariteDto
        {
            DossierFinancierId = dossierScolariteId,
            TotalPaiements = paiements.Where(x => x.Statut != StatutPaiementScolarite.Annule).Sum(x => x.Montant),
            TotalPaiementsAffectes = paiements.Where(x => x.Statut != StatutPaiementScolarite.Annule).Sum(x => x.MontantAffecte),
            TotalPaiementsNonAffectes = paiements.Where(x => x.Statut != StatutPaiementScolarite.Annule).Sum(x => x.MontantNonAffecte),
            Elements = elements
                .Where(x => x.MontantAttendu > 0)
                .OrderBy(x => x.Libelle)
                .Select(ToElementDto)
                .ToList(),
            Echeances = echeanceItems
                .OrderBy(x => x.DateExigibilite)
                .ThenBy(x => x.Numero)
                .Select(x => ToEcheanceDto(x, elements))
                .ToList(),
            Paiements = paiements
                .OrderByDescending(x => x.DatePaiement)
                .ThenByDescending(x => x.Id)
                .Select(x => ToPaiementDto(x, types, tarifs))
                .ToList(),
            Affectations = affectationItems
                .OrderByDescending(x => x.DateAffectationUtc)
                .Select(x => ToAffectationDto(x, paiements, echeanceItems, elements))
                .ToList()
        };
    }

    private static ElementFinancierDossierDto ToElementDto(ElementScolariteEtudiant entity)
        => new()
        {
            Id = entity.Id,
            TypeElementScolariteId = entity.TypeElementScolariteId,
            Code = entity.Code,
            Libelle = entity.Libelle,
            MontantAttendu = entity.MontantAttendu,
            MontantAffecte = entity.MontantAffecte,
            MontantRestant = entity.MontantRestant,
            Statut = entity.Statut
        };

    private static EcheanceFinanciereDossierDto ToEcheanceDto(
        EcheanceScolarite entity,
        IReadOnlyCollection<ElementScolariteEtudiant> elements)
    {
        var element = elements.FirstOrDefault(x => x.Id == entity.ElementScolariteEtudiantId);

        return new()
        {
            Id = entity.Id,
            ElementScolariteEtudiantId = entity.ElementScolariteEtudiantId,
            ElementLibelle = element?.Libelle ?? string.Empty,
            Numero = entity.Numero,
            Libelle = entity.Libelle,
            DateExigibilite = entity.DateExigibilite,
            MontantAttendu = entity.MontantAttendu,
            MontantAffecte = entity.MontantAffecte,
            MontantRestant = entity.MontantRestant,
            Statut = entity.Statut
        };
    }

    private static PaiementLibreDossierDto ToPaiementDto(
        PaiementScolarite entity,
        IReadOnlyCollection<TypeElementScolarite> types,
        IReadOnlyCollection<TarifScolariteDto> tarifs)
    {
        var type = entity.TypeElementScolariteId is null
            ? null
            : types.FirstOrDefault(x => x.Id == entity.TypeElementScolariteId);
        var tarif = entity.TarifScolariteId is null
            ? null
            : tarifs.FirstOrDefault(x => x.Id == entity.TarifScolariteId);

        return new()
        {
            Id = entity.Id,
            DossierFinancierId = entity.DossierFinancierId,
            TypeElementScolariteId = entity.TypeElementScolariteId ?? 0,
            TypeElementScolariteLibelle = type is null
                ? string.Empty
                : FormatCodeLibelle(type.Code, type.Libelle),
            TarifScolariteId = entity.TarifScolariteId ?? 0,
            TarifMontant = tarif?.Montant ?? 0,
            ModePaiementScolariteId = entity.ModePaiementScolariteId ?? 0,
            DatePaiement = entity.DatePaiement,
            Montant = entity.Montant,
            ModePaiement = entity.ModePaiement,
            ReferencePaiement = entity.ReferencePaiement,
            EncaissePar = entity.EncaissePar,
            Observation = entity.Observation,
            MontantAffecte = entity.MontantAffecte,
            MontantNonAffecte = entity.MontantNonAffecte,
            Statut = entity.Statut
        };
    }

    private async Task<TarifScolariteDto?> ResolveTarifForDossierAsync(
        DossierFinancier dossier,
        long typeElementScolariteId,
        CancellationToken cancellationToken)
        => await ResolveTarifForParcoursAsync(dossier.ParcoursAcademiqueId, typeElementScolariteId, cancellationToken);

    private async Task<TarifScolariteDto?> ResolveTarifForParcoursAsync(
        long parcoursAcademiqueId,
        long typeElementScolariteId,
        CancellationToken cancellationToken)
        => await tarifsScolariteService.ResolveTarifScolariteAsync(
            typeElementScolariteId,
            parcoursAcademiqueId,
            cancellationToken);

    private async Task<List<TarifScolariteDto>> GetTarifsForPaiementsAsync(
        IReadOnlyCollection<PaiementScolarite> paiements,
        CancellationToken cancellationToken)
    {
        var result = new List<TarifScolariteDto>();
        var tarifIds = paiements
            .Where(x => x.TarifScolariteId is not null)
            .Select(x => x.TarifScolariteId!.Value)
            .Distinct()
            .ToList();

        foreach (var tarifId in tarifIds)
        {
            var tarif = await tarifsScolariteService.GetTarifScolariteAsync(tarifId, cancellationToken);
            if (tarif is not null)
            {
                result.Add(tarif);
            }
        }

        return result;
    }

    private static string FormatCodeLibelle(string code, string libelle)
        => string.IsNullOrWhiteSpace(code) ? libelle : $"{code} - {libelle}";

    private static AffectationPaiementDossierDto ToAffectationDto(
        AffectationPaiementEcheance entity,
        IReadOnlyCollection<PaiementScolarite> paiements,
        IReadOnlyCollection<EcheanceScolarite> echeances,
        IReadOnlyCollection<ElementScolariteEtudiant> elements)
    {
        var paiement = paiements.FirstOrDefault(x => x.Id == entity.PaiementScolariteId);
        var echeance = echeances.FirstOrDefault(x => x.Id == entity.EcheanceScolariteId);
        var element = echeance is null
            ? null
            : elements.FirstOrDefault(x => x.Id == echeance.ElementScolariteEtudiantId);

        return new()
        {
            Id = entity.Id,
            PaiementScolariteId = entity.PaiementScolariteId,
            EcheanceScolariteId = entity.EcheanceScolariteId,
            PaiementLibelle = paiement is null ? string.Empty : $"{paiement.DatePaiement:dd/MM/yyyy} - {paiement.Montant:N0}",
            EcheanceLibelle = echeance is null ? string.Empty : $"{element?.Libelle} / {echeance.Libelle}",
            MontantAffecte = entity.MontantAffecte,
            DateAffectationUtc = entity.DateAffectationUtc,
            AffectePar = entity.AffectePar
        };
    }

    private static StatutPaiementScolarite ResolvePaiementStatut(decimal montant, decimal montantAffecte)
    {
        if (montantAffecte <= 0)
        {
            return StatutPaiementScolarite.NonAffecte;
        }

        return montantAffecte >= montant
            ? StatutPaiementScolarite.Affecte
            : StatutPaiementScolarite.PartiellementAffecte;
    }

    private static string RequireText(string? value, string errorMessage)
    {
        var normalized = NormalizeText(value);
        if (normalized is null)
        {
            throw new InvalidOperationException(errorMessage);
        }

        return normalized;
    }

    private static string? NormalizeText(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
