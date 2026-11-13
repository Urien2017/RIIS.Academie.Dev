using RIIS.Academic.Application.ProcesVerbaux.Dtos;
using RIIS.Academic.Domain;

namespace RIIS.Academic.Application.ProcesVerbaux.Services;

public interface IProcesVerbalOptions2WordExportService
{
    Task<ProcesVerbalWordExportDto?> ExporterFicheAsync(
        long anneeAcademiqueId,
        long classePedagogiqueId,
        long semestrePedagogiqueId,
        long? uniteEnseignementId = null,
        long? elementConstitutifId = null,
        TypeEvaluation? typeEvaluation = null,
        CancellationToken cancellationToken = default);
}
