using RIIS.Academic.Domain;

namespace RIIS.Academic.Application.Dossiers.Dtos;

public class DossierAcademiqueDto
{
    public long Id { get; set; }
    public long EtudiantId { get; set; }
    public long ParcoursAcademiqueId { get; set; }
    public long? ClassePedagogiqueId { get; set; }
    public StatutDossierAcademique Statut { get; set; } = StatutDossierAcademique.EnCours;
    public DateTime CreeLeUtc { get; set; }
    public string EtudiantMatricule { get; set; } = string.Empty;
    public string EtudiantNomComplet { get; set; } = string.Empty;
    public string ParcoursLibelle { get; set; } = string.Empty;
    public string AnneeAcademiqueLibelle { get; set; } = string.Empty;
    public string ClassePedagogiqueLibelle { get; set; } = string.Empty;

    public static DossierAcademiqueDto FromEntity(
        DossierAcademique entity,
        Etudiant? etudiant = null,
        ParcoursAcademique? parcours = null,
        AnneeAcademique? annee = null,
        ClassePedagogique? classe = null,
        string? matricule = null)
        => new()
        {
            Id = entity.Id,
            EtudiantId = entity.EtudiantId,
            ParcoursAcademiqueId = entity.ParcoursAcademiqueId,
            ClassePedagogiqueId = entity.ClassePedagogiqueId,
            Statut = entity.Statut,
            CreeLeUtc = entity.CreeLeUtc,
            EtudiantMatricule = matricule ?? string.Empty,
            EtudiantNomComplet = etudiant is null ? string.Empty : $"{etudiant.Nom} {etudiant.Prenoms}".Trim(),
            ParcoursLibelle = parcours?.Libelle ?? string.Empty,
            AnneeAcademiqueLibelle = annee?.Libelle ?? string.Empty,
            ClassePedagogiqueLibelle = classe?.Libelle ?? string.Empty
        };
}
