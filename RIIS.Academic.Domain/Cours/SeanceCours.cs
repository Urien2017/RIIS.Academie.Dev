namespace RIIS.Academic.Domain;

/// <summary>
/// Seance de cours validee ou en cours de correction. L'EC est porte par
/// MaquetteElementConstitutif pour conserver le contexte exact de la maquette.
/// </summary>
public class SeanceCours
{
    public long Id { get; set; }
    public long CahierTexteId { get; set; }
    public long MaquetteElementConstitutifId { get; set; }
    public long? CahierTextePageId { get; set; }
    public DateOnly DateCours { get; set; }
    public TimeOnly? HeureDebut { get; set; }
    public TimeOnly? HeureFin { get; set; }
    public NatureSeanceCours Nature { get; set; } = NatureSeanceCours.Cours;
    public string? Module { get; set; }
    public string? NumeroChapitre { get; set; }
    public string? TitreLecon { get; set; }
    public string? Paragraphe { get; set; }
    public string? PointsEssentiels { get; set; }
    public string? PointsPrincipaux { get; set; }
    public string? ExercicesApplications { get; set; }
    public string? DevoirsMaison { get; set; }
    public string? Evaluation { get; set; }
    public string? CompetencesObjectifs { get; set; }
    public string? Observation { get; set; }
    public decimal? ConfianceExtraction { get; set; }
    public StatutSeanceCours Statut { get; set; } = StatutSeanceCours.Brouillon;
    public DateTime CreeLeUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ModifieLeUtc { get; set; }

    public CahierTexte CahierTexte { get; set; } = null!;
    public MaquetteElementConstitutif MaquetteElementConstitutif { get; set; } = null!;
    public CahierTextePage? CahierTextePage { get; set; }
}
