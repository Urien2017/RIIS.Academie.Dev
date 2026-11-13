using RIIS.Academic.Application.Cours.Dtos;

namespace RIIS.Academic.Application.Cours.Services;

public interface ICahierTexteValidationService
{
    Task<CahierTexteValidationReportDto> ValiderAsync(
        CahierTexteImportRequestDto request,
        long? cahierTexteId = null,
        CancellationToken cancellationToken = default);

    Task<CahierTexteSeancesSaveResultDto> EnregistrerSeancesValideesAsync(
        long cahierTexteId,
        CahierTexteImportRequestDto request,
        CahierTexteValidationReportDto validation,
        CancellationToken cancellationToken = default);
}
