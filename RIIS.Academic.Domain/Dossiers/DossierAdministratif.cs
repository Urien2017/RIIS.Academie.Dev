namespace RIIS.Academic.Domain;

public class DossierAdministratif
{
    public long Id { get; set; }
    public long EtudiantId { get; set; }
    public long ParcoursAcademiqueId { get; set; }
    public long? DossierEtudiantId { get; set; }
    public StatutDossierAdministratif Statut { get; set; } = StatutDossierAdministratif.EnPreparation;
    public DateTime CreeLeUtc { get; set; } = DateTime.UtcNow;
    public byte[] Version { get; set; } = [];

    public Etudiant Etudiant { get; set; } = null!;
    public ParcoursAcademique ParcoursAcademique { get; set; } = null!;
    public DossierEtudiant? DossierEtudiant { get; set; }
}
