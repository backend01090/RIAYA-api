using Riaya.Api.Common;
using Riaya.Api.Constants;
using Riaya.Api.Data;
using Riaya.Api.DTOs.Doctor;
using Riaya.Api.Entities;
using Riaya.Api.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Riaya.Api.Services;

public class DoctorService : IDoctorService
{
    private readonly AppDbContext _context;

    public DoctorService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResponse<DoctorDto>> GetAllAsync(DoctorQueryParams queryParams, CancellationToken cancellationToken = default)
    {
        var query = _context.Doctors
            .Include(d => d.Specialization)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(queryParams.Search))
        {
            var search = queryParams.Search.Trim().ToLower();

            query = query.Where(d =>
                d.FullName.ToLower().Contains(search) ||
                d.PhoneNumber.ToLower().Contains(search) ||
                (d.Specialization != null && d.Specialization.Name.ToLower().Contains(search)));
        }

        if (queryParams.SpecializationId.HasValue)
        {
            query = query.Where(d => d.SpecializationId == queryParams.SpecializationId.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(d => d.FullName)
            .Skip((queryParams.PageNumber - 1) * queryParams.PageSize)
            .Take(queryParams.PageSize)
            .Select(d => new DoctorDto
            {
                Id = d.Id,
                FullName = d.FullName,
                PhoneNumber = d.PhoneNumber,
                SpecializationId = d.SpecializationId,
                SpecializationName = d.Specialization != null ? d.Specialization.Name : string.Empty
            })
            .ToListAsync(cancellationToken);

        return new PagedResponse<DoctorDto>
        {
            Items = items,
            PageNumber = queryParams.PageNumber,
            PageSize = queryParams.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<DoctorDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Doctors
            .Include(d => d.Specialization)
            .Where(d => d.Id == id)
            .Select(d => new DoctorDto
            {
                Id = d.Id,
                FullName = d.FullName,
                PhoneNumber = d.PhoneNumber,
                SpecializationId = d.SpecializationId,
                SpecializationName = d.Specialization != null ? d.Specialization.Name : string.Empty
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<CurrentDoctorDto?> GetCurrentDoctorAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.Doctors
            .Include(d => d.ApplicationUser)
            .Include(d => d.Specialization)
            .Where(d => d.ApplicationUserId == userId)
            .Select(d => new CurrentDoctorDto
            {
                DoctorId = d.Id,
                UserId = d.ApplicationUserId!,
                FullName = d.FullName,
                Email = d.ApplicationUser != null && d.ApplicationUser.Email != null
                    ? d.ApplicationUser.Email
                    : string.Empty,
                SpecializationId = d.SpecializationId,
                SpecializationName = d.Specialization != null ? d.Specialization.Name : null,
                IsActive = !d.IsDeleted
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ServiceResult<DoctorDto>> CreateAsync(CreateDoctorDto dto, CancellationToken cancellationToken = default)
    {
        var validation = await ValidateDoctorAsync(dto, excludedDoctorId: null, cancellationToken);
        if (!validation.Success)
            return ServiceResult<DoctorDto>.Fail(validation.Message, validation.ErrorType, validation.ErrorCode);

        var doctor = new Doctor
        {
            FullName = dto.FullName.Trim(),
            PhoneNumber = dto.PhoneNumber.Trim(),
            SpecializationId = dto.SpecializationId,
            ApplicationUserId = NormalizeApplicationUserId(dto.ApplicationUserId)
        };

        _context.Doctors.Add(doctor);
        await _context.SaveChangesAsync(cancellationToken);

        return ServiceResult<DoctorDto>.Ok(await GetByIdAsync(doctor.Id, cancellationToken), "Doctor created successfully.");
    }

    public async Task<ServiceResult<DoctorDto>> UpdateAsync(int id, CreateDoctorDto dto, CancellationToken cancellationToken = default)
    {
        var doctor = await _context.Doctors.FindAsync(new object[] { id }, cancellationToken);
        if (doctor is null)
            return ServiceResult<DoctorDto>.Fail("Doctor not found.", ErrorType.NotFound, "DoctorNotFound");

        var validation = await ValidateDoctorAsync(dto, id, cancellationToken);
        if (!validation.Success)
            return ServiceResult<DoctorDto>.Fail(validation.Message, validation.ErrorType, validation.ErrorCode);

        doctor.FullName = dto.FullName.Trim();
        doctor.PhoneNumber = dto.PhoneNumber.Trim();
        doctor.SpecializationId = dto.SpecializationId;
        doctor.ApplicationUserId = NormalizeApplicationUserId(dto.ApplicationUserId);

        await _context.SaveChangesAsync(cancellationToken);

        return ServiceResult<DoctorDto>.Ok(await GetByIdAsync(doctor.Id, cancellationToken), "Doctor updated successfully.");
    }

    public async Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var doctor = await _context.Doctors.FindAsync(new object[] { id }, cancellationToken);
        if (doctor is null)
            return ServiceResult.Fail("Doctor not found.", ErrorType.NotFound, "DoctorNotFound");

        var hasSchedules = await _context.DoctorSchedules.AnyAsync(s => s.DoctorId == id, cancellationToken);
        if (hasSchedules)
            return ServiceResult.Fail("Cannot delete doctor because there are schedules linked to this doctor.", ErrorType.BusinessRule, "DoctorHasSchedules");

        var hasAppointments = await _context.Appointments.AnyAsync(a => a.DoctorId == id, cancellationToken);
        if (hasAppointments)
            return ServiceResult.Fail("Cannot delete doctor because there are appointments linked to this doctor.", ErrorType.BusinessRule, "DoctorHasAppointments");

        _context.Doctors.Remove(doctor);
        await _context.SaveChangesAsync(cancellationToken);

        return ServiceResult.Ok("Doctor deleted successfully.");
    }

    private async Task<ServiceResult> ValidateDoctorAsync(
        CreateDoctorDto dto,
        int? excludedDoctorId,
        CancellationToken cancellationToken)
    {
        var phoneNumber = dto.PhoneNumber.Trim();
        var applicationUserId = NormalizeApplicationUserId(dto.ApplicationUserId);

        var specializationExists = await _context.Specializations.AnyAsync(s => s.Id == dto.SpecializationId, cancellationToken);
        if (!specializationExists)
            return ServiceResult.Fail("Invalid specialization id.", ErrorType.Validation, "InvalidSpecialization");

        var phoneExists = await _context.Doctors.AnyAsync(d =>
            (!excludedDoctorId.HasValue || d.Id != excludedDoctorId.Value) &&
            d.PhoneNumber.ToLower() == phoneNumber.ToLower(),
            cancellationToken);

        if (phoneExists)
            return ServiceResult.Fail("Doctor phone number already exists.", ErrorType.Conflict, "DoctorPhoneExists");

        if (!string.IsNullOrWhiteSpace(applicationUserId))
        {
            var userExists = await _context.Users.AnyAsync(u => u.Id == applicationUserId, cancellationToken);
            if (!userExists)
                return ServiceResult.Fail("Invalid application user id.", ErrorType.Validation, "InvalidApplicationUser");

            var userIsDoctor = await UserHasDoctorRoleAsync(applicationUserId, cancellationToken);
            if (!userIsDoctor)
                return ServiceResult.Fail("Application user must have the Doctor role before linking to a doctor profile.", ErrorType.BusinessRule, "ApplicationUserIsNotDoctor");

            var userAlreadyLinked = await _context.Doctors.AnyAsync(d =>
                (!excludedDoctorId.HasValue || d.Id != excludedDoctorId.Value) &&
                d.ApplicationUserId == applicationUserId,
                cancellationToken);

            if (userAlreadyLinked)
                return ServiceResult.Fail("This user is already linked to another doctor.", ErrorType.Conflict, "DoctorUserAlreadyLinked");
        }

        return ServiceResult.Ok();
    }

    private async Task<bool> UserHasDoctorRoleAsync(string applicationUserId, CancellationToken cancellationToken)
    {
        return await (
            from userRole in _context.UserRoles
            join role in _context.Roles on userRole.RoleId equals role.Id
            where userRole.UserId == applicationUserId && role.Name == AppRoles.Doctor
            select userRole.UserId)
            .AnyAsync(cancellationToken);
    }

    private static string? NormalizeApplicationUserId(string? applicationUserId)
    {
        return string.IsNullOrWhiteSpace(applicationUserId)
            ? null
            : applicationUserId.Trim();
    }
}
