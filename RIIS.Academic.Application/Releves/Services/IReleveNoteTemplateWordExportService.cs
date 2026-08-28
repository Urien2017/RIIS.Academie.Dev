using RIIS.Academic.Application.Releves.Dtos;

namespace RIIS.Academic.Application.Releves.Services;

public interface IReleveNoteTemplateWordExportService
{
    Task<ReleveNoteWordExportDto?> ExporterReleveAnnuelAsync(
        long dossierAcademiqueId,
        CancellationToken cancellationToken = default);
}
