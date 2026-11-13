using RIIS.Academic.Application.Cours.Dtos;

namespace RIIS.Academic.Application.Cours.Services;

public interface ICahierTexteImportService
{
    Task<CahierTexteImportResultDto> ImporterAsync(
        CahierTexteImportRequestDto request,
        CancellationToken cancellationToken = default);
}
