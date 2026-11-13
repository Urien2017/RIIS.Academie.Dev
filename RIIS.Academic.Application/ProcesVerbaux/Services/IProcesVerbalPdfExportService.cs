using RIIS.Academic.Application.ProcesVerbaux.Dtos;

namespace RIIS.Academic.Application.ProcesVerbaux.Services;

public interface IProcesVerbalPdfExportService
{
    Task<ProcesVerbalPdfExportDto?> ExporterProcesVerbalAsync(
        long procesVerbalId,
        CancellationToken cancellationToken = default);
}
