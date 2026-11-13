namespace RIIS.Academic.Domain;

public class Utilisateur
{
    public long Id { get; set; }
    public string NomUtilisateur { get; set; } = string.Empty;
    public string MotDePasseHash { get; set; } = string.Empty;
    public long RoleId { get; set; }
    public bool EstActif { get; set; } = true;
    public DateTime CreeLeUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ModifieLeUtc { get; set; }

    public Role Role { get; set; } = null!;
}
