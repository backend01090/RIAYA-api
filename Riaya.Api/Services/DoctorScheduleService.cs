using Riaya.Api.Common;
using Riaya.Api.Data;
using Riaya.Api.DTOs.DoctorSchedule;
using Riaya.Api.Entities;
using Riaya.Api.Enums;
using Riaya.Api.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Riaya.Api.Services;

public class DoctorScheduleService : IDoctorScheduleService
{
    private readonly AppDbContext _context;

    public DoctorScheduleService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<DoctorScheduleDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.DoctorSchedules
            .AsNoTracking()
            .Select(s => new DoctorScheduleDto
            {
                Id = s.Id,
                DoctorId = s.DoctorId,
                DoctorName = s.Doctor != null ? s.Doctor.FullName : string.Empty,
                SpecializationName = s.Doctor != null && s.Doctor.Specialization != null ? s.Doctor.Specialization.Name : string.Empty,
                DayOfWeek = s.DayOfWeek,
                StartTime = s.StartTime,
                EndTime = s.EndTime
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<DoctorScheduleDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.DoctorSchedules
            .AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new DoctorScheduleDto
            {
                Id = s.Id,
                DoctorId = s.DoctorId,
                DoctorName = s.Doctor != null ? s.Doctor.FullName : string.Empty,
                SpecializationName = s.Doctor != null && s.Doctor.Specialization != null ? s.Doctor.Specialization.Name : string.Empty,
                DayOfWeek = s.DayOfWeek,
                StartTime = s.StartTime,
                EndTime = s.EndTime
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ServiceResult<DoctorScheduleDto>> CreateAsync(CreateDoctorScheduleDto dto, CancellationToken cancellationToken = default)
    {
        var validation = await ValidateScheduleAsync(dto, excludedScheduleId: null, cancellationToken);
        if (!validation.Success)
            return ServiceResult<DoctorScheduleDto>.Fail(validation.Message, validation.ErrorType, validation.ErrorCode);

        var schedule = new DoctorSchedule
        {
            DoctorId = dto.DoctorId,
            DayOfWeek = dto.DayOfWeek,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime
        };

        _context.DoctorSchedules.Add(schedule);
        await _context.SaveChangesAsync(cancellationToken);

        var created = await GetByIdAsync(schedule.Id, cancellationToken);
        return ServiceResult<DoctorScheduleDto>.Ok(created, "Schedule created successfully.");
    }

    public async Task<ServiceResult<DoctorScheduleDto>> UpdateAsync(int id, CreateDoctorScheduleDto dto, CancellationToken cancellationToken = default)
    {
        var schedule = await _context.DoctorSchedules.FindAsync(new object[] { id }, cancellationToken);
        if (schedule is null)
            return ServiceResult<DoctorScheduleDto>.Fail("Schedule not found.", ErrorType.NotFound, "ScheduleNotFound");

        var validation = await ValidateScheduleAsync(dto, id, cancellationToken);
        if (!validation.Success)
            return ServiceResult<DoctorScheduleDto>.Fail(validation.Message, validation.ErrorType, validation.ErrorCode);

        var futureAppointmentValidation = await ValidateFutureAppointmentsRemainInScheduleAsync(schedule, dto, cancellationToken);
        if (!futureAppointmentValidation.Success)
            return ServiceResult<DoctorScheduleDto>.Fail(futureAppointmentValidation.Message, futureAppointmentValidation.ErrorType, futureAppointmentValidation.ErrorCode);

        schedule.DoctorId = dto.DoctorId;
        schedule.DayOfWeek = dto.DayOfWeek;
        schedule.StartTime = dto.StartTime;
        schedule.EndTime = dto.EndTime;

        await _context.SaveChangesAsync(cancellationToken);

        var updated = await GetByIdAsync(schedule.Id, cancellationToken);
        return ServiceResult<DoctorScheduleDto>.Ok(updated, "Schedule updated successfully.");
    }

    public async Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var schedule = await _context.DoctorSchedules.FindAsync(new object[] { id }, cancellationToken);
        if (schedule is null)
            return ServiceResult.Fail("Schedule not found.", ErrorType.NotFound, "ScheduleNotFound");

        var now = DateTime.Now;
        var futureAppointmentDates = await _context.Appointments
            .AsNoTracking()
            .Where(a =>
                a.DoctorId == schedule.DoctorId &&
                a.AppointmentDate > now &&
                a.Status != AppointmentStatus.Cancelled)
            .Select(a => a.AppointmentDate)
            .ToListAsync(cancellationToken);

        var hasFutureAppointmentsInThisSchedule = futureAppointmentDates.Any(appointmentDate =>
            appointmentDate.DayOfWeek == schedule.DayOfWeek &&
            appointmentDate.TimeOfDay >= schedule.StartTime &&
            appointmentDate.TimeOfDay < schedule.EndTime);

        if (hasFutureAppointmentsInThisSchedule)
            return ServiceResult.Fail("Cannot delete schedule because there are future appointments within this schedule.", ErrorType.BusinessRule, "ScheduleHasFutureAppointments");

        _context.DoctorSchedules.Remove(schedule);
        await _context.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok("Schedule deleted successfully.");
    }

    public async Task<DoctorDailyScheduleDto?> GetDoctorDailyScheduleAsync(int doctorId, DateTime date, CancellationToken cancellationToken = default)
    {
        var doctor = await _context.Doctors
            .AsNoTracking()
            .Where(d => d.Id == doctorId)
            .Select(d => new
            {
                d.Id,
                d.FullName,
                SpecializationName = d.Specialization != null ? d.Specialization.Name : string.Empty
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (doctor is null)
            return null;

        var dayOfWeek = date.DayOfWeek;
        var patientsQuery = _context.Patients.IgnoreQueryFilters();

        var workingSlots = await _context.DoctorSchedules
            .AsNoTracking()
            .Where(s => s.DoctorId == doctorId && s.DayOfWeek == dayOfWeek)
            .OrderBy(s => s.StartTime)
            .Select(s => new DoctorDailyWorkingSlotDto
            {
                DayOfWeek = s.DayOfWeek,
                StartTime = s.StartTime,
                EndTime = s.EndTime
            })
            .ToListAsync(cancellationToken);

        var dateStart = date.Date;
        var dateEnd = dateStart.AddDays(1);

        var appointments = await _context.Appointments
            .AsNoTracking()
            .Where(a => a.DoctorId == doctorId && a.AppointmentDate >= dateStart && a.AppointmentDate < dateEnd)
            .OrderBy(a => a.AppointmentDate)
            .Select(a => new DoctorDailyScheduleItemDto
            {
                AppointmentId = a.Id,
                AppointmentDate = a.AppointmentDate,
                Status = a.Status.ToString(),
                PatientId = a.PatientId,
                PatientName = patientsQuery
                    .Where(p => p.Id == a.PatientId)
                    .Select(p => p.FullName)
                    .FirstOrDefault() ?? string.Empty
            })
            .ToListAsync(cancellationToken);

        return new DoctorDailyScheduleDto
        {
            DoctorId = doctor.Id,
            DoctorName = doctor.FullName,
            SpecializationName = doctor.SpecializationName,
            Date = date.Date,
            IsWorkingDay = workingSlots.Any(),
            WorkingSlots = workingSlots,
            Appointments = appointments
        };
    }

    private async Task<ServiceResult> ValidateScheduleAsync(
        CreateDoctorScheduleDto dto,
        int? excludedScheduleId,
        CancellationToken cancellationToken)
    {
        var doctorExists = await _context.Doctors.AnyAsync(d => d.Id == dto.DoctorId, cancellationToken);
        if (!doctorExists)
            return ServiceResult.Fail("Invalid doctor id.", ErrorType.Validation, "InvalidDoctor");

        if (dto.StartTime >= dto.EndTime)
            return ServiceResult.Fail("Start time must be earlier than end time.", ErrorType.Validation, "InvalidScheduleTimeRange");

        var schedulesForDay = await _context.DoctorSchedules
            .AsNoTracking()
            .Where(s =>
                (!excludedScheduleId.HasValue || s.Id != excludedScheduleId.Value) &&
                s.DoctorId == dto.DoctorId &&
                s.DayOfWeek == dto.DayOfWeek)
            .Select(s => new { s.StartTime, s.EndTime })
            .ToListAsync(cancellationToken);

        var hasOverlap = schedulesForDay.Any(s =>
            dto.StartTime < s.EndTime && dto.EndTime > s.StartTime);

        return hasOverlap
            ? ServiceResult.Fail("This doctor already has an overlapping schedule on this day.", ErrorType.Conflict, "DoctorScheduleOverlap")
            : ServiceResult.Ok();
    }

    private async Task<ServiceResult> ValidateFutureAppointmentsRemainInScheduleAsync(
        DoctorSchedule schedule,
        CreateDoctorScheduleDto dto,
        CancellationToken cancellationToken)
    {
        var now = DateTime.Now;

        var futureAppointmentDates = await _context.Appointments
            .AsNoTracking()
            .Where(a =>
                a.DoctorId == schedule.DoctorId &&
                a.AppointmentDate > now &&
                a.Status != AppointmentStatus.Cancelled)
            .Select(a => a.AppointmentDate)
            .ToListAsync(cancellationToken);

        var hasAffectedFutureAppointments = futureAppointmentDates.Any(appointmentDate =>
            appointmentDate.DayOfWeek == schedule.DayOfWeek &&
            appointmentDate.TimeOfDay >= schedule.StartTime &&
            appointmentDate.TimeOfDay < schedule.EndTime &&
            (
                dto.DoctorId != schedule.DoctorId ||
                dto.DayOfWeek != schedule.DayOfWeek ||
                appointmentDate.TimeOfDay < dto.StartTime ||
                appointmentDate.TimeOfDay >= dto.EndTime
            ));

        return hasAffectedFutureAppointments
            ? ServiceResult.Fail("Cannot update schedule because there are future appointments that would become outside the new working hours.", ErrorType.BusinessRule, "ScheduleHasAffectedFutureAppointments")
            : ServiceResult.Ok();
    }
}
