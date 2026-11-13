namespace RIIS.Academic.Application.Authorization.Dtos;

public class PermissionDto
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Libelle { get; set; } = string.Empty;
    public string? Route { get; set; }
    public string? Icon { get; set; }
    public long? ParentPermissionId { get; set; }
    public int OrdreAffichage { get; set; }
    public bool EstActif { get; set; } = true;
}
