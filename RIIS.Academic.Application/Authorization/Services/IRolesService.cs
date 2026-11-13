using RIIS.Academic.Application.Authorization.Dtos;

namespace RIIS.Academic.Application.Authorization.Services;

public interface IRolesService
{
    Task<List<RoleDto>> GetRolesAsync(bool inclureInactifs = false, CancellationToken cancellationToken = default);
    Task<RoleDto?> GetRoleAsync(long id, CancellationToken cancellationToken = default);
    Task<List<PermissionDto>> GetPermissionsAsync(bool inclureInactives = false, CancellationToken cancellationToken = default);
    Task EnsureMenuPermissionsAsync(CancellationToken cancellationToken = default);
    Task SaveRoleAsync(RoleDto dto, CancellationToken cancellationToken = default);
    Task SaveRolePermissionsAsync(long roleId, IReadOnlyCollection<long> permissionIds, CancellationToken cancellationToken = default);
    Task DeleteRoleAsync(long id, CancellationToken cancellationToken = default);
}
