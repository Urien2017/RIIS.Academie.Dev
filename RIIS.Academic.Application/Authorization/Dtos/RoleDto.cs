namespace RIIS.Academic.Application.Authorization.Dtos;

public class RoleDto
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Libelle { get; set; } = string.Empty;
    public bool EstSysteme { get; set; }
    public bool EstActif { get; set; } = true;
    public List<long> PermissionIds { get; set; } = [];
}
