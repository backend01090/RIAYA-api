using Riaya.Api.Constants;
using Riaya.Api.Data;
using Riaya.Api.DTOs.Appointment;
using Riaya.Api.Enums;
using Riaya.Api.Services;
using Riaya.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace Riaya.Tests;

public class AppointmentServiceTests
{
    [Fact]
    public async Task CreateAsync_ReturnsError_WhenAppointmentDateIsInPast()
    {
        await using var context = ClinicTestFactory.CreateContext();
        var service = ClinicTestFactory.CreateAppointmentService(context);

        var result = await service.CreateAsync(new CreateAppointmentDto
        {
            DoctorId = 1,
            PatientId = 1,
            AppointmentDate = DateTime.Now.AddMinutes(-15)
        });

        Assert.False(result.Success);
        Assert.Equal("Appointment date must be in the future.", result.Message);
    }

    [Fact]
    public async Task CreateAsync_ReturnsError_WhenAppointmentIsOutsideDoctorSchedule()
    {
        await using var context = ClinicTestFactory.CreateContext();
        var seeded = await ClinicTestFactory.SeedClinicAsync(context);
        var service = ClinicTestFactory.CreateAppointmentService(context);

        var result = await service.CreateAsync(ClinicTestFactory.CreateAppointmentDto(
            seeded,
            ClinicTestFactory.NextMondayAt(8, 0)));

        Assert.False(result.Success);
        Assert.Equal("Appointment time is outside the doctor's working schedule.", result.Message);
    }

    [Fact]
    public async Task CreateAsync_ReturnsError_WhenAppointmentIsNotOnFifteenMinuteBoundary()
    {
        await using var context = ClinicTestFactory.CreateContext();
        var seeded = await ClinicTestFactory.SeedClinicAsync(context);
        var service = ClinicTestFactory.CreateAppointmentService(context);

        var result = await service.CreateAsync(ClinicTestFactory.CreateAppointmentDto(
            seeded,
            ClinicTestFactory.NextMondayAt(9, 10)));

        Assert.False(result.Success);
        Assert.Contains("15-minute interval", result.Message);
    }

    [Fact]
    public async Task CreateAsync_ReturnsError_WhenDoctorAlreadyHasActiveAppointmentAtTime()
    {
        await using var context = ClinicTestFactory.CreateContext();
        var seeded = await ClinicTestFactory.SeedClinicAsync(context);
        var appointmentDate = ClinicTestFactory.NextMondayAt(9, 0);
        context.Appointments.Add(ClinicTestFactory.CreateAppointment(
            seeded.Doctor,
            seeded.OtherPatient,
            appointmentDate,
            AppointmentStatus.Confirmed));
        await context.SaveChangesAsync();
        var service = ClinicTestFactory.CreateAppointmentService(context);

        var result = await service.CreateAsync(ClinicTestFactory.CreateAppointmentDto(seeded, appointmentDate));

        Assert.False(result.Success);
        Assert.Equal("This doctor already has an appointment at this time.", result.Message);
    }

    [Fact]
    public async Task CreateAsync_ReturnsError_WhenPatientAlreadyHasActiveAppointmentAtTime()
    {
        await using var context = ClinicTestFactory.CreateContext();
        var seeded = await ClinicTestFactory.SeedClinicAsync(context);
        var appointmentDate = ClinicTestFactory.NextMondayAt(9, 0);
        context.Appointments.Add(ClinicTestFactory.CreateAppointment(
            seeded.OtherDoctor,
            seeded.Patient,
            appointmentDate,
            AppointmentStatus.Confirmed));
        await context.SaveChangesAsync();
        var service = ClinicTestFactory.CreateAppointmentService(context);

        var result = await service.CreateAsync(ClinicTestFactory.CreateAppointmentDto(seeded, appointmentDate));

        Assert.False(result.Success);
        Assert.Equal("This patient already has an appointment at this time.", result.Message);
    }

    [Fact]
    public async Task CreateAsync_AllowsCancelledAppointmentSlotToBeReused()
    {
        await using var context = ClinicTestFactory.CreateContext();
        var seeded = await ClinicTestFactory.SeedClinicAsync(context);
        var appointmentDate = ClinicTestFactory.NextMondayAt(9, 0);
        context.Appointments.Add(ClinicTestFactory.CreateAppointment(
            seeded.Doctor,
            seeded.Patient,
            appointmentDate,
            AppointmentStatus.Cancelled));
        await context.SaveChangesAsync();
        var service = ClinicTestFactory.CreateAppointmentService(context);

        var result = await service.CreateAsync(ClinicTestFactory.CreateAppointmentDto(seeded, appointmentDate));

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
    }

    [Fact]
    public async Task GetAllAsync_WhenCurrentUserIsDoctor_ReturnsOnlyLinkedDoctorAppointments()
    {
        await using var context = ClinicTestFactory.CreateContext(ClinicTestFactory.DoctorUserId);
        var seeded = await ClinicTestFactory.SeedClinicAsync(context);
        context.Appointments.AddRange(
            ClinicTestFactory.CreateAppointment(seeded.Doctor, seeded.Patient, ClinicTestFactory.NextMondayAt(9, 0)),
            ClinicTestFactory.CreateAppointment(seeded.OtherDoctor, seeded.OtherPatient, ClinicTestFactory.NextMondayAt(9, 15)));
        await context.SaveChangesAsync();
        var service = ClinicTestFactory.CreateAppointmentService(
            context,
            ClinicTestFactory.DoctorUserId,
            AppRoles.Doctor);

        var result = await service.GetAllAsync(new AppointmentQueryParams());

        Assert.Single(result.Items);
        Assert.Equal(seeded.Doctor.Id, result.Items[0].DoctorId);
    }

    [Fact]
    public async Task SoftDelete_HidesDeletedAppointmentsFromNormalQueries()
    {
        await using var context = ClinicTestFactory.CreateContext("admin-user");
        var seeded = await ClinicTestFactory.SeedClinicAsync(context);
        var appointment = ClinicTestFactory.CreateAppointment(
            seeded.Doctor,
            seeded.Patient,
            ClinicTestFactory.NextMondayAt(9, 0));
        context.Appointments.Add(appointment);
        await context.SaveChangesAsync();

        context.Appointments.Remove(appointment);
        await context.SaveChangesAsync();

        Assert.False(await context.Appointments.AnyAsync(a => a.Id == appointment.Id));

        var deletedAppointment = await context.Appointments
            .IgnoreQueryFilters()
            .SingleAsync(a => a.Id == appointment.Id);
        Assert.True(deletedAppointment.IsDeleted);
        Assert.Equal("admin-user", deletedAppointment.DeletedByUserId);
    }
}

