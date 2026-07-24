using Riaya.Api.Common;
using Riaya.Api.DTOs.Billing;

namespace Riaya.Api.Interfaces;

public interface IMedicalServiceService
{
    Task<List<MedicalServiceDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<MedicalServiceDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult<MedicalServiceDto>> CreateAsync(UpsertMedicalServiceDto dto, CancellationToken cancellationToken = default);
    Task<ServiceResult<MedicalServiceDto>> UpdateAsync(int id, UpsertMedicalServiceDto dto, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
