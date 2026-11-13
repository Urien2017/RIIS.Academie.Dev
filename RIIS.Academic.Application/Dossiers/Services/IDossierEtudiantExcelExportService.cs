using RIIS.Academic.Application.Dossiers.Dtos;

namespace RIIS.Academic.Application.Dossiers.Services;

public interface IDossierEtudiantExcelExportService
{
    Task<DossierEtudiantExcelExportDto> ExporterDossiersAsync(
        long? parcoursAcademiqueId = null,
        long? anneeAcademiqueId = null,
        long? cycleFormationId = null,
        long? niveauEtudeId = null,
        long? etudiantId = null,
        CancellationToken cancellationToken = default);
}
