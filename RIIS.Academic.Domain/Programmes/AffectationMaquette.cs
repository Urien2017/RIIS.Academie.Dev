namespace RIIS.Academic.Domain;

/// <summary>
/// Association explicite entre une offre administrative et une maquette pédagogique.
/// Une maquette peut ainsi être réutilisée par plusieurs parcours et un parcours peut
/// conserver plusieurs versions historiques.
/// </summary>
public class AffectationMaquette
{
    public long Id { get; set; }
    public long ParcoursAcademiqueId { get; set; }
    public long MaquettePedagogiqueId { get; set; }
    public bool EstPrincipale { get; set; }
    public bool EstActive { get; set; } = true;
    public DateOnly? DateDebut { get; set; }
    public DateOnly? DateFin { get; set; }
    public string? Observation { get; set; }
    public DateTime CreeLeUtc { get; set; } = DateTime.UtcNow;

    public ParcoursAcademique ParcoursAcademique { get; set; } = null!;
    public MaquettePedagogique MaquettePedagogique { get; set; } = null!;
}
