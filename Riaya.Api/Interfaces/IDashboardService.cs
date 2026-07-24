using Riaya.Api.DTOs.Dashboard;

namespace Riaya.Api.Interfaces;

public interface IDashboardService
{
    Task<DashboardOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default);
}

