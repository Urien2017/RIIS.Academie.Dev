using RIIS.Academic.Domain;

namespace RIIS.Academic.Application.Dossiers.Dtos;

public class DossierEtudiantDto
{
    public long Id { get; set; }
    public long EtudiantId { get; set; }
    public long ParcoursAcademiqueId { get; set; }
    public string Matricule { get; set; } = string.Empty;
    public DateTime CreeLeUtc { get; set; }

    public static DossierEtudiantDto FromEntity(DossierEtudiant entity)
        => new()
        {
            Id = entity.Id,
            EtudiantId = entity.EtudiantId,
            ParcoursAcademiqueId = entity.ParcoursAcademiqueId,
            Matricule = entity.Matricule,
            CreeLeUtc = entity.CreeLeUtc
        };
}
