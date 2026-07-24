using Riaya.Api.Common;
using Riaya.Api.Constants;
using Riaya.Api.Data;
using Riaya.Api.DTOs.Appointment;
using Riaya.Api.Entities;
using Riaya.Api.Enums;
using Riaya.Api.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Riaya.Api.Services;

public class AppointmentService : IAppointmentService
{
    private const int DefaultDurationMinutes = 30;
    private const int MinimumDurationMinutes = 5;
    private const int MaximumDurationMinutes = 240;

    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AppointmentService(
        AppDbContext context,
        ICurrentUserService currentUserService,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _currentUserService = currentUserService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<PagedResponse<AppointmentDto>> GetAllAsync(AppointmentQueryParams queryParams, CancellationToken cancellationToken = default)
    {
        var query = _context.Appointments
            .AsNoTracking()
            .AsQueryable();

        if (IsDoctor())
        {
            var currentDoctorId = await GetCurrentDoctorIdAsync(cancellationToken);
            if (!currentDoctorId.HasValue)
            {
                return new PagedResponse<AppointmentDto>
                {
                    Items = new List<AppointmentDto>(),
                    PageNumber = queryParams.PageNumber,
                    PageSize = queryParams.PageSize,
                    TotalCount = 0
                };
            }

            query = query.Where(a => a.DoctorId == currentDoctorId.Value);
        }
        else if (queryParams.DoctorId.HasValue)
        {
            query = query.Where(a => a.DoctorId == queryParams.DoctorId.Value);
        }

        if (queryParams.PatientId.HasValue)
            query = query.Where(a => a.PatientId == queryParams.PatientId.Value);

        if (!string.IsNullOrWhiteSpace(queryParams.Status) &&
            Enum.TryParse<AppointmentStatus>(queryParams.Status, true, out var parsedStatus))
        {
            query = query.Where(a => a.Status == parsedStatus);
        }

        if (queryParams.Date.HasValue)
        {
            var date = queryParams.Date.Value.Date;
            var nextDate = date.AddDays(1);
            query = query.Where(a => a.AppointmentDate >= date && a.AppointmentDate < nextDate);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await ProjectAppointmentQuery(query)
            .OrderByDescending(a => a.AppointmentDate)
            .Skip((queryParams.PageNumber - 1) * queryParams.PageSize)
            .Take(queryParams.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResponse<AppointmentDto>
        {
            Items = items,
            PageNumber = queryParams.PageNumber,
            PageSize = queryParams.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<AppointmentDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var query = _context.Appointments
            .AsNoTracking()
            .Where(a => a.Id == id);

        if (IsDoctor())
        {
            var currentDoctorId = await GetCurrentDoctorIdAsync(cancellationToken);
            if (!currentDoctorId.HasValue)
                return null;

            query = query.Where(a => a.DoctorId == currentDoctorId.Value);
        }

        return await ProjectAppointmentQuery(query).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ServiceResult<AppointmentDto>> CreateAsync(CreateAppointmentDto dto, CancellationToken cancellationToken = default)
    {
        var durationMinutes = dto.DurationMinutes ?? DefaultDurationMinutes;
        var slotValidation = ValidateRequestedSlot(dto.AppointmentDate, durationMinutes);
        if (!slotValidation.Success)
            return ServiceResult<AppointmentDto>.Fail(slotValidation.Message, slotValidation.ErrorType, slotValidation.ErrorCode);

        if (IsDoctor())
        {
            var currentDoctorId = await GetCurrentDoctorIdAsync(cancellationToken);
            if (!currentDoctorId.HasValue)
                return ServiceResult<AppointmentDto>.Fail("No doctor profile is linked to the current user.", ErrorType.Forbidden, "DoctorProfileMissing");

            if (dto.DoctorId != currentDoctorId.Value)
                return ServiceResult<AppointmentDto>.Fail("You are not allowed to create appointments for another doctor.", ErrorType.Forbidden, "DoctorMismatch");
        }

        var doctorExists = await _context.Doctors.AnyAsync(d => d.Id == dto.DoctorId, cancellationToken);
        if (!doctorExists)
            return ServiceResult<AppointmentDto>.Fail("Invalid doctor id.", ErrorType.Validation, "InvalidDoctor");

        var patientExists = await _context.Patients.AnyAsync(p => p.Id == dto.PatientId, cancellationToken);
        if (!patientExists)
            return ServiceResult<AppointmentDto>.Fail("Invalid patient id.", ErrorType.Validation, "InvalidPatient");

        var clinicRoomValidation = await ValidateClinicRoomAssignmentAsync(dto, durationMinutes, cancellationToken);
        if (!clinicRoomValidation.Success)
            return ServiceResult<AppointmentDto>.Fail(clinicRoomValidation.Message, clinicRoomValidation.ErrorType, clinicRoomValidation.ErrorCode);

        var scheduleValidation = await ValidateDoctorScheduleAsync(
            dto.DoctorId,
            dto.AppointmentDate,
            durationMinutes,
            cancellationToken);

        if (!scheduleValidation.Success)
            return ServiceResult<AppointmentDto>.Fail(scheduleValidation.Message, scheduleValidation.ErrorType, scheduleValidation.ErrorCode);

        var conflictValidation = await ValidateNoAppointmentOverlapAsync(
            dto.DoctorId,
            dto.PatientId,
            dto.ClinicRoomId,
            dto.AppointmentDate,
            durationMinutes,
            excludedAppointmentId: null,
            cancellationToken);

        if (!conflictValidation.Success)
            return ServiceResult<AppointmentDto>.Fail(conflictValidation.Message, conflictValidation.ErrorType, conflictValidation.ErrorCode);

        var appointment = new Appointment
        {
            DoctorId = dto.DoctorId,
            PatientId = dto.PatientId,
            ClinicRoomId = dto.ClinicRoomId,
            AppointmentDate = dto.AppointmentDate,
            DurationMinutes = durationMinutes,
            Status = AppointmentStatus.Pending
        };

        _context.Appointments.Add(appointment);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return ServiceResult<AppointmentDto>.Fail(
                "Appointment conflict detected. The selected time slot is no longer available.",
                ErrorType.Conflict,
                "AppointmentConflict");
        }

        var created = await GetByIdAsync(appointment.Id, cancellationToken);
        return ServiceResult<AppointmentDto>.Ok(created, "Appointment created successfully.");
    }

    public async Task<ServiceResult> ConfirmAsync(int id, CancellationToken cancellationToken = default)
    {
        var appointment = await _context.Appointments.FindAsync(new object[] { id }, cancellationToken);
        if (appointment is null)
            return ServiceResult.Fail("Appointment not found.", ErrorType.NotFound, "AppointmentNotFound");

        var accessResult = await EnsureDoctorCanAccessAppointmentAsync(appointment, "confirm", cancellationToken);
        if (!accessResult.Success)
            return accessResult;

        if (appointment.Status == AppointmentStatus.Cancelled)
            return ServiceResult.Fail("Cancelled appointment cannot be confirmed.", ErrorType.BusinessRule, "CancelledAppointment");

        if (appointment.Status == AppointmentStatus.Completed)
            return ServiceResult.Fail("Completed appointment cannot be confirmed.", ErrorType.BusinessRule, "CompletedAppointment");

        if (appointment.Status == AppointmentStatus.NoShow)
            return ServiceResult.Fail("No-show appointment cannot be confirmed.", ErrorType.BusinessRule, "NoShowAppointment");

        if (appointment.Status == AppointmentStatus.CheckedIn)
            return ServiceResult.Fail("Checked-in appointment cannot be confirmed again.", ErrorType.BusinessRule, "CheckedInAppointment");

        if (appointment.Status == AppointmentStatus.Confirmed)
            return ServiceResult.Fail("Appointment is already confirmed.", ErrorType.BusinessRule, "AlreadyConfirmed");

        appointment.Status = AppointmentStatus.Confirmed;
        await _context.SaveChangesAsync(cancellationToken);

        return ServiceResult.Ok("Appointment confirmed.");
    }

    public async Task<ServiceResult> CancelAsync(int id, CancellationToken cancellationToken = default)
    {
        var appointment = await _context.Appointments.FindAsync(new object[] { id }, cancellationToken);
        if (appointment is null)
            return ServiceResult.Fail("Appointment not found.", ErrorType.NotFound, "AppointmentNotFound");

        var accessResult = await EnsureDoctorCanAccessAppointmentAsync(appointment, "cancel", cancellationToken);
        if (!accessResult.Success)
            return accessResult;

        if (appointment.Status == AppointmentStatus.Completed)
            return ServiceResult.Fail("Completed appointment cannot be cancelled.", ErrorType.BusinessRule, "CompletedAppointment");

        if (appointment.Status == AppointmentStatus.NoShow)
            return ServiceResult.Fail("No-show appointment cannot be cancelled.", ErrorType.BusinessRule, "NoShowAppointment");

        if (appointment.Status == AppointmentStatus.Cancelled)
            return ServiceResult.Fail("Appointment is already cancelled.", ErrorType.BusinessRule, "AlreadyCancelled");

        appointment.Status = AppointmentStatus.Cancelled;
        await _context.SaveChangesAsync(cancellationToken);

        return ServiceResult.Ok("Appointment cancelled.");
    }

    public async Task<ServiceResult> CheckInAsync(int id, CancellationToken cancellationToken = default)
    {
        var appointment = await _context.Appointments.FindAsync(new object[] { id }, cancellationToken);
        if (appointment is null)
            return ServiceResult.Fail("Appointment not found.", ErrorType.NotFound, "AppointmentNotFound");

        var accessResult = await EnsureDoctorCanAccessAppointmentAsync(appointment, "check in", cancellationToken);
        if (!accessResult.Success)
            return accessResult;

        if (appointment.Status == AppointmentStatus.Pending)
            return ServiceResult.Fail("Only confirmed appointments can be checked in.", ErrorType.BusinessRule, "PendingAppointment");

        if (appointment.Status == AppointmentStatus.Cancelled)
            return ServiceResult.Fail("Cancelled appointment cannot be checked in.", ErrorType.BusinessRule, "CancelledAppointment");

        if (appointment.Status == AppointmentStatus.Completed)
            return ServiceResult.Fail("Completed appointment cannot be checked in.", ErrorType.BusinessRule, "CompletedAppointment");

        if (appointment.Status == AppointmentStatus.NoShow)
            return ServiceResult.Fail("No-show appointment cannot be checked in.", ErrorType.BusinessRule, "NoShowAppointment");

        if (appointment.Status == AppointmentStatus.CheckedIn)
            return ServiceResult.Fail("Appointment is already checked in.", ErrorType.BusinessRule, "AlreadyCheckedIn");

        if (appointment.Status != AppointmentStatus.Confirmed)
            return ServiceResult.Fail("Only confirmed appointments can be checked in.", ErrorType.BusinessRule, "InvalidCheckInStatus");

        if (appointment.AppointmentDate.Date > DateTime.Now.Date)
            return ServiceResult.Fail("Appointment cannot be checked in before its scheduled day.", ErrorType.BusinessRule, "EarlyCheckIn");

        appointment.Status = AppointmentStatus.CheckedIn;
        await _context.SaveChangesAsync(cancellationToken);

        return ServiceResult.Ok("Appointment checked in.");
    }

    public async Task<ServiceResult> CompleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var appointment = await _context.Appointments.FindAsync(new object[] { id }, cancellationToken);
        if (appointment is null)
            return ServiceResult.Fail("Appointment not found.", ErrorType.NotFound, "AppointmentNotFound");

        var accessResult = await EnsureDoctorCanAccessAppointmentAsync(appointment, "complete", cancellationToken);
        if (!accessResult.Success)
            return accessResult;

        if (appointment.Status == AppointmentStatus.Cancelled)
            return ServiceResult.Fail("Cancelled appointment cannot be completed.", ErrorType.BusinessRule, "CancelledAppointment");

        if (appointment.Status == AppointmentStatus.NoShow)
            return ServiceResult.Fail("No-show appointment cannot be completed.", ErrorType.BusinessRule, "NoShowAppointment");

        if (appointment.Status == AppointmentStatus.Pending)
            return ServiceResult.Fail("Pending appointment must be confirmed before completion.", ErrorType.BusinessRule, "PendingAppointment");

        if (appointment.AppointmentDate > DateTime.Now)
            return ServiceResult.Fail("Future appointment cannot be completed.", ErrorType.BusinessRule, "FutureAppointment");

        var hasVisit = await _context.Visits.AnyAsync(v => v.AppointmentId == id, cancellationToken);
        if (!hasVisit)
            return ServiceResult.Fail("Appointment cannot be completed without a visit.", ErrorType.BusinessRule, "VisitRequired");

        if (appointment.Status == AppointmentStatus.Completed)
            return ServiceResult.Fail("Appointment is already completed.", ErrorType.BusinessRule, "AlreadyCompleted");

        appointment.Status = AppointmentStatus.Completed;
        await _context.SaveChangesAsync(cancellationToken);

        return ServiceResult.Ok("Appointment completed.");
    }

    public async Task<ServiceResult> MarkNoShowAsync(int id, CancellationToken cancellationToken = default)
    {
        var appointment = await _context.Appointments.FindAsync(new object[] { id }, cancellationToken);
        if (appointment is null)
            return ServiceResult.Fail("Appointment not found.", ErrorType.NotFound, "AppointmentNotFound");

        var accessResult = await EnsureDoctorCanAccessAppointmentAsync(appointment, "mark as no-show", cancellationToken);
        if (!accessResult.Success)
            return accessResult;

        if (appointment.AppointmentDate > DateTime.Now)
            return ServiceResult.Fail("Future appointment cannot be marked as no-show.", ErrorType.BusinessRule, "FutureAppointment");

        if (appointment.Status == AppointmentStatus.Completed)
            return ServiceResult.Fail("Completed appointment cannot be marked as no-show.", ErrorType.BusinessRule, "CompletedAppointment");

        if (appointment.Status == AppointmentStatus.Cancelled)
            return ServiceResult.Fail("Cancelled appointment cannot be marked as no-show.", ErrorType.BusinessRule, "CancelledAppointment");

        if (appointment.Status == AppointmentStatus.NoShow)
            return ServiceResult.Fail("Appointment is already marked as no-show.", ErrorType.BusinessRule, "AlreadyNoShow");

        appointment.Status = AppointmentStatus.NoShow;
        await _context.SaveChangesAsync(cancellationToken);

        return ServiceResult.Ok("Appointment marked as no-show.");
    }

    public async Task<List<UpcomingAppointmentDto>> GetUpcomingAsync(int days = 7, CancellationToken cancellationToken = default)
    {
        if (days <= 0)
            days = 7;

        var now = DateTime.Now;
        var endDate = now.AddDays(days);

        var query = _context.Appointments
            .AsNoTracking()
            .Where(a => a.AppointmentDate >= now &&
                        a.AppointmentDate <= endDate &&
                        a.Status != AppointmentStatus.Cancelled &&
                        a.Status != AppointmentStatus.NoShow);

        if (IsDoctor())
        {
            var currentDoctorId = await GetCurrentDoctorIdAsync(cancellationToken);
            if (!currentDoctorId.HasValue)
                return new List<UpcomingAppointmentDto>();

            query = query.Where(a => a.DoctorId == currentDoctorId.Value);
        }

        var doctorsQuery = _context.Doctors.IgnoreQueryFilters();
        var patientsQuery = _context.Patients.IgnoreQueryFilters();
        var clinicRoomsQuery = _context.ClinicRooms.IgnoreQueryFilters();
        var specializationsQuery = _context.Specializations.IgnoreQueryFilters();

        return await query
            .OrderBy(a => a.AppointmentDate)
            .Select(a => new UpcomingAppointmentDto
            {
                Id = a.Id,
                AppointmentDate = a.AppointmentDate,
                DurationMinutes = a.DurationMinutes,
                Status = a.Status.ToString(),
                DoctorId = a.DoctorId,
                DoctorName = doctorsQuery.Where(d => d.Id == a.DoctorId).Select(d => d.FullName).FirstOrDefault() ?? string.Empty,
                SpecializationName = (
                    from d in doctorsQuery
                    join s in specializationsQuery on d.SpecializationId equals s.Id
                    where d.Id == a.DoctorId
                    select s.Name
                ).FirstOrDefault() ?? string.Empty,
                PatientId = a.PatientId,
                PatientName = patientsQuery.Where(p => p.Id == a.PatientId).Select(p => p.FullName).FirstOrDefault() ?? string.Empty,
                ClinicRoomId = a.ClinicRoomId,
                ClinicRoomName = a.ClinicRoomId.HasValue
                    ? clinicRoomsQuery.Where(c => c.Id == a.ClinicRoomId.Value).Select(c => c.Name).FirstOrDefault()
                    : null
            })
            .ToListAsync(cancellationToken);
    }

    private IQueryable<AppointmentDto> ProjectAppointmentQuery(IQueryable<Appointment> query)
    {
        var doctorsQuery = _context.Doctors.IgnoreQueryFilters();
        var patientsQuery = _context.Patients.IgnoreQueryFilters();
        var clinicRoomsQuery = _context.ClinicRooms.IgnoreQueryFilters();
        var specializationsQuery = _context.Specializations.IgnoreQueryFilters();

        return query.Select(a => new AppointmentDto
        {
            Id = a.Id,
            AppointmentDate = a.AppointmentDate,
            DurationMinutes = a.DurationMinutes,
            Status = a.Status.ToString(),
            DoctorId = a.DoctorId,
            DoctorName = doctorsQuery.Where(d => d.Id == a.DoctorId).Select(d => d.FullName).FirstOrDefault() ?? string.Empty,
            PatientId = a.PatientId,
            PatientName = patientsQuery.Where(p => p.Id == a.PatientId).Select(p => p.FullName).FirstOrDefault() ?? string.Empty,
            ClinicRoomId = a.ClinicRoomId,
            ClinicRoomName = a.ClinicRoomId.HasValue
                ? clinicRoomsQuery.Where(c => c.Id == a.ClinicRoomId.Value).Select(c => c.Name).FirstOrDefault()
                : null,
            SpecializationName = (
                from d in doctorsQuery
                join s in specializationsQuery on d.SpecializationId equals s.Id
                where d.Id == a.DoctorId
                select s.Name
            ).FirstOrDefault() ?? string.Empty
        });
    }

    private ServiceResult ValidateRequestedSlot(DateTime appointmentDate, int durationMinutes)
    {
        if (appointmentDate <= DateTime.Now)
            return ServiceResult.Fail("Appointment date must be in the future.", ErrorType.Validation, "AppointmentInPast");

        if (appointmentDate.Second != 0 || appointmentDate.Millisecond != 0)
            return ServiceResult.Fail("Appointment time must not include seconds or milliseconds.", ErrorType.Validation, "InvalidAppointmentPrecision");

        if (appointmentDate.Ticks % TimeSpan.FromMinutes(15).Ticks != 0)
        {
            return ServiceResult.Fail(
                "Appointment time must be on a 15-minute interval. Example: 09:00, 09:15, 09:30, 09:45.",
                ErrorType.Validation,
                "InvalidAppointmentInterval");
        }

        if (durationMinutes is < MinimumDurationMinutes or > MaximumDurationMinutes)
            return ServiceResult.Fail("Appointment duration must be between 5 and 240 minutes.", ErrorType.Validation, "InvalidAppointmentDuration");

        if (appointmentDate.AddMinutes(durationMinutes).Date != appointmentDate.Date)
            return ServiceResult.Fail("Appointment must start and end on the same day.", ErrorType.Validation, "AppointmentCrossesDay");

        return ServiceResult.Ok();
    }

    private async Task<ServiceResult> ValidateDoctorScheduleAsync(
        int doctorId,
        DateTime appointmentDate,
        int durationMinutes,
        CancellationToken cancellationToken)
    {
        var appointmentDay = appointmentDate.DayOfWeek;
        var appointmentStart = appointmentDate.TimeOfDay;
        var appointmentEnd = appointmentDate.AddMinutes(durationMinutes).TimeOfDay;

        var doctorSchedules = await _context.DoctorSchedules
            .AsNoTracking()
            .Where(s => s.DoctorId == doctorId && s.DayOfWeek == appointmentDay)
            .Select(s => new { s.StartTime, s.EndTime })
            .ToListAsync(cancellationToken);

        var isWithinDoctorSchedule = doctorSchedules.Any(s =>
            appointmentStart >= s.StartTime && appointmentEnd <= s.EndTime);

        return isWithinDoctorSchedule
            ? ServiceResult.Ok()
            : ServiceResult.Fail("Appointment time is outside the doctor's working schedule.", ErrorType.BusinessRule, "OutsideDoctorSchedule");
    }

    private async Task<ServiceResult> ValidateClinicRoomAssignmentAsync(
        CreateAppointmentDto dto,
        int durationMinutes,
        CancellationToken cancellationToken)
    {
        if (!dto.ClinicRoomId.HasValue)
            return ServiceResult.Ok();

        var roomExists = await _context.ClinicRooms.AnyAsync(
            c => c.Id == dto.ClinicRoomId.Value && c.IsActive,
            cancellationToken);

        if (!roomExists)
            return ServiceResult.Fail("Invalid active clinic room id.", ErrorType.Validation, "InvalidClinicRoom");

        var appointmentEnd = dto.AppointmentDate.AddMinutes(durationMinutes);

        var assigned = await _context.DoctorClinicAssignments.AnyAsync(a =>
            a.DoctorId == dto.DoctorId &&
            a.ClinicRoomId == dto.ClinicRoomId.Value &&
            a.ActiveFrom <= dto.AppointmentDate &&
            (!a.ActiveTo.HasValue || a.ActiveTo.Value >= appointmentEnd),
            cancellationToken);

        return assigned
            ? ServiceResult.Ok()
            : ServiceResult.Fail("Doctor is not assigned to the selected clinic room.", ErrorType.BusinessRule, "DoctorClinicRoomMismatch");
    }

    private async Task<ServiceResult> ValidateNoAppointmentOverlapAsync(
        int doctorId,
        int patientId,
        int? clinicRoomId,
        DateTime newStart,
        int durationMinutes,
        int? excludedAppointmentId,
        CancellationToken cancellationToken)
    {
        var newEnd = newStart.AddMinutes(durationMinutes);
        var searchStart = newStart.Date.AddDays(-1);
        var searchEnd = newStart.Date.AddDays(2);

        var activeAppointments = await _context.Appointments
            .AsNoTracking()
            .Where(a => a.Status != AppointmentStatus.Cancelled &&
                        a.AppointmentDate >= searchStart &&
                        a.AppointmentDate < searchEnd &&
                        (!excludedAppointmentId.HasValue || a.Id != excludedAppointmentId.Value) &&
                        (a.DoctorId == doctorId ||
                         a.PatientId == patientId ||
                         (clinicRoomId.HasValue && a.ClinicRoomId == clinicRoomId.Value)))
            .Select(a => new
            {
                a.DoctorId,
                a.PatientId,
                a.ClinicRoomId,
                a.AppointmentDate,
                a.DurationMinutes
            })
            .ToListAsync(cancellationToken);

        var hasDoctorConflict = activeAppointments.Any(a =>
            a.DoctorId == doctorId &&
            newStart < a.AppointmentDate.AddMinutes(a.DurationMinutes) &&
            newEnd > a.AppointmentDate);

        if (hasDoctorConflict)
            return ServiceResult.Fail("This doctor already has an appointment at this time.", ErrorType.Conflict, "DoctorAppointmentOverlap");

        var hasPatientConflict = activeAppointments.Any(a =>
            a.PatientId == patientId &&
            newStart < a.AppointmentDate.AddMinutes(a.DurationMinutes) &&
            newEnd > a.AppointmentDate);

        if (hasPatientConflict)
            return ServiceResult.Fail("This patient already has an appointment at this time.", ErrorType.Conflict, "PatientAppointmentOverlap");

        var hasClinicRoomConflict = clinicRoomId.HasValue && activeAppointments.Any(a =>
            a.ClinicRoomId == clinicRoomId.Value &&
            newStart < a.AppointmentDate.AddMinutes(a.DurationMinutes) &&
            newEnd > a.AppointmentDate);

        if (hasClinicRoomConflict)
            return ServiceResult.Fail("This clinic room already has an appointment at this time.", ErrorType.Conflict, "ClinicRoomAppointmentOverlap");

        return ServiceResult.Ok();
    }

    private async Task<ServiceResult> EnsureDoctorCanAccessAppointmentAsync(
        Appointment appointment,
        string action,
        CancellationToken cancellationToken)
    {
        if (!IsDoctor())
            return ServiceResult.Ok();

        var currentDoctorId = await GetCurrentDoctorIdAsync(cancellationToken);
        if (!currentDoctorId.HasValue || appointment.DoctorId != currentDoctorId.Value)
            return ServiceResult.Fail($"You are not allowed to {action} this appointment.", ErrorType.Forbidden, "DoctorAppointmentMismatch");

        return ServiceResult.Ok();
    }

    private bool IsDoctor()
    {
        return _httpContextAccessor.HttpContext?.User?.IsInRole(AppRoles.Doctor) == true;
    }

    private async Task<int?> GetCurrentDoctorIdAsync(CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrWhiteSpace(userId))
            return null;

        return await _context.Doctors
            .AsNoTracking()
            .Where(d => d.ApplicationUserId == userId)
            .Select(d => (int?)d.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
