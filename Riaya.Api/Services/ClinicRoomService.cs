using Riaya.Api.Common;
using Riaya.Api.Data;
using Riaya.Api.DTOs.ClinicRoom;
using Riaya.Api.Entities;
using Riaya.Api.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Riaya.Api.Services;

public class ClinicRoomService : IClinicRoomService
{
    private readonly AppDbContext _context;

    public ClinicRoomService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<ClinicRoomDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await ProjectClinicRooms(_context.ClinicRooms.AsNoTracking())
            .OrderBy(c => c.RoomNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<ClinicRoomDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await ProjectClinicRooms(_context.ClinicRooms.AsNoTracking().Where(c => c.Id == id))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ServiceResult<ClinicRoomDto>> CreateAsync(UpsertClinicRoomDto dto, CancellationToken cancellationToken = default)
    {
        var validation = await ValidateAsync(dto, excludedRoomId: null, cancellationToken);
        if (!validation.Success)
            return ServiceResult<ClinicRoomDto>.Fail(validation.Message, validation.ErrorType, validation.ErrorCode);

        var room = new ClinicRoom
        {
            Name = dto.Name.Trim(),
            RoomNumber = dto.RoomNumber.Trim(),
            DepartmentId = dto.DepartmentId,
            IsActive = dto.IsActive
        };

        _context.ClinicRooms.Add(room);
        await _context.SaveChangesAsync(cancellationToken);

        return ServiceResult<ClinicRoomDto>.Ok(await GetByIdAsync(room.Id, cancellationToken), "Clinic room created successfully.");
    }

    public async Task<ServiceResult<ClinicRoomDto>> UpdateAsync(int id, UpsertClinicRoomDto dto, CancellationToken cancellationToken = default)
    {
        var room = await _context.ClinicRooms.FindAsync(new object[] { id }, cancellationToken);
        if (room is null)
            return ServiceResult<ClinicRoomDto>.Fail("Clinic room not found.", ErrorType.NotFound, "ClinicRoomNotFound");

        var validation = await ValidateAsync(dto, id, cancellationToken);
        if (!validation.Success)
            return ServiceResult<ClinicRoomDto>.Fail(validation.Message, validation.ErrorType, validation.ErrorCode);

        room.Name = dto.Name.Trim();
        room.RoomNumber = dto.RoomNumber.Trim();
        room.DepartmentId = dto.DepartmentId;
        room.IsActive = dto.IsActive;

        await _context.SaveChangesAsync(cancellationToken);

        return ServiceResult<ClinicRoomDto>.Ok(await GetByIdAsync(room.Id, cancellationToken), "Clinic room updated successfully.");
    }

    public async Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var room = await _context.ClinicRooms.FindAsync(new object[] { id }, cancellationToken);
        if (room is null)
            return ServiceResult.Fail("Clinic room not found.", ErrorType.NotFound, "ClinicRoomNotFound");

        var hasAssignments = await _context.DoctorClinicAssignments.AnyAsync(a => a.ClinicRoomId == id, cancellationToken);
        if (hasAssignments)
            return ServiceResult.Fail("Cannot delete clinic room because doctor assignments are linked to it.", ErrorType.BusinessRule, "ClinicRoomHasAssignments");

        var hasAppointments = await _context.Appointments.AnyAsync(a => a.ClinicRoomId == id, cancellationToken);
        if (hasAppointments)
            return ServiceResult.Fail("Cannot delete clinic room because appointments are linked to it.", ErrorType.BusinessRule, "ClinicRoomHasAppointments");

        _context.ClinicRooms.Remove(room);
        await _context.SaveChangesAsync(cancellationToken);

        return ServiceResult.Ok("Clinic room deleted successfully.");
    }

    private async Task<ServiceResult> ValidateAsync(
        UpsertClinicRoomDto dto,
        int? excludedRoomId,
        CancellationToken cancellationToken)
    {
        var departmentExists = await _context.Departments
            .AnyAsync(d => d.Id == dto.DepartmentId && d.IsActive, cancellationToken);

        if (!departmentExists)
            return ServiceResult.Fail("Active department is required for a clinic room.", ErrorType.Validation, "InvalidDepartment");

        var roomNumber = dto.RoomNumber.Trim();
        var duplicateExists = await _context.ClinicRooms.AnyAsync(c =>
            (!excludedRoomId.HasValue || c.Id != excludedRoomId.Value) &&
            c.IsActive &&
            c.RoomNumber.ToLower() == roomNumber.ToLower(),
            cancellationToken);

        if (duplicateExists && dto.IsActive)
            return ServiceResult.Fail("Active clinic room number already exists.", ErrorType.Conflict, "ClinicRoomNumberExists");

        return ServiceResult.Ok();
    }

    private IQueryable<ClinicRoomDto> ProjectClinicRooms(IQueryable<ClinicRoom> query)
    {
        return query.Select(c => new ClinicRoomDto
        {
            Id = c.Id,
            Name = c.Name,
            RoomNumber = c.RoomNumber,
            DepartmentId = c.DepartmentId,
            DepartmentName = c.Department != null ? c.Department.Name : string.Empty,
            IsActive = c.IsActive
        });
    }
}
