namespace RIIS.Academic.Application.Programmes.Dtos;

public sealed class AffectationMaquetteDto
{
    public long Id { get; set; }
    public long ParcoursAcademiqueId { get; set; }
    public long MaquettePedagogiqueId { get; set; }
    public string ParcoursAcademiqueLibelle { get; set; } = string.Empty;
    public string MaquettePedagogiqueLibelle { get; set; } = string.Empty;
    public bool EstPrincipale { get; set; }
    public bool EstActive { get; set; } = true;
    public DateOnly? DateDebut { get; set; }
    public DateOnly? DateFin { get; set; }
    public string? Observation { get; set; }
}
