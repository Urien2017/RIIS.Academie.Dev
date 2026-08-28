namespace RIIS.Academic.Domain;

public class DossierAcademique
{
    public long Id { get; set; }
    public long EtudiantId { get; set; }
    public long ParcoursAcademiqueId { get; set; }
    public long? ClassePedagogiqueId { get; set; }
    public StatutDossierAcademique Statut { get; set; } = StatutDossierAcademique.EnCours;
    public DateTime CreeLeUtc { get; set; } = DateTime.UtcNow;
    public byte[] Version { get; set; } = [];

    public Etudiant Etudiant { get; set; } = null!;
    public ParcoursAcademique ParcoursAcademique { get; set; } = null!;
    public ClassePedagogique? ClassePedagogique { get; set; }
    public ICollection<NoteEvaluation> NotesEvaluations { get; set; } = [];
    public ICollection<ResultatElementConstitutif> ResultatsElementsConstitutifs { get; set; } = [];
    public ICollection<ResultatUniteEnseignement> ResultatsUnitesEnseignement { get; set; } = [];
    public ICollection<ResultatSemestre> ResultatsSemestres { get; set; } = [];
    public ICollection<ResultatAnnuel> ResultatsAnnuels { get; set; } = [];
    public ICollection<ProcesVerbalLigne> LignesProcesVerbaux { get; set; } = [];
}
