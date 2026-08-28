using RIIS.Academic.Application.Dossiers.Dtos;

namespace RIIS.Academic.Application.Dossiers.Services;

public interface IDossierAcademiqueService
{
    Task<DossierAcademiqueDto?> GetDossierAcademiqueAsync(long id, CancellationToken cancellationToken = default);
    Task<DossierAcademiqueDto?> GetDossierAcademiqueAsync(long etudiantId, long parcoursAcademiqueId, CancellationToken cancellationToken = default);
    Task<DossierAcademiqueDto> GetOrCreateDossierAcademiqueAsync(long etudiantId, long parcoursAcademiqueId, long? classePedagogiqueId = null, CancellationToken cancellationToken = default);
}
