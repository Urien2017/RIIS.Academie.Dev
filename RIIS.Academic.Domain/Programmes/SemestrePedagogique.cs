using System.ComponentModel.DataAnnotations.Schema;

namespace RIIS.Academic.Domain;

public class SemestrePedagogique
{
    public long Id { get; set; }
    public long MaquettePedagogiqueId { get; set; }
    public long? NiveauEtudeId { get; set; }
    public byte NumeroSemestre { get; set; }
    public required string Libelle { get; set; }
    public short OrdreAffichage { get; set; }

    [NotMapped]
    public decimal CreditsUE => ElementsConstitutifs?.Sum(ec => ec.Credits) ?? 0m;

    [NotMapped]
    public short VolumeHoraireUE => (short)(ElementsConstitutifs?.Sum(ec => ec.VolumeHoraire) ?? 0);

    public MaquettePedagogique MaquettePedagogique { get; set; } = null!;
    public NiveauEtude? NiveauEtude { get; set; }
    public ICollection<MaquetteElementConstitutif> ElementsConstitutifs { get; set; } = [];
    public ICollection<ResultatSemestre> ResultatsSemestres { get; set; } = [];
    public ICollection<ProcesVerbal> ProcesVerbaux { get; set; } = [];
}
