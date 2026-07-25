using Riaya.Api.Common;
using Riaya.Api.Constants;
using Riaya.Api.Data;
using Riaya.Api.DTOs.Visit;
using Riaya.Api.Entities;
using Riaya.Api.Enums;
using Riaya.Api.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Riaya.Api.Services;

public class VisitService : IVisitService
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public VisitService(
        AppDbContext context,
        ICurrentUserService currentUserService,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _currentUserService = currentUserService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<PagedResponse<VisitDto>> GetAllAsync(VisitQueryParams queryParams, CancellationToken cancellationToken = default)
    {
        var query = _context.Visits
            .AsNoTracking()
            .AsQueryable();

        if (IsDoctor())
        {
            var currentDoctorId = await GetCurrentDoctorIdAsync(cancellationToken);
            if (!currentDoctorId.HasValue)
            {
                return new PagedResponse<VisitDto>
                {
                    Items = new List<VisitDto>(),
                    PageNumber = queryParams.PageNumber,
                    PageSize = queryParams.PageSize,
                    TotalCount = 0
                };
            }

            query = query.Where(v => v.Appointment != null && v.Appointment.DoctorId == currentDoctorId.Value);
        }
        else if (queryParams.DoctorId.HasValue)
        {
            query = query.Where(v => v.Appointment != null && v.Appointment.DoctorId == queryParams.DoctorId.Value);
        }

        if (queryParams.PatientId.HasValue)
            query = query.Where(v => v.Appointment != null && v.Appointment.PatientId == queryParams.PatientId.Value);

        if (queryParams.Date.HasValue)
        {
            var date = queryParams.Date.Value.Date;
            var nextDate = date.AddDays(1);
            query = query.Where(v =>
                v.Appointment != null &&
                v.Appointment.AppointmentDate >= date &&
                v.Appointment.AppointmentDate < nextDate);
        }

        if (!string.IsNullOrWhiteSpace(queryParams.Search))
        {
            var search = queryParams.Search.Trim();
            var canSearchClinicalData = CanReadClinicalData();
            var parsedId = int.TryParse(search, out var searchId) ? searchId : (int?)null;

            query = query.Where(v =>
                (canSearchClinicalData && (
                    v.Symptoms.Contains(search) ||
                    v.Diagnosis.Contains(search) ||
                    v.Notes.Contains(search))) ||
                (parsedId.HasValue && (v.Id == parsedId.Value || v.AppointmentId == parsedId.Value)) ||
                (v.Appointment != null && v.Appointment.Patient != null && (
                    v.Appointment.Patient.FullName.Contains(search) ||
                    v.Appointment.Patient.PhoneNumber.Contains(search))) ||
                (v.Appointment != null && v.Appointment.Doctor != null && v.Appointment.Doctor.FullName.Contains(search)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await ProjectVisitQuery(query)
            .OrderByDescending(v => v.AppointmentDate)
            .Skip((queryParams.PageNumber - 1) * queryParams.PageSize)
            .Take(queryParams.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResponse<VisitDto>
        {
            Items = items,
            PageNumber = queryParams.PageNumber,
            PageSize = queryParams.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<VisitDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var query = _context.Visits
            .AsNoTracking()
            .Where(v => v.Id == id);

        if (IsDoctor())
        {
            var currentDoctorId = await GetCurrentDoctorIdAsync(cancellationToken);
            if (!currentDoctorId.HasValue)
                return null;

            query = query.Where(v => v.Appointment != null && v.Appointment.DoctorId == currentDoctorId.Value);
        }

        return await ProjectVisitQuery(query).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ServiceResult<VisitDto>> CreateAsync(CreateVisitDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Symptoms))
            return ServiceResult<VisitDto>.Fail("Symptoms are required.", ErrorType.Validation, "SymptomsRequired");

        if (string.IsNullOrWhiteSpace(dto.Diagnosis))
            return ServiceResult<VisitDto>.Fail("Diagnosis is required.", ErrorType.Validation, "DiagnosisRequired");

        var appointment = await _context.Appointments.FirstOrDefaultAsync(a => a.Id == dto.AppointmentId, cancellationToken);
        if (appointment is null)
            return ServiceResult<VisitDto>.Fail("Invalid appointment id.", ErrorType.Validation, "InvalidAppointment");

        var accessResult = await EnsureDoctorCanAccessAppointmentAsync(
            appointment,
            "You are not allowed to create a visit for this appointment.",
            cancellationToken);
        if (!accessResult.Success)
            return ServiceResult<VisitDto>.Fail(accessResult.Message, accessResult.ErrorType, accessResult.ErrorCode);

        if (appointment.Status == AppointmentStatus.Cancelled)
            return ServiceResult<VisitDto>.Fail("Cannot create visit for a cancelled appointment.", ErrorType.BusinessRule, "CancelledAppointment");

        if (appointment.Status == AppointmentStatus.Pending)
            return ServiceResult<VisitDto>.Fail("Appointment should be confirmed before creating a visit.", ErrorType.BusinessRule, "PendingAppointment");

        if (appointment.Status == AppointmentStatus.NoShow)
            return ServiceResult<VisitDto>.Fail("Cannot create visit for a no-show appointment.", ErrorType.BusinessRule, "NoShowAppointment");

        if (appointment.AppointmentDate > DateTime.UtcNow)
            return ServiceResult<VisitDto>.Fail("Cannot create visit before appointment time.", ErrorType.BusinessRule, "FutureAppointment");

        var visitExists = await _context.Visits.AnyAsync(v => v.AppointmentId == dto.AppointmentId, cancellationToken);
        if (visitExists)
            return ServiceResult<VisitDto>.Fail("This appointment already has a visit.", ErrorType.Conflict, "VisitAlreadyExists");

        var visit = new Visit
        {
            AppointmentId = dto.AppointmentId,
            Symptoms = dto.Symptoms.Trim(),
            Diagnosis = dto.Diagnosis.Trim(),
            Notes = dto.Notes?.Trim() ?? string.Empty
        };

        _context.Visits.Add(visit);
        appointment.Status = AppointmentStatus.Completed;

        await _context.SaveChangesAsync(cancellationToken);

        var created = await GetByIdAsync(visit.Id, cancellationToken);
        return ServiceResult<VisitDto>.Ok(created, "Visit created successfully.");
    }

    public async Task<ServiceResult<VisitDto>> UpdateAsync(int id, UpdateVisitDto dto, CancellationToken cancellationToken = default)
    {
        var visit = await _context.Visits
            .Include(v => v.Appointment)
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

        if (visit is null)
            return ServiceResult<VisitDto>.Fail("Visit not found.", ErrorType.NotFound, "VisitNotFound");

        var accessResult = await EnsureDoctorCanAccessAppointmentAsync(
            visit.Appointment,
            "You are not allowed to update this visit.",
            cancellationToken);
        if (!accessResult.Success)
            return ServiceResult<VisitDto>.Fail(accessResult.Message, accessResult.ErrorType, accessResult.ErrorCode);

        if (string.IsNullOrWhiteSpace(dto.Symptoms))
            return ServiceResult<VisitDto>.Fail("Symptoms are required.", ErrorType.Validation, "SymptomsRequired");

        if (string.IsNullOrWhiteSpace(dto.Diagnosis))
            return ServiceResult<VisitDto>.Fail("Diagnosis is required.", ErrorType.Validation, "DiagnosisRequired");

        visit.Symptoms = dto.Symptoms.Trim();
        visit.Diagnosis = dto.Diagnosis.Trim();
        visit.Notes = dto.Notes?.Trim() ?? string.Empty;

        await _context.SaveChangesAsync(cancellationToken);

        var updated = await GetByIdAsync(visit.Id, cancellationToken);
        return ServiceResult<VisitDto>.Ok(updated, "Visit updated successfully.");
    }

    public async Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var visit = await _context.Visits
            .Include(v => v.Appointment)
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

        if (visit is null)
            return ServiceResult.Fail("Visit not found.", ErrorType.NotFound, "VisitNotFound");

        var accessResult = await EnsureDoctorCanAccessAppointmentAsync(
            visit.Appointment,
            "You are not allowed to delete this visit.",
            cancellationToken);
        if (!accessResult.Success)
            return accessResult;

        if (visit.Appointment?.Status == AppointmentStatus.Completed)
            return ServiceResult.Fail("Cannot delete visit because the linked appointment is completed.", ErrorType.BusinessRule, "CompletedAppointmentVisit");

        var hasPrescriptions = await _context.Prescriptions.AnyAsync(p => p.VisitId == id, cancellationToken);
        if (hasPrescriptions)
            return ServiceResult.Fail("Cannot delete visit because there are prescriptions linked to this visit.", ErrorType.BusinessRule, "VisitHasPrescriptions");

        _context.Visits.Remove(visit);
        await _context.SaveChangesAsync(cancellationToken);

        return ServiceResult.Ok("Visit deleted successfully.");
    }

    private IQueryable<VisitDto> ProjectVisitQuery(IQueryable<Visit> query)
    {
        var doctorsQuery = _context.Doctors.IgnoreQueryFilters();
        var patientsQuery = _context.Patients.IgnoreQueryFilters();
        var includeClinical = CanReadClinicalData();

        return query.Select(v => new VisitDto
        {
            Id = v.Id,
            AppointmentId = v.AppointmentId,
            AppointmentDate = v.Appointment != null ? v.Appointment.AppointmentDate : DateTime.MinValue,
            PatientName = v.Appointment != null
                ? patientsQuery.Where(p => p.Id == v.Appointment.PatientId).Select(p => p.FullName).FirstOrDefault() ?? string.Empty
                : string.Empty,
            DoctorName = v.Appointment != null
                ? doctorsQuery.Where(d => d.Id == v.Appointment.DoctorId).Select(d => d.FullName).FirstOrDefault() ?? string.Empty
                : string.Empty,
            Symptoms = includeClinical ? v.Symptoms : string.Empty,
            Diagnosis = includeClinical ? v.Diagnosis : string.Empty,
            Notes = includeClinical ? v.Notes : string.Empty
        });
    }

    private async Task<ServiceResult> EnsureDoctorCanAccessAppointmentAsync(
        Appointment? appointment,
        string forbiddenMessage,
        CancellationToken cancellationToken)
    {
        if (!IsDoctor())
            return ServiceResult.Ok();

        var currentDoctorId = await GetCurrentDoctorIdAsync(cancellationToken);
        if (!currentDoctorId.HasValue || appointment?.DoctorId != currentDoctorId.Value)
            return ServiceResult.Fail(forbiddenMessage, ErrorType.Forbidden, "DoctorVisitMismatch");

        return ServiceResult.Ok();
    }

    private bool IsDoctor()
    {
        return _httpContextAccessor.HttpContext?.User?.IsInRole(AppRoles.Doctor) == true;
    }

    private bool CanReadClinicalData()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        return user?.IsInRole(AppRoles.Admin) == true || user?.IsInRole(AppRoles.Doctor) == true;
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
