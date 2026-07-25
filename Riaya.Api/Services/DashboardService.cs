using Riaya.Api.Data;
using Riaya.Api.DTOs.Dashboard;
using Riaya.Api.Enums;
using Riaya.Api.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Riaya.Api.Services;

public class DashboardService : IDashboardService
{
    private readonly AppDbContext _context;

    public DashboardService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        var now = DateTime.UtcNow;

        var doctorsQuery = _context.Doctors.IgnoreQueryFilters();
        var patientsQuery = _context.Patients.IgnoreQueryFilters();
        var specializationsQuery = _context.Specializations.IgnoreQueryFilters();

        var recentAppointments = await _context.Appointments
            .AsNoTracking()
            .OrderByDescending(a => a.AppointmentDate)
            .Take(5)
            .Select(a => new DashboardRecentAppointmentDto
            {
                AppointmentId = a.Id,
                AppointmentDate = a.AppointmentDate,
                Status = a.Status.ToString(),
                DoctorName = doctorsQuery
                    .Where(d => d.Id == a.DoctorId)
                    .Select(d => d.FullName)
                    .FirstOrDefault() ?? string.Empty,
                PatientName = patientsQuery
                    .Where(p => p.Id == a.PatientId)
                    .Select(p => p.FullName)
                    .FirstOrDefault() ?? string.Empty,
                SpecializationName = (
                    from d in doctorsQuery
                    join s in specializationsQuery on d.SpecializationId equals s.Id
                    where d.Id == a.DoctorId
                    select s.Name
                ).FirstOrDefault() ?? string.Empty
            })
            .ToListAsync();

        var recentVisits = await _context.Visits
            .AsNoTracking()
            .OrderByDescending(v => v.Appointment != null ? v.Appointment.AppointmentDate : DateTime.MinValue)
            .Take(5)
            .Select(v => new DashboardRecentVisitDto
            {
                VisitId = v.Id,
                AppointmentDate = v.Appointment != null ? v.Appointment.AppointmentDate : DateTime.MinValue,
                DoctorName = v.Appointment != null
                    ? doctorsQuery
                        .Where(d => d.Id == v.Appointment.DoctorId)
                        .Select(d => d.FullName)
                        .FirstOrDefault() ?? string.Empty
                    : string.Empty,
                PatientName = v.Appointment != null
                    ? patientsQuery
                        .Where(p => p.Id == v.Appointment.PatientId)
                        .Select(p => p.FullName)
                        .FirstOrDefault() ?? string.Empty
                    : string.Empty,
                Diagnosis = v.Diagnosis
            })
            .ToListAsync();

        var recentPrescriptions = await _context.Prescriptions
            .AsNoTracking()
            .OrderByDescending(p => p.Visit != null && p.Visit.Appointment != null
                ? p.Visit.Appointment.AppointmentDate
                : DateTime.MinValue)
            .Take(5)
            .Select(p => new DashboardRecentPrescriptionDto
            {
                PrescriptionId = p.Id,
                MedicationName = p.MedicationName,
                Dosage = p.Dosage,
                DoctorName = p.Visit != null && p.Visit.Appointment != null
                    ? doctorsQuery
                        .Where(d => d.Id == p.Visit.Appointment.DoctorId)
                        .Select(d => d.FullName)
                        .FirstOrDefault() ?? string.Empty
                    : string.Empty,
                PatientName = p.Visit != null && p.Visit.Appointment != null
                    ? patientsQuery
                        .Where(pt => pt.Id == p.Visit.Appointment.PatientId)
                        .Select(pt => pt.FullName)
                        .FirstOrDefault() ?? string.Empty
                    : string.Empty,
                AppointmentDate = p.Visit != null && p.Visit.Appointment != null
                    ? p.Visit.Appointment.AppointmentDate
                    : DateTime.MinValue
            })
            .ToListAsync();

        return new DashboardOverviewDto
        {
            TotalDoctors = await _context.Doctors.CountAsync(),
            TotalPatients = await _context.Patients.CountAsync(),
            TotalSpecializations = await _context.Specializations.CountAsync(),

            TotalAppointments = await _context.Appointments.CountAsync(),
            PendingAppointments = await _context.Appointments.CountAsync(a => a.Status == AppointmentStatus.Pending),
            ConfirmedAppointments = await _context.Appointments.CountAsync(a => a.Status == AppointmentStatus.Confirmed),
            CancelledAppointments = await _context.Appointments.CountAsync(a => a.Status == AppointmentStatus.Cancelled),
            CompletedAppointments = await _context.Appointments.CountAsync(a => a.Status == AppointmentStatus.Completed),

            TodayAppointments = await _context.Appointments.CountAsync(a =>
                a.AppointmentDate >= today && a.AppointmentDate < tomorrow),

            TodayConfirmedAppointments = await _context.Appointments.CountAsync(a =>
                a.AppointmentDate >= today &&
                a.AppointmentDate < tomorrow &&
                a.Status == AppointmentStatus.Confirmed),

            UpcomingAppointments = await _context.Appointments.CountAsync(a =>
                a.AppointmentDate > now &&
                a.Status != AppointmentStatus.Cancelled),

            TodayVisits = await _context.Visits.CountAsync(v =>
                v.Appointment != null &&
                v.Appointment.AppointmentDate >= today &&
                v.Appointment.AppointmentDate < tomorrow),

            TotalVisits = await _context.Visits.CountAsync(),

            TotalPrescriptions = await _context.Prescriptions.CountAsync(),

            TodayPrescriptions = await _context.Prescriptions.CountAsync(p =>
                p.Visit != null &&
                p.Visit.Appointment != null &&
                p.Visit.Appointment.AppointmentDate >= today &&
                p.Visit.Appointment.AppointmentDate < tomorrow),

            RecentAppointments = recentAppointments,
            RecentVisits = recentVisits,
            RecentPrescriptions = recentPrescriptions
        };
    }
}

