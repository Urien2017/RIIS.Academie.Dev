namespace RIIS.Academic.Domain;

/// <summary>
/// Cahier de texte d'une classe pour une annee et un parcours academiques.
/// Le parcours est la reference metier ; la classe fournit le contexte de lecture.
/// </summary>
public class CahierTexte
{
    public long Id { get; set; }
    public long AnneeAcademiqueId { get; set; }
    public long ParcoursAcademiqueId { get; set; }
    public long ClassePedagogiqueId { get; set; }
    public long? EnseignantUtilisateurId { get; set; }
    public string? EnseignantNom { get; set; }
    public StatutCahierTexte Statut { get; set; } = StatutCahierTexte.Actif;
    public DateOnly? DateOuverture { get; set; }
    public DateOnly? DateCloture { get; set; }
    public DateTime CreeLeUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ModifieLeUtc { get; set; }

    public AnneeAcademique AnneeAcademique { get; set; } = null!;
    public ParcoursAcademique ParcoursAcademique { get; set; } = null!;
    public ClassePedagogique ClassePedagogique { get; set; } = null!;
    public Utilisateur? EnseignantUtilisateur { get; set; }
    public ICollection<CahierTextePage> Pages { get; set; } = [];
    public ICollection<SeanceCours> Seances { get; set; } = [];
}
