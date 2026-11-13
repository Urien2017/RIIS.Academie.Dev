namespace RIIS.Academic.Domain;

public class Role
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Libelle { get; set; } = string.Empty;
    public bool EstSysteme { get; set; }
    public bool EstActif { get; set; } = true;
    public DateTime CreeLeUtc { get; set; } = DateTime.UtcNow;

    public ICollection<RolePermission> RolePermissions { get; set; } = [];
}
