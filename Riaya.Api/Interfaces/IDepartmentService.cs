using Riaya.Api.Common;
using Riaya.Api.DTOs.Department;

namespace Riaya.Api.Interfaces;

public interface IDepartmentService
{
    Task<List<DepartmentDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<DepartmentDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult<DepartmentDto>> CreateAsync(UpsertDepartmentDto dto, CancellationToken cancellationToken = default);
    Task<ServiceResult<DepartmentDto>> UpdateAsync(int id, UpsertDepartmentDto dto, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
