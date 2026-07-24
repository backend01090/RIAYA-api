using Riaya.Api.Common;
using Riaya.Api.DTOs.ClinicRoom;

namespace Riaya.Api.Interfaces;

public interface IClinicRoomService
{
    Task<List<ClinicRoomDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ClinicRoomDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult<ClinicRoomDto>> CreateAsync(UpsertClinicRoomDto dto, CancellationToken cancellationToken = default);
    Task<ServiceResult<ClinicRoomDto>> UpdateAsync(int id, UpsertClinicRoomDto dto, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
