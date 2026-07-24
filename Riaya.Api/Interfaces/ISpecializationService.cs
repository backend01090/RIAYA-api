using Riaya.Api.Common;
using Riaya.Api.DTOs.Specialization;

namespace Riaya.Api.Interfaces;

public interface ISpecializationService
{
    Task<List<SpecializationDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<SpecializationDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult<SpecializationDto>> CreateAsync(CreateSpecializationDto dto, CancellationToken cancellationToken = default);
    Task<ServiceResult<SpecializationDto>> UpdateAsync(int id, CreateSpecializationDto dto, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
