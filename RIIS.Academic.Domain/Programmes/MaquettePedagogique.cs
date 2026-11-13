using System.ComponentModel.DataAnnotations.Schema;

namespace RIIS.Academic.Domain;

public class MaquettePedagogique
{
    public long Id { get; set; }
    // Compatibilité de transition avec les anciennes données. La relation métier
    // active est AffectationsMaquettes; cette colonne ne porte plus de FK.
    public long? ParcoursAcademiqueId { get; set; }
    public required string Code { get; set; }
    public required string Libelle { get; set; }
    public required string Version { get; set; }
    public StatutMaquettePedagogique Statut { get; set; } = StatutMaquettePedagogique.Brouillon;
    public DateOnly? DateDebutValidite { get; set; }
    public DateOnly? DateFinValidite { get; set; }
    public string? SourceDocument { get; set; }
    public string? Observation { get; set; }
    public DateTime CreeLeUtc { get; set; } = DateTime.UtcNow;

    [NotMapped]
    public decimal CreditsTotaux => Semestres?.Sum(s => s.CreditsUE) ?? 0m;

    [NotMapped]
    public short VolumeHoraireTotal => (short)(Semestres?.Sum(s => s.VolumeHoraireUE) ?? 0);

    public ICollection<AffectationMaquette> AffectationsMaquettes { get; set; } = [];
    public ICollection<SemestrePedagogique> Semestres { get; set; } = [];
    public ICollection<UniteEnseignement> UnitesEnseignement { get; set; } = [];
}
