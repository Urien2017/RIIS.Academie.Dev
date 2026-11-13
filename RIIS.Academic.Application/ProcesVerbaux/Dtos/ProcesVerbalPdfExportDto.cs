namespace RIIS.Academic.Application.ProcesVerbaux.Dtos;

public class ProcesVerbalPdfExportDto
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/pdf";
    public byte[] Content { get; set; } = [];
}
