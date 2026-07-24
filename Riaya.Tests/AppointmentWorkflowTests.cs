using Riaya.Api.Constants;
using Riaya.Api.DTOs.Appointment;
using Riaya.Api.DTOs.Visit;
using Riaya.Api.Enums;
using Riaya.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace Riaya.Tests;

public class AppointmentWorkflowTests
{
    [Fact]
    public async Task CheckInAsync_ChecksInConfirmedAppointment()
    {
        await using var context = ClinicTestFactory.CreateContext();
        var seeded = await ClinicTestFactory.SeedClinicAsync(context);
        var appointment = ClinicTestFactory.CreateAppointment(
            seeded.Doctor,
            seeded.Patient,
            DateTime.Today.AddHours(10),
            AppointmentStatus.Confirmed);
        context.Appointments.Add(appointment);
        await context.SaveChangesAsync();
        var service = ClinicTestFactory.CreateAppointmentService(context);

        var result = await service.CheckInAsync(appointment.Id);

        Assert.True(result.Success);
        var updated = await context.Appointments.SingleAsync(a => a.Id == appointment.Id);
        Assert.Equal(AppointmentStatus.CheckedIn, updated.Status);
    }

    [Fact]
    public async Task CheckInAsync_ReturnsError_WhenAppointmentIsPending()
    {
        await using var context = ClinicTestFactory.CreateContext();
        var seeded = await ClinicTestFactory.SeedClinicAsync(context);
        var appointment = ClinicTestFactory.CreateAppointment(
            seeded.Doctor,
            seeded.Patient,
            DateTime.Today.AddHours(10),
            AppointmentStatus.Pending);
        context.Appointments.Add(appointment);
        await context.SaveChangesAsync();
        var service = ClinicTestFactory.CreateAppointmentService(context);

        var result = await service.CheckInAsync(appointment.Id);

        Assert.False(result.Success);
        Assert.Equal("Only confirmed appointments can be checked in.", result.Message);
    }

    [Theory]
    [InlineData(AppointmentStatus.Cancelled, "Cancelled appointment cannot be checked in.")]
    [InlineData(AppointmentStatus.Completed, "Completed appointment cannot be checked in.")]
    [InlineData(AppointmentStatus.NoShow, "No-show appointment cannot be checked in.")]
    public async Task CheckInAsync_ReturnsError_WhenAppointmentStatusCannotCheckIn(
        AppointmentStatus status,
        string expectedMessage)
    {
        await using var context = ClinicTestFactory.CreateContext();
        var seeded = await ClinicTestFactory.SeedClinicAsync(context);
        var appointment = ClinicTestFactory.CreateAppointment(
            seeded.Doctor,
            seeded.Patient,
            DateTime.Today.AddHours(10),
            status);
        context.Appointments.Add(appointment);
        await context.SaveChangesAsync();
        var service = ClinicTestFactory.CreateAppointmentService(context);

        var result = await service.CheckInAsync(appointment.Id);

        Assert.False(result.Success);
        Assert.Equal(expectedMessage, result.Message);
    }

    [Fact]
    public async Task CreateVisit_CompletesCheckedInAppointment()
    {
        await using var context = ClinicTestFactory.CreateContext();
        var seeded = await ClinicTestFactory.SeedClinicAsync(context);
        var appointment = ClinicTestFactory.CreateAppointment(
            seeded.Doctor,
            seeded.Patient,
            ClinicTestFactory.PreviousMondayAt(9, 0),
            AppointmentStatus.CheckedIn);
        context.Appointments.Add(appointment);
        await context.SaveChangesAsync();
        var service = ClinicTestFactory.CreateVisitService(context);

        var result = await service.CreateAsync(new CreateVisitDto
        {
            AppointmentId = appointment.Id,
            Symptoms = "Headache",
            Diagnosis = "Migraine"
        });

        Assert.True(result.Success);
        var updated = await context.Appointments.SingleAsync(a => a.Id == appointment.Id);
        Assert.Equal(AppointmentStatus.Completed, updated.Status);
    }

    [Fact]
    public async Task MarkNoShowAsync_ReturnsError_WhenAppointmentIsInFuture()
    {
        await using var context = ClinicTestFactory.CreateContext();
        var seeded = await ClinicTestFactory.SeedClinicAsync(context);
        var appointment = ClinicTestFactory.CreateAppointment(
            seeded.Doctor,
            seeded.Patient,
            ClinicTestFactory.NextMondayAt(9, 0),
            AppointmentStatus.Confirmed);
        context.Appointments.Add(appointment);
        await context.SaveChangesAsync();
        var service = ClinicTestFactory.CreateAppointmentService(context);

        var result = await service.MarkNoShowAsync(appointment.Id);

        Assert.False(result.Success);
        Assert.Equal("Future appointment cannot be marked as no-show.", result.Message);
    }

    [Fact]
    public async Task CompleteAsync_ReturnsError_WhenAppointmentIsInFuture()
    {
        await using var context = ClinicTestFactory.CreateContext();
        var seeded = await ClinicTestFactory.SeedClinicAsync(context);
        var appointment = ClinicTestFactory.CreateAppointment(
            seeded.Doctor,
            seeded.Patient,
            ClinicTestFactory.NextMondayAt(9, 0),
            AppointmentStatus.Confirmed);
        context.Appointments.Add(appointment);
        await context.SaveChangesAsync();
        var service = ClinicTestFactory.CreateAppointmentService(context);

        var result = await service.CompleteAsync(appointment.Id);

        Assert.False(result.Success);
        Assert.Equal("Future appointment cannot be completed.", result.Message);
    }

    [Fact]
    public async Task CompleteAsync_ReturnsError_WhenAppointmentHasNoVisit()
    {
        await using var context = ClinicTestFactory.CreateContext();
        var seeded = await ClinicTestFactory.SeedClinicAsync(context);
        var appointment = ClinicTestFactory.CreateAppointment(
            seeded.Doctor,
            seeded.Patient,
            ClinicTestFactory.PreviousMondayAt(9, 0),
            AppointmentStatus.Confirmed);
        context.Appointments.Add(appointment);
        await context.SaveChangesAsync();
        var service = ClinicTestFactory.CreateAppointmentService(context);

        var result = await service.CompleteAsync(appointment.Id);

        Assert.False(result.Success);
        Assert.Equal("Appointment cannot be completed without a visit.", result.Message);
    }

    [Fact]
    public async Task CreateVisit_CompletesAppointment()
    {
        await using var context = ClinicTestFactory.CreateContext();
        var seeded = await ClinicTestFactory.SeedClinicAsync(context);
        var appointment = ClinicTestFactory.CreateAppointment(
            seeded.Doctor,
            seeded.Patient,
            ClinicTestFactory.PreviousMondayAt(9, 0),
            AppointmentStatus.Confirmed);
        context.Appointments.Add(appointment);
        await context.SaveChangesAsync();
        var service = ClinicTestFactory.CreateVisitService(context);

        var result = await service.CreateAsync(new CreateVisitDto
        {
            AppointmentId = appointment.Id,
            Symptoms = "Headache",
            Diagnosis = "Migraine"
        });

        Assert.True(result.Success);
        var updatedAppointment = await context.Appointments.SingleAsync(a => a.Id == appointment.Id);
        Assert.Equal(AppointmentStatus.Completed, updatedAppointment.Status);
    }

    [Fact]
    public async Task DeleteVisit_ReturnsError_WhenAppointmentIsCompleted()
    {
        await using var context = ClinicTestFactory.CreateContext();
        var seeded = await ClinicTestFactory.SeedClinicAsync(context);
        var appointment = ClinicTestFactory.CreateAppointment(
            seeded.Doctor,
            seeded.Patient,
            ClinicTestFactory.PreviousMondayAt(9, 0),
            AppointmentStatus.Confirmed);
        context.Appointments.Add(appointment);
        await context.SaveChangesAsync();
        var service = ClinicTestFactory.CreateVisitService(context);

        var createResult = await service.CreateAsync(new CreateVisitDto
        {
            AppointmentId = appointment.Id,
            Symptoms = "Headache",
            Diagnosis = "Migraine"
        });

        var deleteResult = await service.DeleteAsync(createResult.Data!.Id);

        Assert.False(deleteResult.Success);
        Assert.Equal("Cannot delete visit because the linked appointment is completed.", deleteResult.Message);
        Assert.True(await context.Visits.AnyAsync(v => v.Id == createResult.Data.Id));
    }

    [Fact]
    public async Task CreateVisit_ReturnsError_ForPendingOrCancelledAppointment()
    {
        await using var context = ClinicTestFactory.CreateContext();
        var seeded = await ClinicTestFactory.SeedClinicAsync(context);
        var pendingAppointment = ClinicTestFactory.CreateAppointment(
            seeded.Doctor,
            seeded.Patient,
            ClinicTestFactory.PreviousMondayAt(9, 0),
            AppointmentStatus.Pending);
        var cancelledAppointment = ClinicTestFactory.CreateAppointment(
            seeded.Doctor,
            seeded.OtherPatient,
            ClinicTestFactory.PreviousMondayAt(10, 0),
            AppointmentStatus.Cancelled);
        context.Appointments.AddRange(pendingAppointment, cancelledAppointment);
        await context.SaveChangesAsync();
        var service = ClinicTestFactory.CreateVisitService(context);

        var pendingResult = await service.CreateAsync(new CreateVisitDto
        {
            AppointmentId = pendingAppointment.Id,
            Symptoms = "Headache",
            Diagnosis = "Migraine"
        });
        var cancelledResult = await service.CreateAsync(new CreateVisitDto
        {
            AppointmentId = cancelledAppointment.Id,
            Symptoms = "Cough",
            Diagnosis = "Cold"
        });

        Assert.False(pendingResult.Success);
        Assert.False(cancelledResult.Success);
    }

    [Fact]
    public async Task Doctor_CannotCreateVisitForAnotherDoctorAppointment()
    {
        await using var context = ClinicTestFactory.CreateContext(ClinicTestFactory.DoctorUserId);
        var seeded = await ClinicTestFactory.SeedClinicAsync(context);
        var appointment = ClinicTestFactory.CreateAppointment(
            seeded.OtherDoctor,
            seeded.Patient,
            ClinicTestFactory.PreviousMondayAt(9, 0),
            AppointmentStatus.Confirmed);
        context.Appointments.Add(appointment);
        await context.SaveChangesAsync();
        var service = ClinicTestFactory.CreateVisitService(
            context,
            ClinicTestFactory.DoctorUserId,
            AppRoles.Doctor);

        var result = await service.CreateAsync(new CreateVisitDto
        {
            AppointmentId = appointment.Id,
            Symptoms = "Headache",
            Diagnosis = "Migraine"
        });

        Assert.False(result.Success);
        Assert.Equal("You are not allowed to create a visit for this appointment.", result.Message);
    }

    [Fact]
    public async Task CreateAsync_ReturnsError_WhenAppointmentOverlapsExistingDoctorSlot()
    {
        await using var context = ClinicTestFactory.CreateContext();
        var seeded = await ClinicTestFactory.SeedClinicAsync(context);
        context.Appointments.Add(ClinicTestFactory.CreateAppointment(
            seeded.Doctor,
            seeded.OtherPatient,
            ClinicTestFactory.NextMondayAt(9, 0),
            AppointmentStatus.Confirmed,
            durationMinutes: 30));
        await context.SaveChangesAsync();
        var service = ClinicTestFactory.CreateAppointmentService(context);

        var result = await service.CreateAsync(new CreateAppointmentDto
        {
            DoctorId = seeded.Doctor.Id,
            PatientId = seeded.Patient.Id,
            AppointmentDate = ClinicTestFactory.NextMondayAt(9, 15),
            DurationMinutes = 30
        });

        Assert.False(result.Success);
        Assert.Equal("This doctor already has an appointment at this time.", result.Message);
    }

    [Fact]
    public async Task CreateAsync_AllowsBackToBackAppointments()
    {
        await using var context = ClinicTestFactory.CreateContext();
        var seeded = await ClinicTestFactory.SeedClinicAsync(context);
        context.Appointments.Add(ClinicTestFactory.CreateAppointment(
            seeded.Doctor,
            seeded.Patient,
            ClinicTestFactory.NextMondayAt(9, 0),
            AppointmentStatus.Confirmed,
            durationMinutes: 30));
        await context.SaveChangesAsync();
        var service = ClinicTestFactory.CreateAppointmentService(context);

        var result = await service.CreateAsync(new CreateAppointmentDto
        {
            DoctorId = seeded.Doctor.Id,
            PatientId = seeded.Patient.Id,
            AppointmentDate = ClinicTestFactory.NextMondayAt(9, 30),
            DurationMinutes = 30
        });

        Assert.True(result.Success);
    }

    [Fact]
    public async Task CreateAsync_ReturnsError_WhenPatientHasOverlappingAppointment()
    {
        await using var context = ClinicTestFactory.CreateContext();
        var seeded = await ClinicTestFactory.SeedClinicAsync(context);
        context.Appointments.Add(ClinicTestFactory.CreateAppointment(
            seeded.OtherDoctor,
            seeded.Patient,
            ClinicTestFactory.NextMondayAt(9, 0),
            AppointmentStatus.Confirmed,
            durationMinutes: 30));
        await context.SaveChangesAsync();
        var service = ClinicTestFactory.CreateAppointmentService(context);

        var result = await service.CreateAsync(new CreateAppointmentDto
        {
            DoctorId = seeded.Doctor.Id,
            PatientId = seeded.Patient.Id,
            AppointmentDate = ClinicTestFactory.NextMondayAt(9, 15),
            DurationMinutes = 30
        });

        Assert.False(result.Success);
        Assert.Equal("This patient already has an appointment at this time.", result.Message);
    }

    [Fact]
    public async Task CreateAsync_IgnoresCancelledAppointmentWhenCheckingOverlap()
    {
        await using var context = ClinicTestFactory.CreateContext();
        var seeded = await ClinicTestFactory.SeedClinicAsync(context);
        context.Appointments.Add(ClinicTestFactory.CreateAppointment(
            seeded.Doctor,
            seeded.Patient,
            ClinicTestFactory.NextMondayAt(9, 0),
            AppointmentStatus.Cancelled,
            durationMinutes: 30));
        await context.SaveChangesAsync();
        var service = ClinicTestFactory.CreateAppointmentService(context);

        var result = await service.CreateAsync(new CreateAppointmentDto
        {
            DoctorId = seeded.Doctor.Id,
            PatientId = seeded.Patient.Id,
            AppointmentDate = ClinicTestFactory.NextMondayAt(9, 15),
            DurationMinutes = 30
        });

        Assert.True(result.Success);
    }

    [Fact]
    public async Task CreateAsync_ReturnsError_WhenAppointmentEndsAfterDoctorSchedule()
    {
        await using var context = ClinicTestFactory.CreateContext();
        var seeded = await ClinicTestFactory.SeedClinicAsync(context);
        var service = ClinicTestFactory.CreateAppointmentService(context);

        var result = await service.CreateAsync(new CreateAppointmentDto
        {
            DoctorId = seeded.Doctor.Id,
            PatientId = seeded.Patient.Id,
            AppointmentDate = ClinicTestFactory.NextMondayAt(16, 45),
            DurationMinutes = 30
        });

        Assert.False(result.Success);
        Assert.Equal("Appointment time is outside the doctor's working schedule.", result.Message);
    }
}
