using Riaya.Api.Common;
using Riaya.Api.Constants;
using Riaya.Api.Data;
using Riaya.Api.DTOs.Patient;
using Riaya.Api.Entities;
using Riaya.Api.Enums;
using Riaya.Api.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Riaya.Api.Services;

public class PatientService : IPatientService
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PatientService(
        AppDbContext context,
        ICurrentUserService currentUserService,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _currentUserService = currentUserService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<PagedResponse<PatientDto>> GetAllAsync(PatientQueryParams queryParams, CancellationToken cancellationToken = default)
    {
        var query = _context.Patients.AsNoTracking().AsQueryable();
        query = await ApplyPatientAccessAsync(query, cancellationToken);

        if (!string.IsNullOrWhiteSpace(queryParams.Search))
        {
            var search = queryParams.Search.Trim().ToLower();
            query = query.Where(p =>
                p.FullName.ToLower().Contains(search) ||
                p.PhoneNumber.ToLower().Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(queryParams.Gender))
        {
            var gender = queryParams.Gender.Trim().ToLower();
            query = query.Where(p => p.Gender.ToLower() == gender);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(p => p.FullName)
            .Skip((queryParams.PageNumber - 1) * queryParams.PageSize)
            .Take(queryParams.PageSize)
            .Select(p => new PatientDto
            {
                Id = p.Id,
                FullName = p.FullName,
                PhoneNumber = p.PhoneNumber,
                DateOfBirth = p.DateOfBirth,
                Gender = p.Gender
            })
            .ToListAsync(cancellationToken);

        return new PagedResponse<PatientDto>
        {
            Items = items,
            PageNumber = queryParams.PageNumber,
            PageSize = queryParams.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<PatientDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var query = _context.Patients
            .AsNoTracking()
            .Where(p => p.Id == id);

        query = await ApplyPatientAccessAsync(query, cancellationToken);

        return await query
            .Select(p => new PatientDto
            {
                Id = p.Id,
                FullName = p.FullName,
                PhoneNumber = p.PhoneNumber,
                DateOfBirth = p.DateOfBirth,
                Gender = p.Gender
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<PatientDto>> SearchByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var search = name.Trim().ToLower();
        var query = _context.Patients.AsNoTracking().AsQueryable();
        query = await ApplyPatientAccessAsync(query, cancellationToken);

        return await query
            .Where(p => p.FullName.ToLower().Contains(search))
            .OrderBy(p => p.FullName)
            .Select(p => new PatientDto
            {
                Id = p.Id,
                FullName = p.FullName,
                PhoneNumber = p.PhoneNumber,
                DateOfBirth = p.DateOfBirth,
                Gender = p.Gender
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<PatientHistoryDto?> GetHistoryAsync(int id, CancellationToken cancellationToken = default)
    {
        var query = _context.Patients.AsNoTracking().Where(p => p.Id == id);
        query = await ApplyPatientAccessAsync(query, cancellationToken);

        var patient = await query.FirstOrDefaultAsync(cancellationToken);
        if (patient is null)
            return null;

        var currentDoctorId = IsDoctor()
            ? await GetCurrentDoctorIdAsync(cancellationToken)
            : null;
        var includeClinical = CanReadClinicalData();

        var appointmentsQuery = _context.Appointments
            .AsNoTracking()
            .Where(a => a.PatientId == id);

        var visitsQuery = _context.Visits
            .AsNoTracking()
            .Where(v => v.Appointment != null && v.Appointment.PatientId == id);

        if (currentDoctorId.HasValue)
        {
            appointmentsQuery = appointmentsQuery.Where(a => a.DoctorId == currentDoctorId.Value);
            visitsQuery = visitsQuery.Where(v => v.Appointment != null && v.Appointment.DoctorId == currentDoctorId.Value);
        }

        var totalAppointments = await appointmentsQuery.CountAsync(cancellationToken);

        var visitRecords = await visitsQuery
            .Include(v => v.Prescriptions)
            .Include(v => v.Appointment)
            .ThenInclude(a => a!.Doctor)
            .OrderByDescending(v => v.Appointment != null ? v.Appointment.AppointmentDate : DateTime.MinValue)
            .ToListAsync(cancellationToken);

        var visits = visitRecords
            .Select(v => new PatientHistoryVisitDto
            {
                VisitId = v.Id,
                AppointmentId = v.AppointmentId,
                AppointmentDate = v.Appointment != null ? v.Appointment.AppointmentDate : DateTime.MinValue,
                DoctorName = v.Appointment?.Doctor?.FullName ?? string.Empty,
                Symptoms = includeClinical ? v.Symptoms : string.Empty,
                Diagnosis = includeClinical ? v.Diagnosis : string.Empty,
                Notes = includeClinical ? v.Notes : string.Empty,
                Prescriptions = includeClinical
                    ? v.Prescriptions
                        .Select(p => new PatientHistoryPrescriptionDto
                        {
                            PrescriptionId = p.Id,
                            MedicationName = p.MedicationName,
                            Dosage = p.Dosage,
                            Instructions = p.Instructions,
                            DurationInDays = p.DurationInDays
                        })
                        .ToList()
                    : new List<PatientHistoryPrescriptionDto>()
            })
            .ToList();

        return new PatientHistoryDto
        {
            PatientId = patient.Id,
            FullName = patient.FullName,
            PhoneNumber = patient.PhoneNumber,
            DateOfBirth = patient.DateOfBirth,
            Gender = patient.Gender,
            TotalAppointments = totalAppointments,
            TotalVisits = visits.Count,
            LastVisitDate = visits.FirstOrDefault()?.AppointmentDate,
            Visits = visits
        };
    }

    public async Task<PatientSummaryDto?> GetSummaryAsync(int id, CancellationToken cancellationToken = default)
    {
        var query = _context.Patients.AsNoTracking().Where(p => p.Id == id);
        query = await ApplyPatientAccessAsync(query, cancellationToken);

        var patient = await query
            .Select(p => new PatientSummaryDto
            {
                PatientId = p.Id,
                FullName = p.FullName,
                PhoneNumber = p.PhoneNumber,
                DateOfBirth = p.DateOfBirth,
                Gender = p.Gender
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (patient is null)
            return null;

        var appointmentsQuery = _context.Appointments.AsNoTracking().Where(a => a.PatientId == id);
        var visitsQuery = _context.Visits.AsNoTracking().Where(v => v.Appointment != null && v.Appointment.PatientId == id);
        var prescriptionsQuery = _context.Prescriptions.AsNoTracking().Where(p =>
            p.Visit != null &&
            p.Visit.Appointment != null &&
            p.Visit.Appointment.PatientId == id);

        var currentDoctorId = IsDoctor()
            ? await GetCurrentDoctorIdAsync(cancellationToken)
            : null;

        if (currentDoctorId.HasValue)
        {
            appointmentsQuery = appointmentsQuery.Where(a => a.DoctorId == currentDoctorId.Value);
            visitsQuery = visitsQuery.Where(v => v.Appointment != null && v.Appointment.DoctorId == currentDoctorId.Value);
            prescriptionsQuery = prescriptionsQuery.Where(p =>
                p.Visit != null &&
                p.Visit.Appointment != null &&
                p.Visit.Appointment.DoctorId == currentDoctorId.Value);
        }

        patient.TotalAppointments = await appointmentsQuery.CountAsync(cancellationToken);
        patient.PendingAppointments = await appointmentsQuery.CountAsync(a => a.Status == AppointmentStatus.Pending, cancellationToken);
        patient.ConfirmedAppointments = await appointmentsQuery.CountAsync(a => a.Status == AppointmentStatus.Confirmed, cancellationToken);
        patient.CompletedAppointments = await appointmentsQuery.CountAsync(a => a.Status == AppointmentStatus.Completed, cancellationToken);
        patient.CancelledAppointments = await appointmentsQuery.CountAsync(a => a.Status == AppointmentStatus.Cancelled, cancellationToken);
        patient.TotalVisits = await visitsQuery.CountAsync(cancellationToken);
        patient.TotalPrescriptions = CanReadClinicalData()
            ? await prescriptionsQuery.CountAsync(cancellationToken)
            : 0;

        patient.LastAppointmentDate = await appointmentsQuery
            .OrderByDescending(a => a.AppointmentDate)
            .Select(a => (DateTime?)a.AppointmentDate)
            .FirstOrDefaultAsync(cancellationToken);

        patient.LastVisitDate = await visitsQuery
            .OrderByDescending(v => v.Appointment != null ? v.Appointment.AppointmentDate : DateTime.MinValue)
            .Select(v => (DateTime?)(v.Appointment != null ? v.Appointment.AppointmentDate : null))
            .FirstOrDefaultAsync(cancellationToken);

        return patient;
    }

    public async Task<ServiceResult<PatientDto>> CreateAsync(CreatePatientDto dto, CancellationToken cancellationToken = default)
    {
        var fullName = dto.FullName.Trim();
        var phoneNumber = dto.PhoneNumber.Trim();
        var gender = dto.Gender.Trim();
        var today = DateTime.Today;
        var minimumAllowedDate = today.AddYears(-130);

        if (dto.DateOfBirth.Date > today)
            return ServiceResult<PatientDto>.Fail("Date of birth cannot be in the future.", ErrorType.Validation, "DateOfBirthInFuture");

        if (dto.DateOfBirth.Date < minimumAllowedDate)
            return ServiceResult<PatientDto>.Fail("Date of birth is outside the allowed range.", ErrorType.Validation, "DateOfBirthOutOfRange");

        var phoneExists = await _context.Patients
            .AnyAsync(p => p.PhoneNumber.ToLower() == phoneNumber.ToLower(), cancellationToken);

        if (phoneExists)
            return ServiceResult<PatientDto>.Fail("Patient phone number already exists.", ErrorType.Conflict, "PatientPhoneExists");

        var patient = new Patient
        {
            FullName = fullName,
            PhoneNumber = phoneNumber,
            DateOfBirth = dto.DateOfBirth.Date,
            Gender = gender
        };

        _context.Patients.Add(patient);
        await _context.SaveChangesAsync(cancellationToken);

        return ServiceResult<PatientDto>.Ok(await GetByIdAsync(patient.Id, cancellationToken), "Patient created successfully.");
    }

    public async Task<ServiceResult<PatientDto>> UpdateAsync(int id, CreatePatientDto dto, CancellationToken cancellationToken = default)
    {
        var patient = await _context.Patients.FindAsync(new object[] { id }, cancellationToken);
        if (patient is null)
            return ServiceResult<PatientDto>.Fail("Patient not found.", ErrorType.NotFound, "PatientNotFound");

        var fullName = dto.FullName.Trim();
        var phoneNumber = dto.PhoneNumber.Trim();
        var gender = dto.Gender.Trim();
        var today = DateTime.Today;
        var minimumAllowedDate = today.AddYears(-130);

        if (dto.DateOfBirth.Date > today)
            return ServiceResult<PatientDto>.Fail("Date of birth cannot be in the future.", ErrorType.Validation, "DateOfBirthInFuture");

        if (dto.DateOfBirth.Date < minimumAllowedDate)
            return ServiceResult<PatientDto>.Fail("Date of birth is outside the allowed range.", ErrorType.Validation, "DateOfBirthOutOfRange");

        var phoneExists = await _context.Patients
            .AnyAsync(p => p.Id != id && p.PhoneNumber.ToLower() == phoneNumber.ToLower(), cancellationToken);

        if (phoneExists)
            return ServiceResult<PatientDto>.Fail("Patient phone number already exists.", ErrorType.Conflict, "PatientPhoneExists");

        patient.FullName = fullName;
        patient.PhoneNumber = phoneNumber;
        patient.DateOfBirth = dto.DateOfBirth.Date;
        patient.Gender = gender;

        await _context.SaveChangesAsync(cancellationToken);
        return ServiceResult<PatientDto>.Ok(await GetByIdAsync(patient.Id, cancellationToken), "Patient updated successfully.");
    }

    public async Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var patient = await _context.Patients.FindAsync(new object[] { id }, cancellationToken);
        if (patient is null)
            return ServiceResult.Fail("Patient not found.", ErrorType.NotFound, "PatientNotFound");

        var hasAppointments = await _context.Appointments.AnyAsync(a => a.PatientId == id, cancellationToken);
        if (hasAppointments)
            return ServiceResult.Fail("Cannot delete patient because there are appointments linked to this patient.", ErrorType.BusinessRule, "PatientHasAppointments");

        _context.Patients.Remove(patient);
        await _context.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok("Patient deleted successfully.");
    }

    private async Task<IQueryable<Patient>> ApplyPatientAccessAsync(IQueryable<Patient> query, CancellationToken cancellationToken)
    {
        if (!IsDoctor())
            return query;

        var currentDoctorId = await GetCurrentDoctorIdAsync(cancellationToken);
        if (!currentDoctorId.HasValue)
            return query.Where(_ => false);

        return query.Where(p => _context.Appointments.Any(a =>
            a.PatientId == p.Id &&
            a.DoctorId == currentDoctorId.Value));
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
