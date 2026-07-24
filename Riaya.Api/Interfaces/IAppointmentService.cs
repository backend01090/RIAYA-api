using Riaya.Api.Common;
using Riaya.Api.DTOs.Appointment;

namespace Riaya.Api.Interfaces;

public interface IAppointmentService
{
    Task<PagedResponse<AppointmentDto>> GetAllAsync(AppointmentQueryParams queryParams, CancellationToken cancellationToken = default);
    Task<AppointmentDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult<AppointmentDto>> CreateAsync(CreateAppointmentDto dto, CancellationToken cancellationToken = default);
    Task<ServiceResult> ConfirmAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult> CheckInAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult> CancelAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult> CompleteAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult> MarkNoShowAsync(int id, CancellationToken cancellationToken = default);
    Task<List<UpcomingAppointmentDto>> GetUpcomingAsync(int days = 7, CancellationToken cancellationToken = default);
}

