using RIIS.Academic.Application.Abstractions.Persistence;
using RIIS.Academic.Application.Dossiers.Dtos;
using RIIS.Academic.Application.Etudiants.Services;
using RIIS.Academic.Application.Referentiels.Services;
using RIIS.Academic.Domain;

namespace RIIS.Academic.Application.Dossiers.Services;

public class DossierEtudiantService(
    IEtudiantsService etudiantsService,
    IReferentielsService referentielsService,
    IDossierAdministratifService dossierAdministratifService,
    IDossierAcademiqueService dossierAcademiqueService,
    IDossierFinancierService dossierFinancierService,
    IRepository<DossierEtudiant> dossiersEtudiants,
    IRepository<DossierAdministratif> dossiersAdministratifs,
    IRepository<DossierAcademique> dossiersAcademiques,
    IRepository<DossierFinancier> dossiersFinanciers,
    IRepository<PaiementScolarite> paiementsScolarite,
    IRepository<ParcoursAcademique> parcoursAcademiques,
    IRepository<ClassePedagogique> classesPedagogiques) : IDossierEtudiantService
{
    public async Task<List<DossierEtudiantAnnuelDto>> GetDossiersEtudiantAnnuelsAsync(
        long? parcoursAcademiqueId = null,
        CancellationToken cancellationToken = default)
    {
        var etudiants = await etudiantsService.GetEtudiantsAsync(cancellationToken: cancellationToken);
        var parcours = await referentielsService.GetParcoursAcademiquesAsync(
            inclureInactifs: true,
            cancellationToken: cancellationToken);

        if (parcoursAcademiqueId is not null)
        {
            parcours = parcours.Where(x => x.Id == parcoursAcademiqueId.Value).ToList();
        }

        var dossiers = new List<DossierEtudiantAnnuelDto>();
        foreach (var etudiant in etudiants)
        {
            foreach (var item in parcours)
            {
                var dossier = await GetDossierEtudiantAnnuelAsync(etudiant.Id, item.Id, cancellationToken);
                if (dossier?.EstComplet == true)
                {
                    dossiers.Add(dossier);
                }
            }
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
        var etudiant = await etudiantsService.GetEtudiantAsync(etudiantId, cancellationToken);
        if (etudiant is null)
        {
            return null;
        }

        return new DossierEtudiantAnnuelDto
        {
            EtudiantId = etudiantId,
            ParcoursAcademiqueId = parcoursAcademiqueId,
            Etudiant = etudiant,
            Administratif = await dossierAdministratifService.GetDossierAdministratifAsync(etudiantId, parcoursAcademiqueId, cancellationToken),
            Academique = await dossierAcademiqueService.GetDossierAcademiqueAsync(etudiantId, parcoursAcademiqueId, cancellationToken),
            Financier = await dossierFinancierService.GetDossierFinancierAsync(etudiantId, parcoursAcademiqueId, cancellationToken)
        };
    }

    public async Task<DossierEtudiantAnnuelDto> GetOrCreateDossierEtudiantAnnuelAsync(
        long etudiantId,
        long parcoursAcademiqueId,
        long? classePedagogiqueId = null,
        CancellationToken cancellationToken = default)
    {
        var etudiant = await etudiantsService.GetEtudiantAsync(etudiantId, cancellationToken)
            ?? throw new InvalidOperationException("L'etudiant selectionne est introuvable.");

        return new()
        {
            EtudiantId = etudiantId,
            ParcoursAcademiqueId = parcoursAcademiqueId,
            Etudiant = etudiant,
            Administratif = await dossierAdministratifService.GetOrCreateDossierAdministratifAsync(etudiantId, parcoursAcademiqueId, cancellationToken),
            Academique = await dossierAcademiqueService.GetOrCreateDossierAcademiqueAsync(etudiantId, parcoursAcademiqueId, classePedagogiqueId, cancellationToken),
            Financier = await dossierFinancierService.GetDossierFinancierAsync(etudiantId, parcoursAcademiqueId, cancellationToken)
        };
    }

    public async Task<DossierEtudiantAnnuelDto> ChangeParcoursAcademiqueAsync(
        long etudiantId,
        long ancienParcoursAcademiqueId,
        long nouveauParcoursAcademiqueId,
        CancellationToken cancellationToken = default)
    {
        if (ancienParcoursAcademiqueId == nouveauParcoursAcademiqueId)
        {
            return await GetDossierEtudiantAnnuelAsync(
                etudiantId,
                ancienParcoursAcademiqueId,
                cancellationToken)
                ?? throw new InvalidOperationException("Le dossier étudiant est introuvable.");
        }

        var dossiers = await dossiersEtudiants.ListAsync(cancellationToken);
        var current = dossiers.FirstOrDefault(x =>
            x.EtudiantId == etudiantId
            && x.ParcoursAcademiqueId == ancienParcoursAcademiqueId)
            ?? throw new InvalidOperationException("Le dossier étudiant sélectionné est introuvable.");

        var parcours = (await parcoursAcademiques.ListAsync(cancellationToken))
            .FirstOrDefault(x => x.Id == nouveauParcoursAcademiqueId)
            ?? throw new InvalidOperationException("Le nouveau parcours académique sélectionné est introuvable.");
        if (!parcours.EstActive)
        {
            throw new InvalidOperationException("Le nouveau parcours académique sélectionné est inactif.");
        }

        var allParcours = await parcoursAcademiques.ListAsync(cancellationToken);
        if (dossiers.Any(x =>
                x.Id != current.Id
                && x.EtudiantId == etudiantId
                && allParcours.Any(p =>
                    p.Id == x.ParcoursAcademiqueId
                    && p.AnneeAcademiqueId == parcours.AnneeAcademiqueId)))
        {
            throw new InvalidOperationException("Cet étudiant possède déjà un dossier pour la nouvelle année académique.");
        }

        if (dossiers.Any(x =>
                x.Id != current.Id
                && x.EtudiantId == etudiantId
                && x.ParcoursAcademiqueId == nouveauParcoursAcademiqueId))
        {
            throw new InvalidOperationException("Un dossier existe déjà pour cet étudiant et le nouveau parcours sélectionné.");
        }

        var administratifItems = await dossiersAdministratifs.ListAsync(cancellationToken);
        var academiqueItems = await dossiersAcademiques.ListAsync(cancellationToken);
        var financierItems = await dossiersFinanciers.ListAsync(cancellationToken);
        var paiementItems = await paiementsScolarite.ListAsync(cancellationToken);

        var administratif = administratifItems.FirstOrDefault(x =>
            x.DossierEtudiantId == current.Id
            || (x.EtudiantId == etudiantId && x.ParcoursAcademiqueId == ancienParcoursAcademiqueId));
        var academique = academiqueItems.FirstOrDefault(x =>
            x.DossierEtudiantId == current.Id
            || (x.EtudiantId == etudiantId && x.ParcoursAcademiqueId == ancienParcoursAcademiqueId));
        var financier = financierItems.FirstOrDefault(x =>
            x.DossierEtudiantId == current.Id
            || (x.EtudiantId == etudiantId && x.ParcoursAcademiqueId == ancienParcoursAcademiqueId));

        if (financier is not null && paiementItems.Any(x =>
                x.DossierFinancierId == financier.Id
                && x.Statut != StatutPaiementScolarite.Annule))
        {
            throw new InvalidOperationException("Le parcours ne peut plus être modifié : un paiement existe déjà pour ce dossier financier.");
        }

        if (administratifItems.Any(x =>
                (administratif is null || x.Id != administratif.Id)
                && x.EtudiantId == etudiantId
                && x.ParcoursAcademiqueId == nouveauParcoursAcademiqueId)
            || academiqueItems.Any(x =>
                (academique is null || x.Id != academique.Id)
                && x.EtudiantId == etudiantId
                && x.ParcoursAcademiqueId == nouveauParcoursAcademiqueId)
            || financierItems.Any(x =>
                (financier is null || x.Id != financier.Id)
                && x.EtudiantId == etudiantId
                && x.ParcoursAcademiqueId == nouveauParcoursAcademiqueId))
        {
            throw new InvalidOperationException("Les dossiers liés existent déjà pour le nouveau parcours sélectionné.");
        }

        var currentTracked = await dossiersEtudiants.GetByIdAsync(current.Id, cancellationToken)
            ?? throw new InvalidOperationException("Le dossier étudiant sélectionné est introuvable.");
        currentTracked.ParcoursAcademiqueId = nouveauParcoursAcademiqueId;

        if (administratif is not null)
        {
            var tracked = await dossiersAdministratifs.GetByIdAsync(administratif.Id, cancellationToken);
            if (tracked is not null) tracked.ParcoursAcademiqueId = nouveauParcoursAcademiqueId;
        }

        if (academique is not null)
        {
            var tracked = await dossiersAcademiques.GetByIdAsync(academique.Id, cancellationToken);
            if (tracked is not null)
            {
                tracked.ParcoursAcademiqueId = nouveauParcoursAcademiqueId;
                tracked.ClassePedagogiqueId = (await classesPedagogiques.ListAsync(cancellationToken))
                    .FirstOrDefault(x => x.ParcoursAcademiqueId == nouveauParcoursAcademiqueId)?.Id;
            }
        }

        if (financier is not null)
        {
            var tracked = await dossiersFinanciers.GetByIdAsync(financier.Id, cancellationToken);
            if (tracked is not null) tracked.ParcoursAcademiqueId = nouveauParcoursAcademiqueId;
        }

        await dossiersEtudiants.SaveChangesAsync(cancellationToken);
        return await GetDossierEtudiantAnnuelAsync(
            etudiantId,
            nouveauParcoursAcademiqueId,
            cancellationToken)
            ?? throw new InvalidOperationException("Le dossier étudiant modifié est introuvable.");
    }

    private static DateTime GetDateCreation(DossierEtudiantAnnuelDto dossier)
        => dossier.Academique?.CreeLeUtc
            ?? dossier.Administratif?.CreeLeUtc
            ?? dossier.Financier?.CreeLeUtc
            ?? DateTime.MinValue;

}
