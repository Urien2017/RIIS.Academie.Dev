namespace RIIS.Academic.Domain;

public class DocumentPersonnel
{
    public long Id { get; set; }
    public long EtudiantId { get; set; }
    public required string NomDocument { get; set; }
    public required string UrlFichier { get; set; }
    public DateTime DateDepotUtc { get; set; } = DateTime.UtcNow;
    public DateTime? DateVerificationUtc { get; set; }
    public string? VerifiePar { get; set; }
    public string? Observation { get; set; }

    public Etudiant Etudiant { get; set; } = null!;
}
