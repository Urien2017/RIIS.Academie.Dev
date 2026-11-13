namespace RIIS.Academic.Application.Authorization.Dtos;

public class UtilisateurDto
{
    public long Id { get; set; }
    public string NomUtilisateur { get; set; } = string.Empty;
    public string MotDePasse { get; set; } = string.Empty;
    public long RoleId { get; set; }
    public string RoleLibelle { get; set; } = string.Empty;
    public bool EstActif { get; set; } = true;
}
