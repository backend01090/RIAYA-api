using Riaya.Api.Constants;
using Riaya.Api.Entities;
using Riaya.Api.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Riaya.Api.Data.Seed;

public static class DemoDataSeeder
{
    private const string DemoPassword = "Admin@12345";
    private const int DoctorCount = 8;
    private const int PatientCount = 35;
    private const int AppointmentCount = 70;
    private const int VisitCount = 24;
    private const int PrescriptionCount = 20;
    private const int InvoiceCount = 36;

    private static readonly (string Email, string FullName, string Role)[] DemoUsers =
    {
        ("admin@riaya.local", "RIAYA Demo Admin", AppRoles.Admin),
        ("reception1@riaya.local", "Nadia Reception", AppRoles.Receptionist),
        ("reception2@riaya.local", "Omar Reception", AppRoles.Receptionist),
        ("doctor1@riaya.local", "Dr. Layla Hassan", AppRoles.Doctor),
        ("doctor2@riaya.local", "Dr. Sami Kareem", AppRoles.Doctor),
        ("doctor3@riaya.local", "Dr. Huda Ali", AppRoles.Doctor),
        ("doctor4@riaya.local", "Dr. Yousif Nader", AppRoles.Doctor),
        ("doctor5@riaya.local", "Dr. Rasha Omar", AppRoles.Doctor),
        ("doctor6@riaya.local", "Dr. Karim Salim", AppRoles.Doctor),
        ("doctor7@riaya.local", "Dr. Mina Saad", AppRoles.Doctor),
        ("doctor8@riaya.local", "Dr. Farah Adel", AppRoles.Doctor)
    };

    private static readonly string[] SpecializationNames =
    {
        "Internal Medicine",
        "Pediatrics",
        "Dermatology",
        "Orthopedics",
        "Cardiology",
        "ENT",
        "Dentistry",
        "Laboratory"
    };

    private static readonly (string Name, string Description)[] DepartmentSeed =
    {
        ("Internal Medicine", "General adult medicine and chronic care"),
        ("Pediatrics", "Child health and follow-up care"),
        ("Dermatology", "Skin care and minor dermatology visits"),
        ("Orthopedics", "Bone, joint, and mobility clinics"),
        ("Cardiology", "Heart care and ECG services"),
        ("ENT", "Ear, nose, and throat clinics"),
        ("Dentistry", "Dental checkups and procedures"),
        ("Laboratory", "Basic laboratory testing services")
    };

    private static readonly (string Name, string Number, string Department)[] RoomSeed =
    {
        ("Room 101", "101", "Internal Medicine"),
        ("Room 102", "102", "Pediatrics"),
        ("Room 201", "201", "Dermatology"),
        ("Room 202", "202", "Orthopedics"),
        ("Cardiology Room", "301", "Cardiology"),
        ("ENT Room", "302", "ENT"),
        ("Dental Room", "401", "Dentistry"),
        ("Lab Room", "501", "Laboratory")
    };

    private static readonly (string Name, string Phone, DateTime DateOfBirth, string Gender)[] PatientSeed =
    {
        ("Mariam Saleh", "07530000001", new DateTime(1991, 2, 14), "Female"),
        ("Ahmed Kareem", "07530000002", new DateTime(1987, 8, 9), "Male"),
        ("Noor Abbas", "07530000003", new DateTime(1996, 5, 21), "Female"),
        ("Omar Naji", "07530000004", new DateTime(1979, 11, 3), "Male"),
        ("Sara Mahdi", "07530000005", new DateTime(2001, 1, 18), "Female"),
        ("Ali Salman", "07530000006", new DateTime(1984, 4, 7), "Male"),
        ("Rana Hameed", "07530000007", new DateTime(1993, 9, 30), "Female"),
        ("Yousif Adel", "07530000008", new DateTime(1975, 12, 12), "Male"),
        ("Hiba Falah", "07530000009", new DateTime(1999, 6, 6), "Female"),
        ("Zaid Hussein", "07530000010", new DateTime(1989, 3, 25), "Male"),
        ("Lina Faris", "07530000011", new DateTime(1994, 10, 10), "Female"),
        ("Mustafa Raad", "07530000012", new DateTime(1982, 7, 17), "Male"),
        ("Dalia Sami", "07530000013", new DateTime(2004, 2, 4), "Female"),
        ("Khalid Jawad", "07530000014", new DateTime(1971, 5, 27), "Male"),
        ("Aya Nabil", "07530000015", new DateTime(1998, 8, 2), "Female"),
        ("Hassan Basil", "07530000016", new DateTime(1986, 1, 29), "Male"),
        ("Fatima Riyad", "07530000017", new DateTime(1990, 11, 16), "Female"),
        ("Mahmoud Fadi", "07530000018", new DateTime(1978, 6, 11), "Male"),
        ("Mina Tarek", "07530000019", new DateTime(1997, 3, 19), "Female"),
        ("Bilal Amer", "07530000020", new DateTime(1981, 9, 5), "Male"),
        ("Reem Jasim", "07530000021", new DateTime(1992, 12, 8), "Female"),
        ("Laith Saad", "07530000022", new DateTime(1976, 4, 23), "Male"),
        ("Nourhan Nizar", "07530000023", new DateTime(2000, 7, 1), "Female"),
        ("Anas Kareem", "07530000024", new DateTime(1988, 10, 14), "Male"),
        ("Saja Imad", "07530000025", new DateTime(1995, 1, 6), "Female"),
        ("Firas Latif", "07530000026", new DateTime(1973, 8, 26), "Male"),
        ("Ruba Hazim", "07530000027", new DateTime(1991, 5, 13), "Female"),
        ("Bashar Wael", "07530000028", new DateTime(1985, 2, 22), "Male"),
        ("Hanin Akram", "07530000029", new DateTime(2002, 9, 9), "Female"),
        ("Ameer Zaki", "07530000030", new DateTime(1977, 6, 28), "Male"),
        ("Shatha Farhan", "07530000031", new DateTime(1996, 12, 3), "Female"),
        ("Nabil Sami", "07530000032", new DateTime(1980, 4, 15), "Male"),
        ("Maya Jalal", "07530000033", new DateTime(1999, 11, 24), "Female"),
        ("Tariq Firas", "07530000034", new DateTime(1983, 7, 12), "Male"),
        ("Dina Kamil", "07530000035", new DateTime(1994, 3, 31), "Female")
    };

    private static readonly (string Name, decimal Price)[] MedicalServiceSeed =
    {
        ("General Consultation", 25m),
        ("Specialist Consultation", 45m),
        ("Follow-up Visit", 15m),
        ("ECG", 20m),
        ("X-Ray", 30m),
        ("CBC Blood Test", 18m),
        ("Blood Sugar Test", 10m),
        ("Dental Checkup", 35m),
        ("Eye Examination", 28m),
        ("Prescription Review", 12m),
        ("Skin Consultation", 32m),
        ("Lab Sample Collection", 8m)
    };

    private static readonly string[] Symptoms =
    {
        "Seasonal flu symptoms",
        "Mild abdominal pain",
        "Skin irritation",
        "Follow-up consultation",
        "Routine checkup",
        "Mild joint pain"
    };

    private static readonly string[] Diagnoses =
    {
        "Seasonal flu",
        "Mild gastritis",
        "Skin irritation",
        "Routine follow-up",
        "General checkup",
        "Minor sprain"
    };

    private static readonly (string MedicationName, string Dosage, string Instructions, int DurationInDays)[] MedicationSeed =
    {
        ("Paracetamol", "500 mg", "Take after meals when needed.", 3),
        ("Ibuprofen", "400 mg", "Take after food.", 5),
        ("Amoxicillin", "500 mg", "Take every 8 hours.", 7),
        ("Cetirizine", "10 mg", "Take once daily in the evening.", 5),
        ("Omeprazole", "20 mg", "Take before breakfast.", 14),
        ("Vitamin D", "1000 IU", "Take once daily.", 30)
    };

    public static async Task SeedAsync(AppDbContext context, IOptions<DemoSeedOptions> demoSeedOptions)
    {
        if (!demoSeedOptions.Value.Enabled)
            return;

        await SeedRolesAsync(context);
        await SeedUsersAsync(context);
        await SeedSpecializationsAsync(context);
        await SeedDepartmentsAsync(context);
        await SeedClinicRoomsAsync(context);
        await SeedDoctorsAsync(context);
        await SeedPatientsAsync(context);
        await SeedDoctorSchedulesAsync(context);
        await SeedDoctorClinicAssignmentsAsync(context);
        await SeedAppointmentsAsync(context);
        await SeedVisitsAndPrescriptionsAsync(context);
        await SeedMedicalServicesAsync(context);
        await SeedInvoicesAndPaymentsAsync(context);
    }

    private static async Task SeedRolesAsync(AppDbContext context)
    {
        foreach (var roleName in AppRoles.All)
        {
            var normalizedName = roleName.ToUpperInvariant();
            var exists = await context.Roles.AnyAsync(r => r.NormalizedName == normalizedName);
            if (!exists)
            {
                context.Roles.Add(new IdentityRole(roleName)
                {
                    NormalizedName = normalizedName
                });
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedUsersAsync(AppDbContext context)
    {
        var passwordHasher = new PasswordHasher<ApplicationUser>();

        foreach (var userSeed in DemoUsers)
        {
            var normalizedEmail = userSeed.Email.ToUpperInvariant();
            var user = await context.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);

            if (user is null)
            {
                user = new ApplicationUser
                {
                    FullName = userSeed.FullName,
                    Email = userSeed.Email,
                    NormalizedEmail = normalizedEmail,
                    UserName = userSeed.Email,
                    NormalizedUserName = normalizedEmail,
                    EmailConfirmed = true,
                    SecurityStamp = Guid.NewGuid().ToString("N"),
                    ConcurrencyStamp = Guid.NewGuid().ToString("N")
                };
                user.PasswordHash = passwordHasher.HashPassword(user, DemoPassword);
                context.Users.Add(user);
                await context.SaveChangesAsync();
            }

            await EnsureUserRoleAsync(context, user, userSeed.Role);
        }
    }

    private static async Task EnsureUserRoleAsync(AppDbContext context, ApplicationUser user, string roleName)
    {
        var role = await context.Roles.FirstAsync(r => r.NormalizedName == roleName.ToUpperInvariant());
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

    private static async Task SeedSpecializationsAsync(AppDbContext context)
    {
        foreach (var name in SpecializationNames)
        {
            var exists = await context.Specializations.AnyAsync(s => s.Name == name);
            if (!exists)
                context.Specializations.Add(new Specialization { Name = name });
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedDepartmentsAsync(AppDbContext context)
    {
        foreach (var departmentSeed in DepartmentSeed)
        {
            var exists = await context.Departments.AnyAsync(d => d.Name == departmentSeed.Name);
            if (!exists)
            {
                context.Departments.Add(new Department
                {
                    Name = departmentSeed.Name,
                    Description = departmentSeed.Description,
                    IsActive = true
                });
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedClinicRoomsAsync(AppDbContext context)
    {
        var departmentsByName = await context.Departments.ToDictionaryAsync(d => d.Name);

        foreach (var roomSeed in RoomSeed)
        {
            var exists = await context.ClinicRooms.AnyAsync(r => r.RoomNumber == roomSeed.Number);
            if (!exists)
            {
                context.ClinicRooms.Add(new ClinicRoom
                {
                    Name = roomSeed.Name,
                    RoomNumber = roomSeed.Number,
                    DepartmentId = departmentsByName[roomSeed.Department].Id,
                    IsActive = true
                });
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedDoctorsAsync(AppDbContext context)
    {
        var specializations = await context.Specializations.OrderBy(s => s.Id).ToListAsync();
        var doctorUsers = await context.Users
            .Where(u => u.Email != null && u.Email.StartsWith("doctor") && u.Email.EndsWith("@riaya.local"))
            .OrderBy(u => u.Email)
            .ToListAsync();

        for (var index = 0; index < DoctorCount; index++)
        {
            var doctorUser = doctorUsers[index];
            var phone = $"0774000000{index + 1}";
            var doctor = await context.Doctors.FirstOrDefaultAsync(d => d.PhoneNumber == phone);

            if (doctor is null)
            {
                doctor = new Doctor
                {
                    FullName = doctorUser.FullName,
                    PhoneNumber = phone,
                    SpecializationId = specializations[index % specializations.Count].Id,
                    ApplicationUserId = doctorUser.Id
                };
                context.Doctors.Add(doctor);
            }
            else if (doctor.ApplicationUserId != doctorUser.Id)
            {
                doctor.ApplicationUserId = doctorUser.Id;
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedPatientsAsync(AppDbContext context)
    {
        foreach (var patientSeed in PatientSeed)
        {
            var exists = await context.Patients.AnyAsync(p => p.PhoneNumber == patientSeed.Phone);
            if (!exists)
            {
                context.Patients.Add(new Patient
                {
                    FullName = patientSeed.Name,
                    PhoneNumber = patientSeed.Phone,
                    DateOfBirth = patientSeed.DateOfBirth,
                    Gender = patientSeed.Gender
                });
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedDoctorSchedulesAsync(AppDbContext context)
    {
        var doctors = await GetDemoDoctorsAsync(context);
        var workingDays = new[] { DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday };

        foreach (var doctor in doctors)
        {
            foreach (var day in workingDays)
            {
                var exists = await context.DoctorSchedules.AnyAsync(s =>
                    s.DoctorId == doctor.Id &&
                    s.DayOfWeek == day &&
                    s.StartTime == new TimeSpan(9, 0, 0) &&
                    s.EndTime == new TimeSpan(17, 0, 0));

                if (!exists)
                {
                    context.DoctorSchedules.Add(new DoctorSchedule
                    {
                        DoctorId = doctor.Id,
                        DayOfWeek = day,
                        StartTime = new TimeSpan(9, 0, 0),
                        EndTime = new TimeSpan(17, 0, 0)
                    });
                }
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedDoctorClinicAssignmentsAsync(AppDbContext context)
    {
        var doctors = await GetDemoDoctorsAsync(context);
        var rooms = await context.ClinicRooms.OrderBy(r => r.Id).Take(DoctorCount).ToListAsync();
        var activeFrom = DateTime.Today.AddYears(-1);

        for (var index = 0; index < doctors.Count; index++)
        {
            var doctor = doctors[index];
            var room = rooms[index % rooms.Count];
            var exists = await context.DoctorClinicAssignments.AnyAsync(a =>
                a.DoctorId == doctor.Id &&
                a.ClinicRoomId == room.Id &&
                a.ActiveFrom == activeFrom);

            if (!exists)
            {
                context.DoctorClinicAssignments.Add(new DoctorClinicAssignment
                {
                    DoctorId = doctor.Id,
                    ClinicRoomId = room.Id,
                    IsPrimary = true,
                    ActiveFrom = activeFrom
                });
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedAppointmentsAsync(AppDbContext context)
    {
        var patients = await GetDemoPatientsAsync(context);
        var existingDemoAppointments = await context.Appointments.CountAsync(a => patients.Select(p => p.Id).Contains(a.PatientId));
        if (existingDemoAppointments >= AppointmentCount)
            return;

        var doctors = await GetDemoDoctorsAsync(context);
        var assignments = await context.DoctorClinicAssignments
            .Where(a => doctors.Select(d => d.Id).Contains(a.DoctorId))
            .ToDictionaryAsync(a => a.DoctorId);
        var appointmentDates = BuildAppointmentDates(AppointmentCount);
        var appointments = new List<Appointment>();

        for (var index = 0; index < AppointmentCount; index++)
        {
            var doctor = doctors[index % doctors.Count];
            var patient = patients[index % patients.Count];
            var appointmentDate = appointmentDates[index];
            var status = GetAppointmentStatus(index, appointmentDate);

            var exists = await context.Appointments.AnyAsync(a =>
                a.DoctorId == doctor.Id &&
                a.PatientId == patient.Id &&
                a.AppointmentDate == appointmentDate);

            if (exists)
                continue;

            appointments.Add(new Appointment
            {
                DoctorId = doctor.Id,
                PatientId = patient.Id,
                ClinicRoomId = assignments[doctor.Id].ClinicRoomId,
                AppointmentDate = appointmentDate,
                DurationMinutes = 30,
                Status = status
            });
        }

        context.Appointments.AddRange(appointments);
        await context.SaveChangesAsync();
    }

    private static async Task SeedVisitsAndPrescriptionsAsync(AppDbContext context)
    {
        var patients = await GetDemoPatientsAsync(context);
        var existingVisits = await context.Visits.CountAsync(v =>
            v.Appointment != null &&
            patients.Select(p => p.Id).Contains(v.Appointment.PatientId));

        if (existingVisits >= VisitCount)
            return;

        var completedAppointments = await context.Appointments
            .Where(a => patients.Select(p => p.Id).Contains(a.PatientId) && a.Status == AppointmentStatus.Completed)
            .OrderBy(a => a.AppointmentDate)
            .Take(VisitCount)
            .ToListAsync();

        var visits = new List<Visit>();

        for (var index = 0; index < completedAppointments.Count; index++)
        {
            var appointment = completedAppointments[index];
            var exists = await context.Visits.AnyAsync(v => v.AppointmentId == appointment.Id);
            if (exists)
                continue;

            visits.Add(new Visit
            {
                AppointmentId = appointment.Id,
                Symptoms = Symptoms[index % Symptoms.Length],
                Diagnosis = Diagnoses[index % Diagnoses.Length],
                Notes = "Demo local clinical note for workflow demonstration."
            });
        }

        context.Visits.AddRange(visits);
        await context.SaveChangesAsync();

        var demoVisits = await context.Visits
            .Where(v => v.Appointment != null && patients.Select(p => p.Id).Contains(v.Appointment.PatientId))
            .OrderBy(v => v.Id)
            .Take(PrescriptionCount)
            .ToListAsync();

        for (var index = 0; index < demoVisits.Count; index++)
        {
            var visit = demoVisits[index];
            var exists = await context.Prescriptions.AnyAsync(p => p.VisitId == visit.Id);
            if (exists)
                continue;

            var medication = MedicationSeed[index % MedicationSeed.Length];
            context.Prescriptions.Add(new Prescription
            {
                VisitId = visit.Id,
                MedicationName = medication.MedicationName,
                Dosage = medication.Dosage,
                Instructions = medication.Instructions,
                DurationInDays = medication.DurationInDays
            });
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedMedicalServicesAsync(AppDbContext context)
    {
        foreach (var serviceSeed in MedicalServiceSeed)
        {
            var exists = await context.MedicalServices.AnyAsync(s => s.Name == serviceSeed.Name);
            if (!exists)
            {
                context.MedicalServices.Add(new MedicalService
                {
                    Name = serviceSeed.Name,
                    Price = serviceSeed.Price,
                    IsActive = true
                });
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedInvoicesAndPaymentsAsync(AppDbContext context)
    {
        var patients = await GetDemoPatientsAsync(context);
        var existingInvoices = await context.Invoices.CountAsync(i => patients.Select(p => p.Id).Contains(i.PatientId));
        if (existingInvoices >= InvoiceCount)
            return;

        var services = await context.MedicalServices.OrderBy(s => s.Id).ToListAsync();
        var appointments = await context.Appointments
            .Where(a => patients.Select(p => p.Id).Contains(a.PatientId) && a.Status != AppointmentStatus.NoShow)
            .OrderBy(a => a.AppointmentDate)
            .Take(InvoiceCount)
            .ToListAsync();
        var visits = await context.Visits.OrderBy(v => v.Id).ToListAsync();
        var invoices = new List<Invoice>();

        for (var index = 0; index < appointments.Count; index++)
        {
            var appointment = appointments[index];
            var service = services[index % services.Count];
            var extraService = services[(index + 4) % services.Count];
            var visit = visits.FirstOrDefault(v => v.AppointmentId == appointment.Id);
            var invoice = new Invoice
            {
                PatientId = appointment.PatientId,
                AppointmentId = appointment.Id,
                VisitId = visit?.Id,
                IssuedAtUtc = appointment.AppointmentDate.ToUniversalTime().AddMinutes(45),
                Items =
                {
                    new InvoiceItem
                    {
                        MedicalServiceId = service.Id,
                        Description = service.Name,
                        Quantity = 1,
                        UnitPrice = service.Price,
                        TotalPrice = service.Price
                    }
                }
            };

            if (index % 5 == 0)
            {
                invoice.Items.Add(new InvoiceItem
                {
                    MedicalServiceId = extraService.Id,
                    Description = extraService.Name,
                    Quantity = 1,
                    UnitPrice = extraService.Price,
                    TotalPrice = extraService.Price
                });
            }

            RecalculateInvoice(invoice);

            switch (index % 9)
            {
                case 0:
                case 1:
                case 2:
                    invoice.Payments.Add(new Payment
                    {
                        Amount = invoice.TotalAmount,
                        Method = PaymentMethod.Cash,
                        PaidAtUtc = invoice.IssuedAtUtc.AddMinutes(5),
                        Notes = "Demo full payment"
                    });
                    break;
                case 3:
                case 4:
                case 5:
                    invoice.Payments.Add(new Payment
                    {
                        Amount = Math.Round(invoice.TotalAmount / 2, 2),
                        Method = PaymentMethod.Card,
                        PaidAtUtc = invoice.IssuedAtUtc.AddMinutes(10),
                        Notes = "Demo partial payment"
                    });
                    break;
                case 8:
                    invoice.Status = InvoiceStatus.Cancelled;
                    invoices.Add(invoice);
                    continue;
            }

            RecalculateInvoice(invoice);
            invoices.Add(invoice);
        }

        context.Invoices.AddRange(invoices);
        await context.SaveChangesAsync();
    }

    private static List<DateTime> BuildAppointmentDates(int count)
    {
        var dates = new List<DateTime>();
        var anchors = new[]
        {
            DateTime.Today.AddDays(-35),
            DateTime.Today.AddDays(-10),
            DateTime.Today,
            DateTime.Today.AddDays(7)
        };

        var slotsPerAnchor = (int)Math.Ceiling(count / (double)anchors.Length);

        foreach (var anchor in anchors)
        {
            var businessDay = anchor.Date == DateTime.Today
                ? DateTime.Today
                : MoveToBusinessDay(anchor);
            for (var slot = 0; slot < slotsPerAnchor && dates.Count < count; slot++)
            {
                var dayOffset = slot / DoctorCount;
                var hourOffset = (slot % 4) * 2;
                dates.Add(businessDay.AddDays(dayOffset).AddHours(9 + hourOffset));
            }
        }

        return dates;
    }

    private static DateTime MoveToBusinessDay(DateTime date)
    {
        while (date.DayOfWeek is DayOfWeek.Friday or DayOfWeek.Saturday)
            date = date.AddDays(1);

        return date.Date;
    }

    private static AppointmentStatus GetAppointmentStatus(int index, DateTime appointmentDate)
    {
        if (appointmentDate.Date > DateTime.Today)
            return index % 4 == 0 ? AppointmentStatus.Pending : AppointmentStatus.Confirmed;

        if (appointmentDate.Date == DateTime.Today)
            return index % 3 == 0 ? AppointmentStatus.CheckedIn : AppointmentStatus.Confirmed;

        return index switch
        {
            < VisitCount => AppointmentStatus.Completed,
            _ when index % 11 == 0 => AppointmentStatus.NoShow,
            _ when index % 9 == 0 => AppointmentStatus.Cancelled,
            _ => AppointmentStatus.Confirmed
        };
    }

    private static async Task<List<Doctor>> GetDemoDoctorsAsync(AppDbContext context)
    {
        var demoUserIds = await context.Users
            .Where(u => u.Email != null && u.Email.StartsWith("doctor") && u.Email.EndsWith("@riaya.local"))
            .Select(u => u.Id)
            .ToListAsync();

        return await context.Doctors
            .Where(d => d.ApplicationUserId != null && demoUserIds.Contains(d.ApplicationUserId))
            .OrderBy(d => d.PhoneNumber)
            .ToListAsync();
    }

    private static async Task<List<Patient>> GetDemoPatientsAsync(AppDbContext context)
    {
        return await context.Patients
            .Where(p => p.PhoneNumber.StartsWith("075300000"))
            .OrderBy(p => p.PhoneNumber)
            .ToListAsync();
    }

    private static void RecalculateInvoice(Invoice invoice)
    {
        invoice.TotalAmount = invoice.Items.Sum(i => i.TotalPrice);
        invoice.PaidAmount = invoice.Payments.Sum(p => p.Amount);
        invoice.RemainingAmount = invoice.TotalAmount - invoice.PaidAmount;

        if (invoice.Status == InvoiceStatus.Cancelled)
            return;

        invoice.Status = invoice.TotalAmount <= 0
            ? InvoiceStatus.Draft
            : invoice.PaidAmount <= 0
                ? InvoiceStatus.Issued
                : invoice.PaidAmount < invoice.TotalAmount
                    ? InvoiceStatus.PartiallyPaid
                    : InvoiceStatus.Paid;
    }
}
