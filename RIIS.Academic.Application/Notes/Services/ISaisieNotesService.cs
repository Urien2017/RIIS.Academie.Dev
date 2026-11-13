using RIIS.Academic.Application.Common.Dtos;
using RIIS.Academic.Application.Notes.Dtos;
using RIIS.Academic.Domain;

namespace RIIS.Academic.Application.Notes.Services;

public interface ISaisieNotesService
{
    Task<List<LookupDto>> GetAnneesAcademiquesLookupAsync(CancellationToken cancellationToken = default);
    Task<List<LookupDto>> GetClassesPedagogiquesLookupAsync(
        long? anneeAcademiqueId = null,
        CancellationToken cancellationToken = default);
    Task<List<LookupDto>> GetClassesForMatriculesLookupAsync(
        IEnumerable<string> matricules,
        long anneeAcademiqueId,
        CancellationToken cancellationToken = default);
    Task<List<LookupDto>> GetSemestresForClasseLookupAsync(
        long anneeAcademiqueId,
        long classePedagogiqueId,
        CancellationToken cancellationToken = default);
    Task<NotesEvaluationContextDto?> GetEvaluationContextAsync(
        long anneeAcademiqueId,
        long classePedagogiqueId,
        long semestrePedagogiqueId,
        long elementConstitutifId,
        CancellationToken cancellationToken = default);
    Task<SaisieNotesEvaluationContextDto?> GetEvaluationContextForEvaluationAsync(
        long evaluationAcademiqueId,
        CancellationToken cancellationToken = default);
    Task<List<LookupDto>> GetUnitesEnseignementLookupAsync(
        long? semestrePedagogiqueId = null,
        CancellationToken cancellationToken = default);
    Task<List<LookupDto>> GetElementsConstitutifsLookupAsync(
        long? uniteEnseignementId = null,
        long? semestrePedagogiqueId = null,
        CancellationToken cancellationToken = default);
    Task<List<LookupDto>> GetEvaluationsLookupAsync(
        long? anneeAcademiqueId = null,
        long? uniteEnseignementId = null,
        long? elementConstitutifId = null,
        TypeEvaluation? typeEvaluation = null,
        long? semestrePedagogiqueId = null,
        CancellationToken cancellationToken = default);

    Task<SaisieNotesGrilleDto> GetGrilleSaisieAsync(
        long evaluationAcademiqueId,
        long classePedagogiqueId,
        CancellationToken cancellationToken = default);

    Task SaveNotesAsync(SaisieNotesGrilleDto grille, string? saisiePar = null, CancellationToken cancellationToken = default);
}
