using Riaya.Api.Common;
using Riaya.Api.DTOs.DoctorSchedule;

namespace Riaya.Api.Interfaces;

public interface IDoctorScheduleService
{
    Task<List<DoctorScheduleDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<DoctorScheduleDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult<DoctorScheduleDto>> CreateAsync(CreateDoctorScheduleDto dto, CancellationToken cancellationToken = default);
    Task<ServiceResult<DoctorScheduleDto>> UpdateAsync(int id, CreateDoctorScheduleDto dto, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<DoctorDailyScheduleDto?> GetDoctorDailyScheduleAsync(int doctorId, DateTime date, CancellationToken cancellationToken = default);
}
