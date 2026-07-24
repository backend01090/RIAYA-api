using Riaya.Api.Common;
using Riaya.Api.Data;
using Riaya.Api.DTOs.DoctorClinicAssignment;
using Riaya.Api.Entities;
using Riaya.Api.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Riaya.Api.Services;

public class DoctorClinicAssignmentService : IDoctorClinicAssignmentService
{
    private readonly AppDbContext _context;

    public DoctorClinicAssignmentService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<DoctorClinicAssignmentDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await ProjectAssignments(_context.DoctorClinicAssignments.AsNoTracking())
            .OrderBy(a => a.DoctorName)
            .ThenBy(a => a.ClinicRoomName)
            .ToListAsync(cancellationToken);
    }

    public async Task<DoctorClinicAssignmentDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await ProjectAssignments(_context.DoctorClinicAssignments.AsNoTracking().Where(a => a.Id == id))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ServiceResult<DoctorClinicAssignmentDto>> CreateAsync(UpsertDoctorClinicAssignmentDto dto, CancellationToken cancellationToken = default)
    {
        var validation = await ValidateAsync(dto, excludedAssignmentId: null, cancellationToken);
        if (!validation.Success)
            return ServiceResult<DoctorClinicAssignmentDto>.Fail(validation.Message, validation.ErrorType, validation.ErrorCode);

        var assignment = new DoctorClinicAssignment
        {
            DoctorId = dto.DoctorId,
            ClinicRoomId = dto.ClinicRoomId,
            IsPrimary = dto.IsPrimary,
            ActiveFrom = dto.ActiveFrom,
            ActiveTo = dto.ActiveTo
        };

        _context.DoctorClinicAssignments.Add(assignment);
        await _context.SaveChangesAsync(cancellationToken);

        return ServiceResult<DoctorClinicAssignmentDto>.Ok(await GetByIdAsync(assignment.Id, cancellationToken), "Doctor clinic assignment created successfully.");
    }

    public async Task<ServiceResult<DoctorClinicAssignmentDto>> UpdateAsync(int id, UpsertDoctorClinicAssignmentDto dto, CancellationToken cancellationToken = default)
    {
        var assignment = await _context.DoctorClinicAssignments.FindAsync(new object[] { id }, cancellationToken);
        if (assignment is null)
            return ServiceResult<DoctorClinicAssignmentDto>.Fail("Doctor clinic assignment not found.", ErrorType.NotFound, "DoctorClinicAssignmentNotFound");

        var validation = await ValidateAsync(dto, id, cancellationToken);
        if (!validation.Success)
            return ServiceResult<DoctorClinicAssignmentDto>.Fail(validation.Message, validation.ErrorType, validation.ErrorCode);

        assignment.DoctorId = dto.DoctorId;
        assignment.ClinicRoomId = dto.ClinicRoomId;
        assignment.IsPrimary = dto.IsPrimary;
        assignment.ActiveFrom = dto.ActiveFrom;
        assignment.ActiveTo = dto.ActiveTo;

        await _context.SaveChangesAsync(cancellationToken);

        return ServiceResult<DoctorClinicAssignmentDto>.Ok(await GetByIdAsync(assignment.Id, cancellationToken), "Doctor clinic assignment updated successfully.");
    }

    public async Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var assignment = await _context.DoctorClinicAssignments.FindAsync(new object[] { id }, cancellationToken);
        if (assignment is null)
            return ServiceResult.Fail("Doctor clinic assignment not found.", ErrorType.NotFound, "DoctorClinicAssignmentNotFound");

        _context.DoctorClinicAssignments.Remove(assignment);
        await _context.SaveChangesAsync(cancellationToken);

        return ServiceResult.Ok("Doctor clinic assignment deleted successfully.");
    }

    private async Task<ServiceResult> ValidateAsync(
        UpsertDoctorClinicAssignmentDto dto,
        int? excludedAssignmentId,
        CancellationToken cancellationToken)
    {
        if (dto.ActiveTo.HasValue && dto.ActiveTo.Value <= dto.ActiveFrom)
            return ServiceResult.Fail("Assignment ActiveTo must be after ActiveFrom.", ErrorType.Validation, "InvalidAssignmentDates");

        var doctorExists = await _context.Doctors.AnyAsync(d => d.Id == dto.DoctorId, cancellationToken);
        if (!doctorExists)
            return ServiceResult.Fail("Invalid doctor id.", ErrorType.Validation, "InvalidDoctor");

        var roomExists = await _context.ClinicRooms.AnyAsync(c => c.Id == dto.ClinicRoomId && c.IsActive, cancellationToken);
        if (!roomExists)
            return ServiceResult.Fail("Invalid active clinic room id.", ErrorType.Validation, "InvalidClinicRoom");

        var newEnd = dto.ActiveTo ?? DateTime.MaxValue;

        var overlappingSameRoomExists = await _context.DoctorClinicAssignments.AnyAsync(a =>
            (!excludedAssignmentId.HasValue || a.Id != excludedAssignmentId.Value) &&
            a.DoctorId == dto.DoctorId &&
            a.ClinicRoomId == dto.ClinicRoomId &&
            a.ActiveFrom < newEnd &&
            (a.ActiveTo ?? DateTime.MaxValue) > dto.ActiveFrom,
            cancellationToken);

        if (overlappingSameRoomExists)
            return ServiceResult.Fail("Doctor already has an overlapping assignment to this clinic room.", ErrorType.Conflict, "DoctorClinicAssignmentOverlap");

        if (dto.IsPrimary)
        {
            var overlappingPrimaryExists = await _context.DoctorClinicAssignments.AnyAsync(a =>
                (!excludedAssignmentId.HasValue || a.Id != excludedAssignmentId.Value) &&
                a.DoctorId == dto.DoctorId &&
                a.IsPrimary &&
                a.ActiveFrom < newEnd &&
                (a.ActiveTo ?? DateTime.MaxValue) > dto.ActiveFrom,
                cancellationToken);

            if (overlappingPrimaryExists)
                return ServiceResult.Fail("Doctor already has a primary clinic room assignment during this period.", ErrorType.Conflict, "DoctorPrimaryClinicOverlap");
        }

        return ServiceResult.Ok();
    }

    private IQueryable<DoctorClinicAssignmentDto> ProjectAssignments(IQueryable<DoctorClinicAssignment> query)
    {
        return query.Select(a => new DoctorClinicAssignmentDto
        {
            Id = a.Id,
            DoctorId = a.DoctorId,
            DoctorName = a.Doctor != null ? a.Doctor.FullName : string.Empty,
            ClinicRoomId = a.ClinicRoomId,
            ClinicRoomName = a.ClinicRoom != null ? a.ClinicRoom.Name : string.Empty,
            IsPrimary = a.IsPrimary,
            ActiveFrom = a.ActiveFrom,
            ActiveTo = a.ActiveTo
        });
    }
}
