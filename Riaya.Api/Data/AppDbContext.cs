using Riaya.Api.Entities;
using Riaya.Api.Interfaces;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Riaya.Api.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    private readonly ICurrentUserService? _currentUserService;

    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        ICurrentUserService? currentUserService = null) : base(options)
    {
        _currentUserService = currentUserService;
    }

    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Specialization> Specializations => Set<Specialization>();
    public DbSet<DoctorSchedule> DoctorSchedules => Set<DoctorSchedule>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<Visit> Visits => Set<Visit>();
    public DbSet<Prescription> Prescriptions => Set<Prescription>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<ClinicRoom> ClinicRooms => Set<ClinicRoom>();
    public DbSet<DoctorClinicAssignment> DoctorClinicAssignments => Set<DoctorClinicAssignment>();
    public DbSet<MedicalService> MedicalServices => Set<MedicalService>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Doctor>()
            .HasOne(d => d.ApplicationUser)
            .WithOne(u => u.DoctorProfile)
            .HasForeignKey<Doctor>(d => d.ApplicationUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Appointment>()
            .HasOne(a => a.Doctor)
            .WithMany()
            .HasForeignKey(a => a.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Appointment>()
            .HasOne(a => a.Patient)
            .WithMany()
            .HasForeignKey(a => a.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Appointment>()
            .HasOne(a => a.ClinicRoom)
            .WithMany()
            .HasForeignKey(a => a.ClinicRoomId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<DoctorSchedule>()
            .HasOne(s => s.Doctor)
            .WithMany()
            .HasForeignKey(s => s.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Doctor>()
            .HasOne(d => d.Specialization)
            .WithMany()
            .HasForeignKey(d => d.SpecializationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Visit>()
            .HasOne(v => v.Appointment)
            .WithMany()
            .HasForeignKey(v => v.AppointmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Prescription>()
            .HasOne(p => p.Visit)
            .WithMany(v => v.Prescriptions)
            .HasForeignKey(p => p.VisitId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ClinicRoom>()
            .HasOne(c => c.Department)
            .WithMany(d => d.ClinicRooms)
            .HasForeignKey(c => c.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<DoctorClinicAssignment>()
            .HasOne(a => a.Doctor)
            .WithMany()
            .HasForeignKey(a => a.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<DoctorClinicAssignment>()
            .HasOne(a => a.ClinicRoom)
            .WithMany(c => c.DoctorAssignments)
            .HasForeignKey(a => a.ClinicRoomId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Invoice>()
            .HasOne(i => i.Patient)
            .WithMany()
            .HasForeignKey(i => i.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Invoice>()
            .HasOne(i => i.Appointment)
            .WithMany()
            .HasForeignKey(i => i.AppointmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Invoice>()
            .HasOne(i => i.Visit)
            .WithMany()
            .HasForeignKey(i => i.VisitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<InvoiceItem>()
            .HasOne(i => i.Invoice)
            .WithMany(i => i.Items)
            .HasForeignKey(i => i.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<InvoiceItem>()
            .HasOne(i => i.MedicalService)
            .WithMany()
            .HasForeignKey(i => i.MedicalServiceId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Payment>()
            .HasOne(p => p.Invoice)
            .WithMany(i => i.Payments)
            .HasForeignKey(p => p.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Payment>()
            .HasOne(p => p.ReceivedByUser)
            .WithMany()
            .HasForeignKey(p => p.ReceivedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Specialization>()
            .HasIndex(s => s.Name)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.Entity<Doctor>()
            .HasIndex(d => d.PhoneNumber)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.Entity<Patient>()
            .HasIndex(p => p.PhoneNumber)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.Entity<Doctor>()
            .HasIndex(d => d.ApplicationUserId)
            .IsUnique()
            .HasFilter("\"ApplicationUserId\" IS NOT NULL AND \"IsDeleted\" = false");

        builder.Entity<Department>()
            .HasIndex(d => d.Name)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false AND \"IsActive\" = true");

        builder.Entity<ClinicRoom>()
            .HasIndex(c => c.RoomNumber)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false AND \"IsActive\" = true");

        builder.Entity<DoctorClinicAssignment>()
            .HasIndex(a => new { a.DoctorId, a.ClinicRoomId, a.ActiveFrom })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.Entity<MedicalService>()
            .HasIndex(s => s.Name)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false AND \"IsActive\" = true");

        builder.Entity<Appointment>()
            .HasIndex(a => new { a.DoctorId, a.AppointmentDate, a.Status });

        builder.Entity<Appointment>()
            .Property(a => a.DurationMinutes)
            .HasDefaultValue(30);

        builder.Entity<Appointment>()
            .HasIndex(a => new { a.PatientId, a.AppointmentDate, a.Status });

        builder.Entity<Appointment>()
            .HasIndex(a => new { a.DoctorId, a.AppointmentDate })
            .IsUnique()
            .HasFilter("\"Status\" <> 2");

        builder.Entity<Appointment>()
            .HasIndex(a => new { a.PatientId, a.AppointmentDate })
            .IsUnique()
            .HasFilter("\"Status\" <> 2");

        builder.Entity<Visit>()
            .HasIndex(v => v.AppointmentId)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.Entity<Prescription>()
            .HasIndex(p => p.VisitId);

        builder.Entity<DoctorSchedule>()
            .HasIndex(s => new { s.DoctorId, s.DayOfWeek });

        builder.Entity<Specialization>()
            .Property(s => s.Name)
            .HasMaxLength(100);

        builder.Entity<Doctor>()
            .Property(d => d.FullName)
            .HasMaxLength(100);

        builder.Entity<Doctor>()
            .Property(d => d.PhoneNumber)
            .HasMaxLength(20);

        builder.Entity<Patient>()
            .Property(p => p.FullName)
            .HasMaxLength(100);

        builder.Entity<Patient>()
            .Property(p => p.PhoneNumber)
            .HasMaxLength(20);

        builder.Entity<Patient>()
            .Property(p => p.Gender)
            .HasMaxLength(20);

        builder.Entity<Prescription>()
            .Property(p => p.MedicationName)
            .HasMaxLength(200);

        builder.Entity<Prescription>()
            .Property(p => p.Dosage)
            .HasMaxLength(200);

        builder.Entity<Prescription>()
            .Property(p => p.Instructions)
            .HasMaxLength(1000);

        builder.Entity<Visit>()
            .Property(v => v.Symptoms)
            .HasMaxLength(1000);

        builder.Entity<Visit>()
            .Property(v => v.Diagnosis)
            .HasMaxLength(1000);

        builder.Entity<Visit>()
            .Property(v => v.Notes)
            .HasMaxLength(2000);

        builder.Entity<Department>()
            .Property(d => d.Name)
            .HasMaxLength(100);

        builder.Entity<Department>()
            .Property(d => d.Description)
            .HasMaxLength(500);

        builder.Entity<ClinicRoom>()
            .Property(c => c.Name)
            .HasMaxLength(100);

        builder.Entity<ClinicRoom>()
            .Property(c => c.RoomNumber)
            .HasMaxLength(50);

        builder.Entity<MedicalService>()
            .Property(s => s.Name)
            .HasMaxLength(150);

        builder.Entity<MedicalService>()
            .Property(s => s.Price)
            .HasPrecision(18, 2);

        builder.Entity<Invoice>()
            .Property(i => i.TotalAmount)
            .HasPrecision(18, 2);

        builder.Entity<Invoice>()
            .Property(i => i.PaidAmount)
            .HasPrecision(18, 2);

        builder.Entity<Invoice>()
            .Property(i => i.RemainingAmount)
            .HasPrecision(18, 2);

        builder.Entity<InvoiceItem>()
            .Property(i => i.Description)
            .HasMaxLength(250);

        builder.Entity<InvoiceItem>()
            .Property(i => i.UnitPrice)
            .HasPrecision(18, 2);

        builder.Entity<InvoiceItem>()
            .Property(i => i.TotalPrice)
            .HasPrecision(18, 2);

        builder.Entity<Payment>()
            .Property(p => p.Amount)
            .HasPrecision(18, 2);

        builder.Entity<Payment>()
            .Property(p => p.Notes)
            .HasMaxLength(500);

        builder.Entity<Specialization>()
            .HasQueryFilter(s => !s.IsDeleted);

        builder.Entity<Doctor>()
            .HasQueryFilter(d => !d.IsDeleted && !d.Specialization!.IsDeleted);

        builder.Entity<Patient>()
            .HasQueryFilter(p => !p.IsDeleted);

        builder.Entity<Appointment>()
            .HasQueryFilter(a => !a.IsDeleted && !a.Doctor!.IsDeleted && !a.Patient!.IsDeleted);

        builder.Entity<DoctorSchedule>()
            .HasQueryFilter(s => !s.IsDeleted && !s.Doctor!.IsDeleted);

        builder.Entity<Visit>()
            .HasQueryFilter(v => !v.IsDeleted && !v.Appointment!.IsDeleted && !v.Appointment.Doctor!.IsDeleted && !v.Appointment.Patient!.IsDeleted);

        builder.Entity<Prescription>()
            .HasQueryFilter(p => !p.IsDeleted && !p.Visit!.IsDeleted && !p.Visit.Appointment!.IsDeleted && !p.Visit.Appointment.Doctor!.IsDeleted && !p.Visit.Appointment.Patient!.IsDeleted);

        builder.Entity<Department>()
            .HasQueryFilter(d => !d.IsDeleted);

        builder.Entity<ClinicRoom>()
            .HasQueryFilter(c => !c.IsDeleted && !c.Department!.IsDeleted);

        builder.Entity<DoctorClinicAssignment>()
            .HasQueryFilter(a => !a.IsDeleted && !a.Doctor!.IsDeleted && !a.ClinicRoom!.IsDeleted);

        builder.Entity<MedicalService>()
            .HasQueryFilter(s => !s.IsDeleted);

        builder.Entity<Invoice>()
            .HasQueryFilter(i => !i.IsDeleted && !i.Patient!.IsDeleted);

        builder.Entity<InvoiceItem>()
            .HasQueryFilter(i => !i.IsDeleted && !i.Invoice!.IsDeleted);

        builder.Entity<Payment>()
            .HasQueryFilter(p => !p.IsDeleted && !p.Invoice!.IsDeleted);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;
        var currentUserId = _currentUserService?.UserId;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAtUtc = utcNow;
                entry.Entity.CreatedByUserId = currentUserId;
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAtUtc = utcNow;
                entry.Entity.UpdatedByUserId = currentUserId;

                entry.Property(e => e.CreatedAtUtc).IsModified = false;
                entry.Property(e => e.CreatedByUserId).IsModified = false;
            }
        }

        ApplySoftDelete(utcNow, currentUserId);

        return await base.SaveChangesAsync(cancellationToken);
    }

    private void ApplySoftDelete(DateTime utcNow, string? currentUserId)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>().Where(e => e.State == EntityState.Deleted))
        {
            entry.State = EntityState.Modified;
            entry.Entity.IsDeleted = true;
            entry.Entity.DeletedAtUtc = utcNow;
            entry.Entity.DeletedByUserId = currentUserId;
            entry.Entity.UpdatedAtUtc = utcNow;
            entry.Entity.UpdatedByUserId = currentUserId;

            entry.Property(e => e.CreatedAtUtc).IsModified = false;
            entry.Property(e => e.CreatedByUserId).IsModified = false;
        }
    }
}

