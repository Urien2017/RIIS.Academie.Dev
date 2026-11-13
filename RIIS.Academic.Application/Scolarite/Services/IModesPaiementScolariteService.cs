using RIIS.Academic.Application.Scolarite.Dtos;

namespace RIIS.Academic.Application.Scolarite.Services;

public interface IModesPaiementScolariteService
{
    Task<List<ModePaiementScolariteDto>> GetModesPaiementScolariteAsync(
        bool inclureInactifs = false,
        CancellationToken cancellationToken = default);

    ModePaiementScolariteDto CreateDefaultModePaiementScolarite();

    string GenerateCodeFromLibelle(string? libelle);

    Task SaveModePaiementScolariteAsync(
        ModePaiementScolariteDto dto,
        CancellationToken cancellationToken = default);

    Task DeleteModePaiementScolariteAsync(
        long id,
        CancellationToken cancellationToken = default);
}
