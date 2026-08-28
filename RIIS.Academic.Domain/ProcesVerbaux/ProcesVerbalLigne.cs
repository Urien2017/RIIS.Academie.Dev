namespace RIIS.Academic.Domain;

public class ProcesVerbalLigne
{
    public long Id { get; set; }
    public long ProcesVerbalId { get; set; }
    public long DossierAcademiqueId { get; set; }
    public required string MatriculeSnapshot { get; set; }
    public required string NomCompletSnapshot { get; set; }
    public decimal? MoyenneGenerale { get; set; }
    public decimal? CreditsAcquis { get; set; }
    public int? Rang { get; set; }
    public DecisionAcademique DecisionJury { get; set; } = DecisionAcademique.NonDeliberee;
    public string? DetailsNotesJson { get; set; }

    public ProcesVerbal ProcesVerbal { get; set; } = null!;
    public DossierAcademique DossierAcademique { get; set; } = null!;
}
