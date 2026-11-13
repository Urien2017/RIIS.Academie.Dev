using RIIS.Academic.Application.Etudiants.Dtos;

namespace RIIS.Academic.Application.Dossiers.Dtos;

public class DossierEtudiantAnnuelDto
{
    public long EtudiantId { get; set; }
    public long ParcoursAcademiqueId { get; set; }
    public DossierEtudiantDto? Dossier { get; set; }
    public string Matricule => Dossier?.Matricule
        ?? Academique?.EtudiantMatricule
        ?? Administratif?.EtudiantMatricule
        ?? Financier?.EtudiantMatricule
        ?? string.Empty;
    public EtudiantDto? Etudiant { get; set; }
    public DossierAdministratifDto? Administratif { get; set; }
    public DossierAcademiqueDto? Academique { get; set; }
    public DossierFinancierDto? Financier { get; set; }
    public bool AUnPaiement { get; set; }

    public bool EstComplet => Administratif is not null && Academique is not null;
    public bool AUnDossierFinancier => Financier is not null;
}
