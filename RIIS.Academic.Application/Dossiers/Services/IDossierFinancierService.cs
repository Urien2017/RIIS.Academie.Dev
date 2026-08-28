using RIIS.Academic.Application.Dossiers.Dtos;

namespace RIIS.Academic.Application.Dossiers.Services;

public interface IDossierFinancierService
{
    Task<DossierFinancierDto?> GetDossierFinancierAsync(long id, CancellationToken cancellationToken = default);
    Task<DossierFinancierDto?> GetDossierFinancierAsync(long etudiantId, long parcoursAcademiqueId, CancellationToken cancellationToken = default);
    Task<DossierFinancierDto> GetOrCreateDossierFinancierAsync(long etudiantId, long parcoursAcademiqueId, CancellationToken cancellationToken = default);
}
