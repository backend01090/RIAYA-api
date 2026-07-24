using System.Security.Claims;
using Riaya.Api.Constants;
using Riaya.Api.Data;
using Riaya.Api.DTOs.Appointment;
using Riaya.Api.Entities;
using Riaya.Api.Enums;
using Riaya.Api.Interfaces;
using Riaya.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Riaya.Tests.TestSupport;

internal static class ClinicTestFactory
{
    public const string DoctorUserId = "doctor-user-1";
    public const string OtherDoctorUserId = "doctor-user-2";

    public static AppDbContext CreateContext(string? currentUserId = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options, new TestCurrentUserService { UserId = currentUserId });
    }

    public static AppointmentService CreateAppointmentService(
        AppDbContext context,
        string? currentUserId = null,
        params string[] roles)
    {
        return new AppointmentService(
            context,
            new TestCurrentUserService { UserId = currentUserId },
            CreateHttpContextAccessor(currentUserId, roles));
    }

    public static VisitService CreateVisitService(
        AppDbContext context,
        string? currentUserId = null,
        params string[] roles)
    {
        return new VisitService(
            context,
            new TestCurrentUserService { UserId = currentUserId },
            CreateHttpContextAccessor(currentUserId, roles));
    }

    public static PatientService CreatePatientService(
        AppDbContext context,
        string? currentUserId = null,
        params string[] roles)
    {
        return new PatientService(
            context,
            new TestCurrentUserService { UserId = currentUserId },
            CreateHttpContextAccessor(currentUserId, roles));
    }

    public static PrescriptionService CreatePrescriptionService(
        AppDbContext context,
        string? currentUserId = null,
        params string[] roles)
    {
        return new PrescriptionService(
            context,
            new TestCurrentUserService { UserId = currentUserId },
            CreateHttpContextAccessor(currentUserId, roles));
    }

    public static DoctorService CreateDoctorService(AppDbContext context)
    {
        return new DoctorService(context);
    }

    public static DepartmentService CreateDepartmentService(AppDbContext context)
    {
        return new DepartmentService(context);
    }

    public static ClinicRoomService CreateClinicRoomService(AppDbContext context)
    {
        return new ClinicRoomService(context);
    }

    public static DoctorClinicAssignmentService CreateDoctorClinicAssignmentService(AppDbContext context)
    {
        return new DoctorClinicAssignmentService(context);
    }

    public static MedicalServiceService CreateMedicalServiceService(AppDbContext context)
    {
        return new MedicalServiceService(context);
    }

    public static InvoiceService CreateInvoiceService(AppDbContext context)
    {
        return new InvoiceService(context);
    }

    public static PaymentService CreatePaymentService(AppDbContext context, string? currentUserId = null)
    {
        return new PaymentService(context, new TestCurrentUserService { UserId = currentUserId });
    }

    public static async Task<SeededClinic> SeedClinicAsync(AppDbContext context)
    {
        var specialization = new Specialization { Name = "Cardiology" };

        var doctorUser = new ApplicationUser
        {
            Id = DoctorUserId,
            FullName = "Dr. Ahmed Ali",
            Email = "doctor1@example.com",
            UserName = "doctor1@example.com",
            EmailConfirmed = true
        };

        var otherDoctorUser = new ApplicationUser
        {
            Id = OtherDoctorUserId,
            FullName = "Dr. Sara Hassan",
            Email = "doctor2@example.com",
            UserName = "doctor2@example.com",
            EmailConfirmed = true
        };

        var doctor = new Doctor
        {
            FullName = "Dr. Ahmed Ali",
            PhoneNumber = "07712345678",
            ApplicationUserId = DoctorUserId,
            Specialization = specialization
        };

        var otherDoctor = new Doctor
        {
            FullName = "Dr. Sara Hassan",
            PhoneNumber = "07812345678",
            ApplicationUserId = OtherDoctorUserId,
            Specialization = specialization
        };

        var patient = new Patient
        {
            FullName = "Mohammed Samer",
            PhoneNumber = "07512345678",
            DateOfBirth = new DateTime(2003, 5, 10),
            Gender = "Male"
        };

        var otherPatient = new Patient
        {
            FullName = "Zainab Ali",
            PhoneNumber = "07522345678",
            DateOfBirth = new DateTime(1999, 11, 20),
            Gender = "Female"
        };

        context.Users.AddRange(doctorUser, otherDoctorUser);
        context.AddRange(specialization, doctor, otherDoctor, patient, otherPatient);
        await context.SaveChangesAsync();

        context.DoctorSchedules.AddRange(
            new DoctorSchedule
            {
                DoctorId = doctor.Id,
                DayOfWeek = DayOfWeek.Monday,
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(17, 0, 0)
            },
            new DoctorSchedule
            {
                DoctorId = otherDoctor.Id,
                DayOfWeek = DayOfWeek.Monday,
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(17, 0, 0)
            });

        await context.SaveChangesAsync();

        return new SeededClinic(doctor, otherDoctor, patient, otherPatient);
    }

    public static DateTime NextMondayAt(int hour, int minute)
    {
        var today = DateTime.Today;
        var target = today.AddDays(((int)DayOfWeek.Monday - (int)today.DayOfWeek + 7) % 7)
            .AddHours(hour)
            .AddMinutes(minute);

        return target <= DateTime.Now ? target.AddDays(7) : target;
    }

    public static DateTime PreviousMondayAt(int hour, int minute)
    {
        var today = DateTime.Today;
        var daysBack = ((int)today.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        if (daysBack == 0)
            daysBack = 7;

        return today.AddDays(-daysBack).AddHours(hour).AddMinutes(minute);
    }

    public static CreateAppointmentDto CreateAppointmentDto(
        SeededClinic seeded,
        DateTime appointmentDate)
    {
        return new CreateAppointmentDto
        {
            DoctorId = seeded.Doctor.Id,
            PatientId = seeded.Patient.Id,
            AppointmentDate = appointmentDate
        };
    }

    public static Appointment CreateAppointment(
        Doctor doctor,
        Patient patient,
        DateTime appointmentDate,
        AppointmentStatus status = AppointmentStatus.Pending,
        int durationMinutes = 30)
    {
        return new Appointment
        {
            DoctorId = doctor.Id,
            PatientId = patient.Id,
            AppointmentDate = appointmentDate,
            DurationMinutes = durationMinutes,
            Status = status
        };
    }

    public static async Task AddUserRoleAsync(AppDbContext context, ApplicationUser user, string roleName)
    {
        var role = await context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
        if (role is null)
        {
            role = new IdentityRole(roleName)
            {
                NormalizedName = roleName.ToUpperInvariant()
            };
            context.Roles.Add(role);
            await context.SaveChangesAsync();
        }

        var hasRole = await context.UserRoles.AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == role.Id);
        if (!hasRole)
        {
            context.UserRoles.Add(new IdentityUserRole<string>
            {
                UserId = user.Id,
                RoleId = role.Id
            });
            await context.SaveChangesAsync();
        }
    }

    private static IHttpContextAccessor CreateHttpContextAccessor(string? userId, params string[] roles)
    {
        var claims = new List<Claim>();

        if (!string.IsNullOrWhiteSpace(userId))
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
        }

        foreach (var role in roles.DefaultIfEmpty(AppRoles.Admin))
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var identity = new ClaimsIdentity(claims, "Test");

        return new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        };
    }
}

internal sealed record SeededClinic(
    Doctor Doctor,
    Doctor OtherDoctor,
    Patient Patient,
    Patient OtherPatient);

