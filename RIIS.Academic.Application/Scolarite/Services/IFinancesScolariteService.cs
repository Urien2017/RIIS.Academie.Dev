using RIIS.Academic.Application.Scolarite.Dtos;

namespace RIIS.Academic.Application.Scolarite.Services;

public interface IFinancesScolariteService
{
    Task<FinanceDossierScolariteDto?> GetFinanceDossierScolariteAsync(
        long dossierScolariteId,
        CancellationToken cancellationToken = default);

    Task<List<PaiementTypeElementTarifOptionDto>> GetOptionsTypesElementsTarifsPaiementAsync(
        long dossierScolariteId,
        CancellationToken cancellationToken = default);

    Task<List<PaiementTypeElementTarifOptionDto>> GetOptionsTypesElementsTarifsPaiementParParcoursAsync(
        long parcoursAcademiqueId,
        CancellationToken cancellationToken = default);

    Task<FinanceDossierScolariteDto> SavePaiementLibreAsync(
        PaiementLibreDossierDto dto,
        CancellationToken cancellationToken = default);

    Task<FinanceDossierScolariteDto> AnnulerPaiementLibreAsync(
        long dossierFinancierId,
        long paiementScolariteId,
        CancellationToken cancellationToken = default);
}
