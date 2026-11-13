namespace RIIS.Academic.Domain;

public class UniteEnseignement
{
    public long Id { get; set; }
    public long? MaquettePedagogiqueId { get; set; }
    public required string Code { get; set; }
    public required string Libelle { get; set; }
    public TypeElementConstitutif Type { get; set; } = TypeElementConstitutif.CoursMagistraux;

    public MaquettePedagogique? MaquettePedagogique { get; set; }
    public ICollection<ElementConstitutif> ElementsConstitutifs { get; set; } = [];
}
