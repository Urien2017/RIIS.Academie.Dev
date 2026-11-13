namespace RIIS.Academic.Application.Releves.Dtos;

public class ReleveNotePdfExportDto
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/pdf";
    public byte[] Content { get; set; } = [];
}
