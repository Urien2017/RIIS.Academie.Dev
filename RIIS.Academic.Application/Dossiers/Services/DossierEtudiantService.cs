using RIIS.Academic.Application.Dossiers.Dtos;
using RIIS.Academic.Application.Etudiants.Services;
using RIIS.Academic.Application.Referentiels.Services;

namespace RIIS.Academic.Application.Dossiers.Services;

public class DossierEtudiantService(
    IEtudiantsService etudiantsService,
    IReferentielsService referentielsService,
    IDossierAdministratifService dossierAdministratifService,
    IDossierAcademiqueService dossierAcademiqueService,
    IDossierFinancierService dossierFinancierService) : IDossierEtudiantService
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
        => new()
        {
            EtudiantId = etudiantId,
            ParcoursAcademiqueId = parcoursAcademiqueId,
            Etudiant = await etudiantsService.GetEtudiantAsync(etudiantId, cancellationToken),
            Administratif = await dossierAdministratifService.GetOrCreateDossierAdministratifAsync(etudiantId, parcoursAcademiqueId, cancellationToken),
            Academique = await dossierAcademiqueService.GetOrCreateDossierAcademiqueAsync(etudiantId, parcoursAcademiqueId, classePedagogiqueId, cancellationToken),
            Financier = await dossierFinancierService.GetDossierFinancierAsync(etudiantId, parcoursAcademiqueId, cancellationToken)
        };

    private static DateTime GetDateCreation(DossierEtudiantAnnuelDto dossier)
        => dossier.Academique?.CreeLeUtc
            ?? dossier.Administratif?.CreeLeUtc
            ?? dossier.Financier?.CreeLeUtc
            ?? DateTime.MinValue;

}
