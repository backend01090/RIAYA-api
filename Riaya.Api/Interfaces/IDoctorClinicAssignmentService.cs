using Riaya.Api.Common;
using Riaya.Api.DTOs.DoctorClinicAssignment;

namespace Riaya.Api.Interfaces;

public interface IDoctorClinicAssignmentService
{
    Task<List<DoctorClinicAssignmentDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<DoctorClinicAssignmentDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult<DoctorClinicAssignmentDto>> CreateAsync(UpsertDoctorClinicAssignmentDto dto, CancellationToken cancellationToken = default);
    Task<ServiceResult<DoctorClinicAssignmentDto>> UpdateAsync(int id, UpsertDoctorClinicAssignmentDto dto, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
