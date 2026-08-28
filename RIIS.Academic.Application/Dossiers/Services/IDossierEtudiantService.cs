using RIIS.Academic.Application.Dossiers.Dtos;

namespace RIIS.Academic.Application.Dossiers.Services;

public interface IDossierEtudiantService
{
    Task<List<DossierEtudiantAnnuelDto>> GetDossiersEtudiantAnnuelsAsync(long? parcoursAcademiqueId = null, CancellationToken cancellationToken = default);
    Task<DossierEtudiantAnnuelDto?> GetDossierEtudiantAnnuelAsync(long etudiantId, long parcoursAcademiqueId, CancellationToken cancellationToken = default);
    Task<DossierEtudiantAnnuelDto> GetOrCreateDossierEtudiantAnnuelAsync(long etudiantId, long parcoursAcademiqueId, long? classePedagogiqueId = null, CancellationToken cancellationToken = default);
}
