namespace RIIS.Academic.Domain;

public class DossierFinancier
{
    public long Id { get; set; }
    public long EtudiantId { get; set; }
    public long ParcoursAcademiqueId { get; set; }
    public StatutDossierScolarite StatutAdministratif { get; set; } = StatutDossierScolarite.EnPreparation;
    public StatutDossierScolarite StatutFinancier { get; set; } = StatutDossierScolarite.EnPreparation;
    public StatutDossierScolarite StatutGlobal { get; set; } = StatutDossierScolarite.EnPreparation;
    public DateTime CreeLeUtc { get; set; } = DateTime.UtcNow;
    public DateTime? DateDernierRecalculUtc { get; set; }
    public string? Observation { get; set; }
    public byte[] Version { get; set; } = [];

    public Etudiant Etudiant { get; set; } = null!;
    public ParcoursAcademique ParcoursAcademique { get; set; } = null!;
    public ICollection<ElementScolariteEtudiant> ElementsScolarite { get; set; } = [];
    public ICollection<PaiementScolarite> Paiements { get; set; } = [];
    public ICollection<NotificationScolarite> Notifications { get; set; } = [];
}
