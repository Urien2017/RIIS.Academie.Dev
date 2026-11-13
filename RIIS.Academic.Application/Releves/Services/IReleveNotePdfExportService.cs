using RIIS.Academic.Application.Releves.Dtos;

namespace RIIS.Academic.Application.Releves.Services;

public interface IReleveNotePdfExportService
{
    Task<ReleveNotePdfExportDto?> ExporterReleveAnnuelAsync(
        long dossierAcademiqueId,
        CancellationToken cancellationToken = default);
}
