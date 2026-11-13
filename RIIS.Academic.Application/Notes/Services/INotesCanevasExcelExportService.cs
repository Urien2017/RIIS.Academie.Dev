using RIIS.Academic.Application.Notes.Dtos;
using RIIS.Academic.Domain;

namespace RIIS.Academic.Application.Notes.Services;

public interface INotesCanevasExcelExportService
{
    Task<NotesCanevasExcelExportDto?> ExporterCanevasAsync(
        long anneeAcademiqueId,
        long classePedagogiqueId,
        long semestrePedagogiqueId,
        long? uniteEnseignementId = null,
        long? elementConstitutifId = null,
        TypeEvaluation? typeEvaluation = null,
        CancellationToken cancellationToken = default);
}
