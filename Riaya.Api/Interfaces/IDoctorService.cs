using Riaya.Api.Common;
using Riaya.Api.DTOs.Doctor;

namespace Riaya.Api.Interfaces;

public interface IDoctorService
{
    Task<PagedResponse<DoctorDto>> GetAllAsync(DoctorQueryParams queryParams, CancellationToken cancellationToken = default);
    Task<DoctorDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<CurrentDoctorDto?> GetCurrentDoctorAsync(string userId, CancellationToken cancellationToken = default);
    Task<ServiceResult<DoctorDto>> CreateAsync(CreateDoctorDto dto, CancellationToken cancellationToken = default);
    Task<ServiceResult<DoctorDto>> UpdateAsync(int id, CreateDoctorDto dto, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
