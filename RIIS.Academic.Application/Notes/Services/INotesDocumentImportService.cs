using RIIS.Academic.Application.Notes.Dtos;

namespace RIIS.Academic.Application.Notes.Services;

public interface INotesDocumentImportService
{
    Task<NotesDocumentPreviewDto> AnalyserAsync(
        Stream fichier,
        string nomFichier,
        CancellationToken cancellationToken = default);

    Task<NotesDocumentImportResultDto> ImporterAsync(
        NotesDocumentPreviewDto apercu,
        NotesDocumentImportContextDto contexte,
        CancellationToken cancellationToken = default);
}
