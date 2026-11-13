namespace RIIS.Academic.Domain;

/// <summary>
/// Source numerisee d'une page du cahier. Le texte OCR est conserve pour audit
/// et correction, mais ne constitue pas directement une seance validee.
/// </summary>
public class CahierTextePage
{
    public long Id { get; set; }
    public long CahierTexteId { get; set; }
    public int NumeroPage { get; set; }
    public required string FichierOriginalUri { get; set; }
    public required string EmpreinteSha256 { get; set; }
    public string? TexteOcr { get; set; }
    public decimal? ConfianceOcr { get; set; }
    public StatutPageCahierTexte Statut { get; set; } = StatutPageCahierTexte.Importee;
    public DateTime ImporteLeUtc { get; set; } = DateTime.UtcNow;
    public DateTime? TraiteeLeUtc { get; set; }

    public CahierTexte CahierTexte { get; set; } = null!;
    public ICollection<SeanceCours> Seances { get; set; } = [];
}
