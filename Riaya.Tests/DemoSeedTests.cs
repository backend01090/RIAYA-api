using Riaya.Api.Constants;
using Riaya.Api.Data.Seed;
using Riaya.Api.Entities;
using Riaya.Api.Enums;
using Riaya.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Riaya.Tests;

public class DemoSeedTests
{
    [Fact]
    public async Task DemoSeed_CreatesModerateMedicalComplexAndBillingData()
    {
        await using var context = ClinicTestFactory.CreateContext();

        await RunDemoSeedAsync(context);

        Assert.True(await context.Roles.CountAsync() >= 3);
        Assert.True(await CountUsersInRoleAsync(context, AppRoles.Admin) >= 1);
        Assert.True(await CountUsersInRoleAsync(context, AppRoles.Receptionist) >= 2);
        Assert.True(await CountUsersInRoleAsync(context, AppRoles.Doctor) >= 8);
        Assert.True(await context.Departments.CountAsync() >= 6);
        Assert.True(await context.ClinicRooms.CountAsync() >= 8);
        Assert.True(await context.Specializations.CountAsync() >= 6);
        Assert.True(await context.Doctors.CountAsync() >= 8);
        Assert.True(await context.Patients.CountAsync() >= 30);
        Assert.True(await context.Appointments.CountAsync() >= 60);
        Assert.True(await context.Visits.CountAsync() >= 20);
        Assert.True(await context.Prescriptions.CountAsync() >= 15);
        Assert.True(await context.MedicalServices.CountAsync() >= 10);
        Assert.True(await context.Invoices.CountAsync() >= 30);
        Assert.True(await context.Payments.CountAsync() >= 20);
    }

    [Fact]
    public async Task DemoSeed_IsIdempotent()
    {
        await using var context = ClinicTestFactory.CreateContext();

        await RunDemoSeedAsync(context);
        var firstCounts = await GetEntityCountsAsync(context);

        await RunDemoSeedAsync(context);
        var secondCounts = await GetEntityCountsAsync(context);

        Assert.Equal(firstCounts, secondCounts);
    }

    [Fact]
    public async Task DemoSeed_RespectsAppointmentIntegrity()
    {
        await using var context = ClinicTestFactory.CreateContext();
        await RunDemoSeedAsync(context);

        var appointments = await context.Appointments
            .AsNoTracking()
            .OrderBy(a => a.AppointmentDate)
            .ToListAsync();

        Assert.All(appointments.Where(a => a.Status == AppointmentStatus.Completed), appointment =>
        {
            Assert.True(context.Visits.Any(v => v.AppointmentId == appointment.Id));
        });

        Assert.DoesNotContain(appointments, a =>
            a.AppointmentDate > DateTime.Now &&
            (a.Status is AppointmentStatus.Completed or AppointmentStatus.NoShow));

        AssertNoOverlap(appointments, a => a.DoctorId, "doctor");
        AssertNoOverlap(appointments, a => a.PatientId, "patient");
        AssertNoOverlap(
            appointments.Where(a => a.ClinicRoomId.HasValue).ToList(),
            a => a.ClinicRoomId!.Value,
            "clinic room");
    }

    [Fact]
    public async Task DemoSeed_RespectsBillingIntegrity()
    {
        await using var context = ClinicTestFactory.CreateContext();
        await RunDemoSeedAsync(context);

        var invoices = await context.Invoices
            .Include(i => i.Items)
            .Include(i => i.Payments)
            .ToListAsync();

        Assert.Contains(invoices, i => i.Status == InvoiceStatus.Paid);
        Assert.Contains(invoices, i => i.Status == InvoiceStatus.PartiallyPaid);
        Assert.Contains(invoices, i => i.Status == InvoiceStatus.Issued);
        Assert.Contains(invoices, i => i.Status == InvoiceStatus.Cancelled);

        foreach (var invoice in invoices)
        {
            var expectedTotal = invoice.Items.Sum(i => i.TotalPrice);
            var expectedPaid = invoice.Payments.Sum(p => p.Amount);

            Assert.Equal(expectedTotal, invoice.TotalAmount);
            Assert.Equal(expectedPaid, invoice.PaidAmount);
            Assert.Equal(invoice.TotalAmount - invoice.PaidAmount, invoice.RemainingAmount);
            Assert.True(invoice.PaidAmount <= invoice.TotalAmount);

            if (invoice.Status == InvoiceStatus.Cancelled)
                Assert.Empty(invoice.Payments);
        }
    }

    private static async Task RunDemoSeedAsync(Riaya.Api.Data.AppDbContext context)
    {
        await DemoDataSeeder.SeedAsync(
            context,
            Options.Create(new DemoSeedOptions { Enabled = true }));
    }

    private static async Task<int> CountUsersInRoleAsync(Riaya.Api.Data.AppDbContext context, string roleName)
    {
        var normalizedRole = roleName.ToUpperInvariant();

        return await (
            from userRole in context.UserRoles
            join role in context.Roles on userRole.RoleId equals role.Id
            where role.NormalizedName == normalizedRole
            select userRole.UserId
        ).Distinct().CountAsync();
    }

    private static async Task<DemoSeedCounts> GetEntityCountsAsync(Riaya.Api.Data.AppDbContext context)
    {
        return new DemoSeedCounts(
            await context.Roles.CountAsync(),
            await context.Users.CountAsync(),
            await context.UserRoles.CountAsync(),
            await context.Specializations.CountAsync(),
            await context.Departments.CountAsync(),
            await context.ClinicRooms.CountAsync(),
            await context.Doctors.CountAsync(),
            await context.Patients.CountAsync(),
            await context.DoctorSchedules.CountAsync(),
            await context.DoctorClinicAssignments.CountAsync(),
            await context.Appointments.CountAsync(),
            await context.Visits.CountAsync(),
            await context.Prescriptions.CountAsync(),
            await context.MedicalServices.CountAsync(),
            await context.Invoices.CountAsync(),
            await context.InvoiceItems.CountAsync(),
            await context.Payments.CountAsync());
    }

    private static void AssertNoOverlap<T>(
        IReadOnlyCollection<Appointment> appointments,
        Func<Appointment, T> keySelector,
        string ownerName)
        where T : notnull
    {
        foreach (var group in appointments
                     .Where(a => a.Status != AppointmentStatus.Cancelled)
                     .GroupBy(keySelector))
        {
            var ordered = group.OrderBy(a => a.AppointmentDate).ToList();
            for (var index = 1; index < ordered.Count; index++)
            {
                var previous = ordered[index - 1];
                var current = ordered[index];
                var previousEnd = previous.AppointmentDate.AddMinutes(previous.DurationMinutes);

                Assert.False(
                    current.AppointmentDate < previousEnd,
                    $"Demo seed created overlapping {ownerName} appointments for key {group.Key}.");
            }
        }
    }

    private sealed record DemoSeedCounts(
        int Roles,
        int Users,
        int UserRoles,
        int Specializations,
        int Departments,
        int ClinicRooms,
        int Doctors,
        int Patients,
        int DoctorSchedules,
        int DoctorClinicAssignments,
        int Appointments,
        int Visits,
        int Prescriptions,
        int MedicalServices,
        int Invoices,
        int InvoiceItems,
        int Payments);
}
