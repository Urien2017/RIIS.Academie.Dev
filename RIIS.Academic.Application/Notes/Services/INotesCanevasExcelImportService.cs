using RIIS.Academic.Application.Notes.Dtos;

namespace RIIS.Academic.Application.Notes.Services;

public interface INotesCanevasExcelImportService
{
    Task<NotesCanevasImportResultDto> ImporterAsync(
        Stream fichier,
        CancellationToken cancellationToken = default);
}
