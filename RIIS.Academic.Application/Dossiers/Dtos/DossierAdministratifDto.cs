using RIIS.Academic.Domain;

namespace RIIS.Academic.Application.Dossiers.Dtos;

public class DossierAdministratifDto
{
    public long Id { get; set; }
    public long EtudiantId { get; set; }
    public long ParcoursAcademiqueId { get; set; }
    public StatutDossierAdministratif Statut { get; set; } = StatutDossierAdministratif.EnPreparation;
    public DateTime CreeLeUtc { get; set; }
    public string EtudiantMatricule { get; set; } = string.Empty;
    public string EtudiantNomComplet { get; set; } = string.Empty;
    public string ParcoursLibelle { get; set; } = string.Empty;
    public string AnneeAcademiqueLibelle { get; set; } = string.Empty;

    public static DossierAdministratifDto FromEntity(
        DossierAdministratif entity,
        Etudiant? etudiant = null,
        ParcoursAcademique? parcours = null,
        AnneeAcademique? annee = null,
        string? matricule = null)
        => new()
        {
            Id = entity.Id,
            EtudiantId = entity.EtudiantId,
            ParcoursAcademiqueId = entity.ParcoursAcademiqueId,
            Statut = entity.Statut,
            CreeLeUtc = entity.CreeLeUtc,
            EtudiantMatricule = matricule ?? string.Empty,
            EtudiantNomComplet = etudiant is null ? string.Empty : $"{etudiant.Nom} {etudiant.Prenoms}".Trim(),
            ParcoursLibelle = parcours?.Libelle ?? string.Empty,
            AnneeAcademiqueLibelle = annee?.Libelle ?? string.Empty
        };
}
