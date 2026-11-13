using RIIS.Academic.Application.Dashboard.Dtos;

namespace RIIS.Academic.Application.Dashboard.Services;

public interface IDashboardScolariteService
{
    Task<DashboardScolariteDto> GetDashboardScolariteAsync(
        DashboardScolariteFilterDto? filter = null,
        CancellationToken cancellationToken = default);
}
