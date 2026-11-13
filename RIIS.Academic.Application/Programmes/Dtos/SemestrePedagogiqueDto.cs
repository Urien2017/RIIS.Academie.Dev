namespace RIIS.Academic.Application.Programmes.Dtos;

public class SemestrePedagogiqueDto
{
    public long Id { get; set; }
    public long MaquettePedagogiqueId { get; set; }
    public long? NiveauEtudeId { get; set; }
    public string NiveauEtudeLibelle { get; set; } = string.Empty;
    public string MaquettePedagogiqueLibelle { get; set; } = string.Empty;
    public List<long> UniteEnseignementIds { get; set; } = [];
    public string UniteEnseignementLibelle { get; set; } = string.Empty;
    public byte NumeroSemestre { get; set; }
    public string Libelle { get; set; } = string.Empty;
    public decimal CreditsUE { get; set; }
    public short VolumeHoraireUE { get; set; }
    public short OrdreAffichage { get; set; }
    public int NombreUnitesEnseignement { get; set; }
}
