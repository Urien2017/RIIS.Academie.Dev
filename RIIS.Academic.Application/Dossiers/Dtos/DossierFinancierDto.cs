using RIIS.Academic.Domain;

namespace RIIS.Academic.Application.Dossiers.Dtos;

public class DossierFinancierDto
{
    public long Id { get; set; }
    public long EtudiantId { get; set; }
    public long ParcoursAcademiqueId { get; set; }
    public StatutDossierScolarite StatutAdministratif { get; set; } = StatutDossierScolarite.EnPreparation;
    public StatutDossierScolarite StatutFinancier { get; set; } = StatutDossierScolarite.EnPreparation;
    public StatutDossierScolarite StatutGlobal { get; set; } = StatutDossierScolarite.EnPreparation;
    public DateTime CreeLeUtc { get; set; }
    public DateTime? DateDernierRecalculUtc { get; set; }
    public string? Observation { get; set; }
    public string EtudiantMatricule { get; set; } = string.Empty;
    public string EtudiantNomComplet { get; set; } = string.Empty;
    public string ParcoursLibelle { get; set; } = string.Empty;
    public string AnneeAcademiqueLibelle { get; set; } = string.Empty;

    public static DossierFinancierDto FromEntity(
        DossierFinancier entity,
        Etudiant? etudiant = null,
        ParcoursAcademique? parcours = null,
        AnneeAcademique? annee = null)
        => new()
        {
            Id = entity.Id,
            EtudiantId = entity.EtudiantId,
            ParcoursAcademiqueId = entity.ParcoursAcademiqueId,
            StatutAdministratif = entity.StatutAdministratif,
            StatutFinancier = entity.StatutFinancier,
            StatutGlobal = entity.StatutGlobal,
            CreeLeUtc = entity.CreeLeUtc,
            DateDernierRecalculUtc = entity.DateDernierRecalculUtc,
            Observation = entity.Observation,
            EtudiantMatricule = etudiant?.Matricule ?? string.Empty,
            EtudiantNomComplet = etudiant is null ? string.Empty : $"{etudiant.Nom} {etudiant.Prenoms}".Trim(),
            ParcoursLibelle = parcours?.Libelle ?? string.Empty,
            AnneeAcademiqueLibelle = annee?.Libelle ?? string.Empty
        };
}
