using Riaya.Api.Common;
using Riaya.Api.DTOs.Visit;

namespace Riaya.Api.Interfaces;

public interface IVisitService
{
    Task<PagedResponse<VisitDto>> GetAllAsync(VisitQueryParams queryParams, CancellationToken cancellationToken = default);
    Task<VisitDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult<VisitDto>> CreateAsync(CreateVisitDto dto, CancellationToken cancellationToken = default);
    Task<ServiceResult<VisitDto>> UpdateAsync(int id, UpdateVisitDto dto, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

