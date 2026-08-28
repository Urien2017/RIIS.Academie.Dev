namespace RIIS.Academic.Application.Releves.Dtos;

public class ReleveNoteEtudiantDisponibleDto
{
    public long DossierAcademiqueId { get; set; }
    public long InscriptionId
    {
        get => DossierAcademiqueId;
        set => DossierAcademiqueId = value;
    }
    public string Matricule { get; set; } = string.Empty;
    public string EtudiantNomComplet { get; set; } = string.Empty;
    public string ParcoursLibelle { get; set; } = string.Empty;
    public string NiveauLibelle { get; set; } = string.Empty;
    public string AnneeAcademiqueLibelle { get; set; } = string.Empty;
}
