using Riaya.Api.Common;
using Riaya.Api.DTOs.Prescription;

namespace Riaya.Api.Interfaces;

public interface IPrescriptionService
{
    Task<PagedResponse<PrescriptionDto>> GetAllAsync(PrescriptionQueryParams queryParams, CancellationToken cancellationToken = default);
    Task<PrescriptionDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult<PrescriptionDto>> CreateAsync(CreatePrescriptionDto dto, CancellationToken cancellationToken = default);
    Task<ServiceResult<PrescriptionDto>> UpdateAsync(int id, UpdatePrescriptionDto dto, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

