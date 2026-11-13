using RIIS.Academic.Application.Abstractions.Persistence;
using RIIS.Academic.Application.Authorization;
using RIIS.Academic.Application.Authorization.Dtos;
using RIIS.Academic.Domain;

namespace RIIS.Academic.Application.Authorization.Services;

public class RolesService(
    IRepository<Role> roles,
    IRepository<Permission> permissions,
    IRepository<RolePermission> rolePermissions) : IRolesService
{
    public async Task<List<RoleDto>> GetRolesAsync(bool inclureInactifs = false, CancellationToken cancellationToken = default)
    {
        var roleItems = await roles.ListAsync(cancellationToken);
        var assignments = await rolePermissions.ListAsync(cancellationToken);

        return roleItems
            .Where(x => inclureInactifs || x.EstActif)
            .OrderBy(x => x.Libelle)
            .Select(x => new RoleDto
            {
                Id = x.Id,
                Code = x.Code,
                Libelle = x.Libelle,
                EstSysteme = x.EstSysteme,
                EstActif = x.EstActif,
                PermissionIds = assignments.Where(y => y.RoleId == x.Id).Select(y => y.PermissionId).ToList()
            })
            .ToList();
    }

    public async Task<RoleDto?> GetRoleAsync(long id, CancellationToken cancellationToken = default)
    {
        var role = await roles.GetByIdAsync(id, cancellationToken);
        if (role is null) return null;

        var assignments = await rolePermissions.ListAsync(cancellationToken);
        return new RoleDto
        {
            Id = role.Id,
            Code = role.Code,
            Libelle = role.Libelle,
            EstSysteme = role.EstSysteme,
            EstActif = role.EstActif,
            PermissionIds = assignments.Where(x => x.RoleId == id).Select(x => x.PermissionId).ToList()
        };
    }

    public async Task<List<PermissionDto>> GetPermissionsAsync(bool inclureInactives = false, CancellationToken cancellationToken = default)
    {
        var items = await permissions.ListAsync(cancellationToken);
        return items
            .Where(x => inclureInactives || x.EstActif)
            .OrderBy(x => x.ParentPermissionId)
            .ThenBy(x => x.OrdreAffichage)
            .ThenBy(x => x.Libelle)
            .Select(x => new PermissionDto
            {
                Id = x.Id,
                Code = x.Code,
                Libelle = x.Libelle,
                Route = x.Route,
                Icon = x.Icon,
                ParentPermissionId = x.ParentPermissionId,
                OrdreAffichage = x.OrdreAffichage,
                EstActif = x.EstActif
            })
            .ToList();
    }

    public async Task EnsureMenuPermissionsAsync(CancellationToken cancellationToken = default)
    {
        var existing = await permissions.ListAsync(cancellationToken);
        var byCode = existing.ToDictionary(x => x.Code, StringComparer.OrdinalIgnoreCase);

        foreach (var definition in MenuPermissionCatalog.All)
        {
            if (!byCode.TryGetValue(definition.Code, out var permission))
            {
                permission = new Permission { Code = definition.Code };
                await permissions.AddAsync(permission, cancellationToken);
                byCode[definition.Code] = permission;
            }
            else
            {
                permission = await permissions.GetByIdAsync(permission.Id, cancellationToken)
                    ?? permission;
                byCode[definition.Code] = permission;
            }

            permission.Libelle = definition.Libelle;
            permission.Route = definition.Route;
            permission.Icon = definition.Icon;
            permission.OrdreAffichage = definition.Ordre;
            permission.EstActif = true;
        }

        await permissions.SaveChangesAsync(cancellationToken);

        foreach (var definition in MenuPermissionCatalog.All)
        {
            byCode[definition.Code].ParentPermissionId =
                definition.ParentCode is null ? null : byCode[definition.ParentCode].Id;
        }

        await permissions.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveRoleAsync(RoleDto dto, CancellationToken cancellationToken = default)
    {
        var code = RequireText(dto.Code, "Le code du rôle est obligatoire.");
        var libelle = RequireText(dto.Libelle, "Le libellé du rôle est obligatoire.");
        var existingRoles = await roles.ListAsync(cancellationToken);

        if (existingRoles.Any(x => x.Id != dto.Id && string.Equals(x.Code, code, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Un rôle existe déjà avec ce code.");
        }

        Role role;
        if (dto.Id == 0)
        {
            role = new Role { Code = code, Libelle = libelle, EstActif = dto.EstActif };
            await roles.AddAsync(role, cancellationToken);
        }
        else
        {
            role = await roles.GetByIdAsync(dto.Id, cancellationToken)
                ?? throw new InvalidOperationException("Le rôle demandé est introuvable.");
            role.Code = code;
            role.Libelle = libelle;
            role.EstActif = dto.EstActif;
        }

        await roles.SaveChangesAsync(cancellationToken);
        dto.Id = role.Id;
    }

    public async Task SaveRolePermissionsAsync(long roleId, IReadOnlyCollection<long> permissionIds, CancellationToken cancellationToken = default)
    {
        var role = await roles.GetByIdAsync(roleId, cancellationToken)
            ?? throw new InvalidOperationException("Le rôle demandé est introuvable.");
        var knownPermissionIds = (await permissions.ListAsync(cancellationToken)).Select(x => x.Id).ToHashSet();
        var selectedIds = permissionIds.Where(knownPermissionIds.Contains).ToHashSet();
        var currentAssignments = (await rolePermissions.ListAsync(cancellationToken)).Where(x => x.RoleId == role.Id).ToList();

        foreach (var assignment in currentAssignments.Where(x => !selectedIds.Contains(x.PermissionId)))
        {
            rolePermissions.Delete(assignment);
        }

        var currentIds = currentAssignments.Select(x => x.PermissionId).ToHashSet();
        foreach (var permissionId in selectedIds.Except(currentIds))
        {
            await rolePermissions.AddAsync(new RolePermission { RoleId = role.Id, PermissionId = permissionId }, cancellationToken);
        }

        await rolePermissions.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteRoleAsync(long id, CancellationToken cancellationToken = default)
    {
        var role = await roles.GetByIdAsync(id, cancellationToken);
        if (role is null) return;
        if (role.EstSysteme)
        {
            throw new InvalidOperationException("Un rôle système ne peut pas être supprimé.");
        }

        roles.Delete(role);
        await roles.SaveChangesAsync(cancellationToken);
    }

    private static string RequireText(string? value, string message)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? throw new InvalidOperationException(message) : normalized;
    }
}
