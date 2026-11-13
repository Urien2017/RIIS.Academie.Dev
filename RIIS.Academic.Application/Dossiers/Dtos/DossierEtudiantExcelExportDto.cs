namespace RIIS.Academic.Application.Dossiers.Dtos;

public sealed class DossierEtudiantExcelExportDto
{
    public required string FileName { get; init; }
    public required byte[] Content { get; init; }
    public string ContentType { get; init; } =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
}
