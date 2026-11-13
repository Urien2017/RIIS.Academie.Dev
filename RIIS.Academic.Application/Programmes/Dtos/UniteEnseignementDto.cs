namespace RIIS.Academic.Application.Programmes.Dtos;

public class UniteEnseignementDto
{
    public long Id { get; set; }
    public long? MaquettePedagogiqueId { get; set; }
    public string MaquettePedagogiqueLibelle { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Libelle { get; set; } = string.Empty;
    public decimal Credits { get; set; }
    public short VolumeHoraire { get; set; }
    public int NombreElementsConstitutifs { get; set; }
    public List<ElementConstitutifDto> ElementsConstitutifs { get; set; } = [];
    public string SelectionLabel => $"{Code} - {Libelle} | {Credits:0.##} crédits - {VolumeHoraire} h";
}
