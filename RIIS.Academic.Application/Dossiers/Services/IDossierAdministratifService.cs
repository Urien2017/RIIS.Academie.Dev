using RIIS.Academic.Application.Dossiers.Dtos;

namespace RIIS.Academic.Application.Dossiers.Services;

public interface IDossierAdministratifService
{
    Task<DossierAdministratifDto?> GetDossierAdministratifAsync(long id, CancellationToken cancellationToken = default);
    Task<DossierAdministratifDto?> GetDossierAdministratifAsync(long etudiantId, long parcoursAcademiqueId, CancellationToken cancellationToken = default);
    Task<DossierAdministratifDto> GetOrCreateDossierAdministratifAsync(long etudiantId, long parcoursAcademiqueId, CancellationToken cancellationToken = default);
}
