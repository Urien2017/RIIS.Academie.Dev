using RIIS.Academic.Application.Abstractions.Persistence;
using RIIS.Academic.Application.Authorization.Dtos;
using RIIS.Academic.Domain;

namespace RIIS.Academic.Application.Authorization.Services;

public class UtilisateursService(
    IRepository<Utilisateur> utilisateurs,
    IRepository<Role> roles,
    IRepository<Permission> permissions,
    IRepository<RolePermission> rolePermissions) : IUtilisateursService
{
    public async Task<List<UtilisateurDto>> GetUtilisateursAsync(
        bool inclureInactifs = false,
        CancellationToken cancellationToken = default)
    {
        var userItems = await utilisateurs.ListAsync(cancellationToken);
        var roleItems = await roles.ListAsync(cancellationToken);
        var roleById = roleItems.ToDictionary(x => x.Id);

        return userItems
            .Where(x => inclureInactifs || x.EstActif)
            .OrderBy(x => x.NomUtilisateur)
            .Select(x => ToDto(x, roleById.GetValueOrDefault(x.RoleId)))
            .ToList();
    }

    public async Task<UtilisateurDto?> GetUtilisateurAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        var user = await utilisateurs.GetByIdAsync(id, cancellationToken);
        if (user is null) return null;

        var role = await roles.GetByIdAsync(user.RoleId, cancellationToken);
        return ToDto(user, role);
    }

    public async Task<UtilisateurDto?> AuthenticateAsync(
        string nomUtilisateur,
        string motDePasse,
        CancellationToken cancellationToken = default)
    {
        var user = (await utilisateurs.ListAsync(cancellationToken))
            .FirstOrDefault(x => x.EstActif
                && string.Equals(x.NomUtilisateur, nomUtilisateur.Trim(), StringComparison.OrdinalIgnoreCase));
        if (user is null || !VerifyPassword(motDePasse, user.MotDePasseHash))
        {
            return null;
        }

        var role = await roles.GetByIdAsync(user.RoleId, cancellationToken);
        return ToDto(user, role);
    }

    public async Task<HashSet<string>> GetPermissionRoutesAsync(
        string nomUtilisateur,
        CancellationToken cancellationToken = default)
    {
        var user = (await utilisateurs.ListAsync(cancellationToken))
            .FirstOrDefault(x => x.EstActif
                && string.Equals(x.NomUtilisateur, nomUtilisateur.Trim(), StringComparison.OrdinalIgnoreCase));
        if (user is null) return [];

        var role = (await roles.ListAsync(cancellationToken))
            .FirstOrDefault(x => x.Id == user.RoleId);
        if (role is null) return [];

        var permissionIds = (await rolePermissions.ListAsync(cancellationToken))
            .Where(x => x.RoleId == role.Id)
            .Select(x => x.PermissionId)
            .ToHashSet();
        var permissionItems = await permissions.ListAsync(cancellationToken);

        return permissionItems
            .Where(x => permissionIds.Contains(x.Id))
            .Select(x => x.Route)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public async Task SaveUtilisateurAsync(
        UtilisateurDto dto,
        CancellationToken cancellationToken = default)
    {
        var username = RequireText(dto.NomUtilisateur, "Le nom d'utilisateur est obligatoire.");
        var role = await roles.GetByIdAsync(dto.RoleId, cancellationToken)
            ?? throw new InvalidOperationException("Le rôle sélectionné est introuvable.");
        var allUsers = await utilisateurs.ListAsync(cancellationToken);

        if (allUsers.Any(x => x.Id != dto.Id
            && string.Equals(x.NomUtilisateur, username, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Un utilisateur porte déjà ce nom.");
        }

        Utilisateur user;
        if (dto.Id == 0)
        {
            var password = RequireText(dto.MotDePasse, "Le mot de passe est obligatoire.");
            user = new Utilisateur
            {
                NomUtilisateur = username,
                MotDePasseHash = HashPassword(password),
                RoleId = role.Id,
                EstActif = dto.EstActif
            };
            await utilisateurs.AddAsync(user, cancellationToken);
        }
        else
        {
            user = await utilisateurs.GetByIdAsync(dto.Id, cancellationToken)
                ?? throw new InvalidOperationException("L'utilisateur demandé est introuvable.");
            user.NomUtilisateur = username;
            user.RoleId = role.Id;
            user.EstActif = dto.EstActif;
            user.ModifieLeUtc = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(dto.MotDePasse))
            {
                user.MotDePasseHash = HashPassword(dto.MotDePasse);
            }
        }

        await utilisateurs.SaveChangesAsync(cancellationToken);
        dto.Id = user.Id;
        dto.MotDePasse = string.Empty;
    }

    public async Task DeleteUtilisateurAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        var user = await utilisateurs.GetByIdAsync(id, cancellationToken);
        if (user is null) return;
        if (string.Equals(user.NomUtilisateur, "admin", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("L'utilisateur admin ne peut pas être supprimé.");
        }

        utilisateurs.Delete(user);
        await utilisateurs.SaveChangesAsync(cancellationToken);
    }

    private static UtilisateurDto ToDto(Utilisateur user, Role? role)
        => new()
        {
            Id = user.Id,
            NomUtilisateur = user.NomUtilisateur,
            RoleId = user.RoleId,
            RoleLibelle = role?.Libelle ?? string.Empty,
            EstActif = user.EstActif
        };

    private static string HashPassword(string password)
        => MotDePasseHasher.Hash(password);

    private static bool VerifyPassword(string password, string encodedHash)
    {
        return MotDePasseHasher.Verify(password, encodedHash);
    }

    private static string RequireText(string? value, string message)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? throw new InvalidOperationException(message) : normalized;
    }
}
