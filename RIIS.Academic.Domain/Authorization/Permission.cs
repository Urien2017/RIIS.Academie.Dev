namespace RIIS.Academic.Domain;

public class Permission
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Libelle { get; set; } = string.Empty;
    public string? Route { get; set; }
    public string? Icon { get; set; }
    public long? ParentPermissionId { get; set; }
    public int OrdreAffichage { get; set; }
    public bool EstActif { get; set; } = true;

    public Permission? ParentPermission { get; set; }
    public ICollection<Permission> Enfants { get; set; } = [];
    public ICollection<RolePermission> RolePermissions { get; set; } = [];
}
