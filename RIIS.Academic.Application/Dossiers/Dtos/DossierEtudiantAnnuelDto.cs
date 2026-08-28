using RIIS.Academic.Application.Etudiants.Dtos;

namespace RIIS.Academic.Application.Dossiers.Dtos;

public class DossierEtudiantAnnuelDto
{
    public long EtudiantId { get; set; }
    public long ParcoursAcademiqueId { get; set; }
    public EtudiantDto? Etudiant { get; set; }
    public DossierAdministratifDto? Administratif { get; set; }
    public DossierAcademiqueDto? Academique { get; set; }
    public DossierFinancierDto? Financier { get; set; }

    public bool EstComplet => Administratif is not null && Academique is not null;
    public bool AUnDossierFinancier => Financier is not null;
}
