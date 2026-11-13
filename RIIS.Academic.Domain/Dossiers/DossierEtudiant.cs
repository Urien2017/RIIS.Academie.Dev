namespace RIIS.Academic.Domain;

/// <summary>
/// Dossier annuel central d'un étudiant. Le matricule est attribué à ce niveau,
/// car un même étudiant peut avoir un matricule différent selon l'année académique.
/// </summary>
public class DossierEtudiant
{
    public long Id { get; set; }
    public long EtudiantId { get; set; }
    public long ParcoursAcademiqueId { get; set; }
    public required string Matricule { get; set; }
    public DateTime CreeLeUtc { get; set; } = DateTime.UtcNow;
    public byte[] Version { get; set; } = [];

    public Etudiant Etudiant { get; set; } = null!;
    public ParcoursAcademique ParcoursAcademique { get; set; } = null!;
    public DossierAdministratif? DossierAdministratif { get; set; }
    public DossierAcademique? DossierAcademique { get; set; }
    public DossierFinancier? DossierFinancier { get; set; }
}
