using RIIS.Academic.Application.Authorization.Dtos;

namespace RIIS.Academic.Application.Authorization.Services;

public interface IUtilisateursService
{
    Task<List<UtilisateurDto>> GetUtilisateursAsync(
        bool inclureInactifs = false,
        CancellationToken cancellationToken = default);

    Task<UtilisateurDto?> GetUtilisateurAsync(
        long id,
        CancellationToken cancellationToken = default);

    Task<UtilisateurDto?> AuthenticateAsync(
        string nomUtilisateur,
        string motDePasse,
        CancellationToken cancellationToken = default);

    Task<HashSet<string>> GetPermissionRoutesAsync(
        string nomUtilisateur,
        CancellationToken cancellationToken = default);

    Task SaveUtilisateurAsync(
        UtilisateurDto dto,
        CancellationToken cancellationToken = default);

    Task DeleteUtilisateurAsync(
        long id,
        CancellationToken cancellationToken = default);
}
