namespace RIIS.Academic.Domain;

public class AdmissionInfo
{
    public long Id { get; set; }
    public long EtudiantId { get; set; }
    public string? SerieBaccalaureat { get; set; }
    public short? AnneeObtentionBaccalaureat { get; set; }
    public string? MentionBaccalaureat { get; set; }
    public string? DiplomeEntree { get; set; }
    public string? SpecialiteDiplomeEntree { get; set; }
    public string? NumeroEquivalence { get; set; }
    public string? DiplomeEquivalence { get; set; }

    public Etudiant Etudiant { get; set; } = null!;
}
