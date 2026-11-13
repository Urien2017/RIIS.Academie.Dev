using RIIS.Academic.Application.Cours.Dtos;

namespace RIIS.Academic.Application.Cours.Services;

public interface IReferentielCoursValidationService
{
    Task<ReferentielCoursValidationReportDto> ValidateAsync(
        CancellationToken cancellationToken = default);
}
