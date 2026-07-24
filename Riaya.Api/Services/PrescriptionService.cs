using Riaya.Api.Common;
using Riaya.Api.Constants;
using Riaya.Api.Data;
using Riaya.Api.DTOs.Prescription;
using Riaya.Api.Entities;
using Riaya.Api.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Riaya.Api.Services;

public class PrescriptionService : IPrescriptionService
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PrescriptionService(
        AppDbContext context,
        ICurrentUserService currentUserService,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _currentUserService = currentUserService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<PagedResponse<PrescriptionDto>> GetAllAsync(PrescriptionQueryParams queryParams, CancellationToken cancellationToken = default)
    {
        var query = _context.Prescriptions
            .AsNoTracking()
            .AsQueryable();

        if (IsDoctor())
        {
            var currentDoctorId = await GetCurrentDoctorIdAsync(cancellationToken);
            if (!currentDoctorId.HasValue)
            {
                return new PagedResponse<PrescriptionDto>
                {
                    Items = new List<PrescriptionDto>(),
                    PageNumber = queryParams.PageNumber,
                    PageSize = queryParams.PageSize,
                    TotalCount = 0
                };
            }

            query = query.Where(p =>
                p.Visit != null &&
                p.Visit.Appointment != null &&
                p.Visit.Appointment.DoctorId == currentDoctorId.Value);
        }
        else if (queryParams.DoctorId.HasValue)
        {
            query = query.Where(p =>
                p.Visit != null &&
                p.Visit.Appointment != null &&
                p.Visit.Appointment.DoctorId == queryParams.DoctorId.Value);
        }

        if (queryParams.VisitId.HasValue)
            query = query.Where(p => p.VisitId == queryParams.VisitId.Value);

        if (queryParams.PatientId.HasValue)
            query = query.Where(p =>
                p.Visit != null &&
                p.Visit.Appointment != null &&
                p.Visit.Appointment.PatientId == queryParams.PatientId.Value);

        if (!string.IsNullOrWhiteSpace(queryParams.Search))
        {
            var search = queryParams.Search.Trim();
            var canSearchClinicalData = CanReadClinicalData();
            var parsedId = int.TryParse(search, out var searchId) ? searchId : (int?)null;

            query = query.Where(p =>
                (canSearchClinicalData && (
                    p.MedicationName.Contains(search) ||
                    p.Dosage.Contains(search) ||
                    p.Instructions.Contains(search))) ||
                (parsedId.HasValue && (
                    p.Id == parsedId.Value ||
                    p.VisitId == parsedId.Value ||
                    (p.Visit != null && p.Visit.AppointmentId == parsedId.Value))) ||
                (p.Visit != null && p.Visit.Appointment != null && p.Visit.Appointment.Patient != null && (
                    p.Visit.Appointment.Patient.FullName.Contains(search) ||
                    p.Visit.Appointment.Patient.PhoneNumber.Contains(search))) ||
                (p.Visit != null && p.Visit.Appointment != null && p.Visit.Appointment.Doctor != null && p.Visit.Appointment.Doctor.FullName.Contains(search)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await ProjectPrescriptionQuery(query)
            .OrderByDescending(p => p.AppointmentDate)
            .Skip((queryParams.PageNumber - 1) * queryParams.PageSize)
            .Take(queryParams.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResponse<PrescriptionDto>
        {
            Items = items,
            PageNumber = queryParams.PageNumber,
            PageSize = queryParams.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<PrescriptionDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var query = _context.Prescriptions
            .AsNoTracking()
            .Where(p => p.Id == id);

        if (IsDoctor())
        {
            var currentDoctorId = await GetCurrentDoctorIdAsync(cancellationToken);
            if (!currentDoctorId.HasValue)
                return null;

            query = query.Where(p =>
                p.Visit != null &&
                p.Visit.Appointment != null &&
                p.Visit.Appointment.DoctorId == currentDoctorId.Value);
        }

        return await ProjectPrescriptionQuery(query).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ServiceResult<PrescriptionDto>> CreateAsync(CreatePrescriptionDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.MedicationName))
            return ServiceResult<PrescriptionDto>.Fail("Medication name is required.", ErrorType.Validation, "MedicationRequired");

        if (string.IsNullOrWhiteSpace(dto.Dosage))
            return ServiceResult<PrescriptionDto>.Fail("Dosage is required.", ErrorType.Validation, "DosageRequired");

        if (string.IsNullOrWhiteSpace(dto.Instructions))
            return ServiceResult<PrescriptionDto>.Fail("Instructions are required.", ErrorType.Validation, "InstructionsRequired");

        var visit = await _context.Visits
            .Include(v => v.Appointment)
            .FirstOrDefaultAsync(v => v.Id == dto.VisitId, cancellationToken);

        if (visit is null)
            return ServiceResult<PrescriptionDto>.Fail("Invalid visit id.", ErrorType.Validation, "InvalidVisit");

        var accessResult = await EnsureDoctorCanAccessVisitAsync(visit, "create a prescription for this visit.", cancellationToken);
        if (!accessResult.Success)
            return ServiceResult<PrescriptionDto>.Fail(accessResult.Message, accessResult.ErrorType, accessResult.ErrorCode);

        var prescription = new Prescription
        {
            VisitId = dto.VisitId,
            MedicationName = dto.MedicationName.Trim(),
            Dosage = dto.Dosage.Trim(),
            Instructions = dto.Instructions.Trim(),
            DurationInDays = dto.DurationInDays
        };

        _context.Prescriptions.Add(prescription);
        await _context.SaveChangesAsync(cancellationToken);

        var created = await GetByIdAsync(prescription.Id, cancellationToken);
        return ServiceResult<PrescriptionDto>.Ok(created, "Prescription created successfully.");
    }

    public async Task<ServiceResult<PrescriptionDto>> UpdateAsync(int id, UpdatePrescriptionDto dto, CancellationToken cancellationToken = default)
    {
        var prescription = await _context.Prescriptions
            .Include(p => p.Visit)
            .ThenInclude(v => v!.Appointment)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (prescription is null)
            return ServiceResult<PrescriptionDto>.Fail("Prescription not found.", ErrorType.NotFound, "PrescriptionNotFound");

        var accessResult = await EnsureDoctorCanAccessVisitAsync(prescription.Visit, "update this prescription.", cancellationToken);
        if (!accessResult.Success)
            return ServiceResult<PrescriptionDto>.Fail(accessResult.Message, accessResult.ErrorType, accessResult.ErrorCode);

        if (string.IsNullOrWhiteSpace(dto.MedicationName))
            return ServiceResult<PrescriptionDto>.Fail("Medication name is required.", ErrorType.Validation, "MedicationRequired");

        if (string.IsNullOrWhiteSpace(dto.Dosage))
            return ServiceResult<PrescriptionDto>.Fail("Dosage is required.", ErrorType.Validation, "DosageRequired");

        if (string.IsNullOrWhiteSpace(dto.Instructions))
            return ServiceResult<PrescriptionDto>.Fail("Instructions are required.", ErrorType.Validation, "InstructionsRequired");

        prescription.MedicationName = dto.MedicationName.Trim();
        prescription.Dosage = dto.Dosage.Trim();
        prescription.Instructions = dto.Instructions.Trim();
        prescription.DurationInDays = dto.DurationInDays;

        await _context.SaveChangesAsync(cancellationToken);

        var updated = await GetByIdAsync(prescription.Id, cancellationToken);
        return ServiceResult<PrescriptionDto>.Ok(updated, "Prescription updated successfully.");
    }

    public async Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var prescription = await _context.Prescriptions
            .Include(p => p.Visit)
            .ThenInclude(v => v!.Appointment)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (prescription is null)
            return ServiceResult.Fail("Prescription not found.", ErrorType.NotFound, "PrescriptionNotFound");

        var accessResult = await EnsureDoctorCanAccessVisitAsync(prescription.Visit, "delete this prescription.", cancellationToken);
        if (!accessResult.Success)
            return accessResult;

        _context.Prescriptions.Remove(prescription);
        await _context.SaveChangesAsync(cancellationToken);

        return ServiceResult.Ok("Prescription deleted successfully.");
    }

    private IQueryable<PrescriptionDto> ProjectPrescriptionQuery(IQueryable<Prescription> query)
    {
        var doctorsQuery = _context.Doctors.IgnoreQueryFilters();
        var patientsQuery = _context.Patients.IgnoreQueryFilters();
        var includeClinical = CanReadClinicalData();

        return query.Select(p => new PrescriptionDto
        {
            Id = p.Id,
            VisitId = p.VisitId,
            AppointmentId = p.Visit != null ? p.Visit.AppointmentId : 0,
            AppointmentDate = p.Visit != null && p.Visit.Appointment != null
                ? p.Visit.Appointment.AppointmentDate
                : DateTime.MinValue,
            PatientName = p.Visit != null && p.Visit.Appointment != null
                ? patientsQuery.Where(pt => pt.Id == p.Visit.Appointment.PatientId).Select(pt => pt.FullName).FirstOrDefault() ?? string.Empty
                : string.Empty,
            DoctorName = p.Visit != null && p.Visit.Appointment != null
                ? doctorsQuery.Where(d => d.Id == p.Visit.Appointment.DoctorId).Select(d => d.FullName).FirstOrDefault() ?? string.Empty
                : string.Empty,
            MedicationName = includeClinical ? p.MedicationName : string.Empty,
            Dosage = includeClinical ? p.Dosage : string.Empty,
            Instructions = includeClinical ? p.Instructions : string.Empty,
            DurationInDays = includeClinical ? p.DurationInDays : 0
        });
    }

    private async Task<ServiceResult> EnsureDoctorCanAccessVisitAsync(
        Visit? visit,
        string action,
        CancellationToken cancellationToken)
    {
        if (!IsDoctor())
            return ServiceResult.Ok();

        var currentDoctorId = await GetCurrentDoctorIdAsync(cancellationToken);
        if (!currentDoctorId.HasValue || visit?.Appointment?.DoctorId != currentDoctorId.Value)
            return ServiceResult.Fail($"You are not allowed to {action}", ErrorType.Forbidden, "DoctorPrescriptionMismatch");

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
