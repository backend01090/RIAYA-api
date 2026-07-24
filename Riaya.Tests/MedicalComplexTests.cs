using Riaya.Api.DTOs.Appointment;
using Riaya.Api.DTOs.ClinicRoom;
using Riaya.Api.DTOs.Department;
using Riaya.Api.DTOs.DoctorClinicAssignment;
using Riaya.Api.Enums;
using Riaya.Api.Entities;
using Riaya.Tests.TestSupport;

namespace Riaya.Tests;

public class MedicalComplexTests
{
    [Fact]
    public async Task CreateDepartment_ReturnsCreatedDepartment()
    {
        await using var context = ClinicTestFactory.CreateContext();
        var service = ClinicTestFactory.CreateDepartmentService(context);

        var result = await service.CreateAsync(new UpsertDepartmentDto
        {
            Name = "Cardiology",
            Description = "Heart care"
        });

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("Cardiology", result.Data.Name);
    }

    [Fact]
    public async Task CreateDepartment_ReturnsConflict_WhenActiveNameExists()
    {
        await using var context = ClinicTestFactory.CreateContext();
        var service = ClinicTestFactory.CreateDepartmentService(context);
        await service.CreateAsync(new UpsertDepartmentDto { Name = "Cardiology" });

        var result = await service.CreateAsync(new UpsertDepartmentDto { Name = "cardiology" });

        Assert.False(result.Success);
        Assert.Equal("Active department name already exists.", result.Message);
    }

    [Fact]
    public async Task CreateClinicRoom_ReturnsCreatedRoom()
    {
        await using var context = ClinicTestFactory.CreateContext();
        var department = await CreateDepartmentAsync(context);
        var service = ClinicTestFactory.CreateClinicRoomService(context);

        var result = await service.CreateAsync(new UpsertClinicRoomDto
        {
            Name = "Cardiology Room 1",
            RoomNumber = "C-101",
            DepartmentId = department.Id
        });

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("C-101", result.Data.RoomNumber);
    }

    [Fact]
    public async Task AssignDoctorToClinicRoom_ReturnsCreatedAssignment()
    {
        await using var context = ClinicTestFactory.CreateContext();
        var seeded = await ClinicTestFactory.SeedClinicAsync(context);
        var room = await CreateClinicRoomAsync(context);
        var service = ClinicTestFactory.CreateDoctorClinicAssignmentService(context);

        var result = await service.CreateAsync(new UpsertDoctorClinicAssignmentDto
        {
            DoctorId = seeded.Doctor.Id,
            ClinicRoomId = room.Id,
            IsPrimary = true,
            ActiveFrom = DateTime.Today.AddDays(-1)
        });

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(seeded.Doctor.Id, result.Data.DoctorId);
        Assert.Equal(room.Id, result.Data.ClinicRoomId);
    }

    [Fact]
    public async Task CreateDoctorClinicAssignment_ReturnsError_WhenSameDoctorRoomPeriodOverlaps()
    {
        await using var context = ClinicTestFactory.CreateContext();
        var seeded = await ClinicTestFactory.SeedClinicAsync(context);
        var room = await CreateClinicRoomAsync(context);
        var service = ClinicTestFactory.CreateDoctorClinicAssignmentService(context);
        await service.CreateAsync(new UpsertDoctorClinicAssignmentDto
        {
            DoctorId = seeded.Doctor.Id,
            ClinicRoomId = room.Id,
            ActiveFrom = new DateTime(2026, 1, 1),
            ActiveTo = new DateTime(2026, 6, 1)
        });

        var result = await service.CreateAsync(new UpsertDoctorClinicAssignmentDto
        {
            DoctorId = seeded.Doctor.Id,
            ClinicRoomId = room.Id,
            ActiveFrom = new DateTime(2026, 3, 1),
            ActiveTo = new DateTime(2026, 4, 1)
        });

        Assert.False(result.Success);
        Assert.Equal("Doctor already has an overlapping assignment to this clinic room.", result.Message);
    }

    [Fact]
    public async Task CreateDoctorClinicAssignment_AllowsNonOverlappingSameDoctorRoomPeriod()
    {
        await using var context = ClinicTestFactory.CreateContext();
        var seeded = await ClinicTestFactory.SeedClinicAsync(context);
        var room = await CreateClinicRoomAsync(context);
        var service = ClinicTestFactory.CreateDoctorClinicAssignmentService(context);
        await service.CreateAsync(new UpsertDoctorClinicAssignmentDto
        {
            DoctorId = seeded.Doctor.Id,
            ClinicRoomId = room.Id,
            ActiveFrom = new DateTime(2026, 1, 1),
            ActiveTo = new DateTime(2026, 6, 1)
        });

        var result = await service.CreateAsync(new UpsertDoctorClinicAssignmentDto
        {
            DoctorId = seeded.Doctor.Id,
            ClinicRoomId = room.Id,
            ActiveFrom = new DateTime(2026, 6, 1),
            ActiveTo = new DateTime(2026, 12, 1)
        });

        Assert.True(result.Success);
    }

    [Fact]
    public async Task CreateDoctorClinicAssignment_ReturnsError_WhenPrimaryAssignmentPeriodOverlaps()
    {
        await using var context = ClinicTestFactory.CreateContext();
        var seeded = await ClinicTestFactory.SeedClinicAsync(context);
        var room = await CreateClinicRoomAsync(context);
        var otherRoom = await CreateClinicRoomAsync(context);
        var service = ClinicTestFactory.CreateDoctorClinicAssignmentService(context);
        await service.CreateAsync(new UpsertDoctorClinicAssignmentDto
        {
            DoctorId = seeded.Doctor.Id,
            ClinicRoomId = room.Id,
            IsPrimary = true,
            ActiveFrom = new DateTime(2026, 1, 1),
            ActiveTo = new DateTime(2026, 6, 1)
        });

        var result = await service.CreateAsync(new UpsertDoctorClinicAssignmentDto
        {
            DoctorId = seeded.Doctor.Id,
            ClinicRoomId = otherRoom.Id,
            IsPrimary = true,
            ActiveFrom = new DateTime(2026, 2, 1),
            ActiveTo = new DateTime(2026, 3, 1)
        });

        Assert.False(result.Success);
        Assert.Equal("Doctor already has a primary clinic room assignment during this period.", result.Message);
    }

    [Fact]
    public async Task CreateDoctorClinicAssignment_ReturnsError_WhenActiveToIsBeforeActiveFrom()
    {
        await using var context = ClinicTestFactory.CreateContext();
        var seeded = await ClinicTestFactory.SeedClinicAsync(context);
        var room = await CreateClinicRoomAsync(context);
        var service = ClinicTestFactory.CreateDoctorClinicAssignmentService(context);

        var result = await service.CreateAsync(new UpsertDoctorClinicAssignmentDto
        {
            DoctorId = seeded.Doctor.Id,
            ClinicRoomId = room.Id,
            ActiveFrom = new DateTime(2026, 6, 1),
            ActiveTo = new DateTime(2026, 1, 1)
        });

        Assert.False(result.Success);
        Assert.Equal("Assignment ActiveTo must be after ActiveFrom.", result.Message);
    }

    [Fact]
    public async Task CreateAppointment_ReturnsError_WhenDoctorIsNotAssignedToClinicRoom()
    {
        await using var context = ClinicTestFactory.CreateContext();
        var seeded = await ClinicTestFactory.SeedClinicAsync(context);
        var room = await CreateClinicRoomAsync(context);
        var service = ClinicTestFactory.CreateAppointmentService(context);

        var result = await service.CreateAsync(new CreateAppointmentDto
        {
            DoctorId = seeded.Doctor.Id,
            PatientId = seeded.Patient.Id,
            ClinicRoomId = room.Id,
            AppointmentDate = ClinicTestFactory.NextMondayAt(9, 0)
        });

        Assert.False(result.Success);
        Assert.Equal("Doctor is not assigned to the selected clinic room.", result.Message);
    }

    [Fact]
    public async Task CreateAppointment_Succeeds_WhenDoctorIsAssignedToClinicRoom()
    {
        await using var context = ClinicTestFactory.CreateContext();
        var seeded = await ClinicTestFactory.SeedClinicAsync(context);
        var room = await CreateClinicRoomAsync(context);
        await ClinicTestFactory.CreateDoctorClinicAssignmentService(context).CreateAsync(new UpsertDoctorClinicAssignmentDto
        {
            DoctorId = seeded.Doctor.Id,
            ClinicRoomId = room.Id,
            ActiveFrom = DateTime.Today.AddDays(-1)
        });
        var service = ClinicTestFactory.CreateAppointmentService(context);

        var result = await service.CreateAsync(new CreateAppointmentDto
        {
            DoctorId = seeded.Doctor.Id,
            PatientId = seeded.Patient.Id,
            ClinicRoomId = room.Id,
            AppointmentDate = ClinicTestFactory.NextMondayAt(9, 0)
        });

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(room.Id, result.Data.ClinicRoomId);
    }

    [Fact]
    public async Task CreateAppointment_ReturnsError_WhenClinicRoomAssignmentEndsBeforeAppointmentEnds()
    {
        await using var context = ClinicTestFactory.CreateContext();
        var seeded = await ClinicTestFactory.SeedClinicAsync(context);
        var room = await CreateClinicRoomAsync(context);
        var appointmentDate = ClinicTestFactory.NextMondayAt(9, 0);
        await ClinicTestFactory.CreateDoctorClinicAssignmentService(context).CreateAsync(new UpsertDoctorClinicAssignmentDto
        {
            DoctorId = seeded.Doctor.Id,
            ClinicRoomId = room.Id,
            ActiveFrom = appointmentDate.AddDays(-1),
            ActiveTo = appointmentDate.AddMinutes(15)
        });
        var service = ClinicTestFactory.CreateAppointmentService(context);

        var result = await service.CreateAsync(new CreateAppointmentDto
        {
            DoctorId = seeded.Doctor.Id,
            PatientId = seeded.Patient.Id,
            ClinicRoomId = room.Id,
            AppointmentDate = appointmentDate,
            DurationMinutes = 30
        });

        Assert.False(result.Success);
        Assert.Equal("Doctor is not assigned to the selected clinic room.", result.Message);
    }

    [Fact]
    public async Task CreateAppointment_ReturnsError_WhenClinicRoomHasOverlappingAppointment()
    {
        await using var context = ClinicTestFactory.CreateContext();
        var seeded = await ClinicTestFactory.SeedClinicAsync(context);
        var room = await CreateClinicRoomAsync(context);
        await AssignDoctorToRoomAsync(context, seeded.Doctor.Id, room.Id);
        await AssignDoctorToRoomAsync(context, seeded.OtherDoctor.Id, room.Id);
        context.Appointments.Add(WithRoom(ClinicTestFactory.CreateAppointment(
            seeded.OtherDoctor,
            seeded.OtherPatient,
            ClinicTestFactory.NextMondayAt(9, 0),
            AppointmentStatus.Confirmed,
            durationMinutes: 30), room.Id));
        await context.SaveChangesAsync();
        var service = ClinicTestFactory.CreateAppointmentService(context);

        var result = await service.CreateAsync(new CreateAppointmentDto
        {
            DoctorId = seeded.Doctor.Id,
            PatientId = seeded.Patient.Id,
            ClinicRoomId = room.Id,
            AppointmentDate = ClinicTestFactory.NextMondayAt(9, 15),
            DurationMinutes = 30
        });

        Assert.False(result.Success);
        Assert.Equal("This clinic room already has an appointment at this time.", result.Message);
    }

    [Fact]
    public async Task CreateAppointment_AllowsSameClinicRoomWhenSlotDoesNotOverlap()
    {
        await using var context = ClinicTestFactory.CreateContext();
        var seeded = await ClinicTestFactory.SeedClinicAsync(context);
        var room = await CreateClinicRoomAsync(context);
        await AssignDoctorToRoomAsync(context, seeded.Doctor.Id, room.Id);
        await AssignDoctorToRoomAsync(context, seeded.OtherDoctor.Id, room.Id);
        context.Appointments.Add(WithRoom(ClinicTestFactory.CreateAppointment(
            seeded.OtherDoctor,
            seeded.OtherPatient,
            ClinicTestFactory.NextMondayAt(9, 0),
            AppointmentStatus.Confirmed,
            durationMinutes: 30), room.Id));
        await context.SaveChangesAsync();
        var service = ClinicTestFactory.CreateAppointmentService(context);

        var result = await service.CreateAsync(new CreateAppointmentDto
        {
            DoctorId = seeded.Doctor.Id,
            PatientId = seeded.Patient.Id,
            ClinicRoomId = room.Id,
            AppointmentDate = ClinicTestFactory.NextMondayAt(9, 30),
            DurationMinutes = 30
        });

        Assert.True(result.Success);
    }

    [Fact]
    public async Task CreateAppointment_IgnoresCancelledClinicRoomAppointment()
    {
        await using var context = ClinicTestFactory.CreateContext();
        var seeded = await ClinicTestFactory.SeedClinicAsync(context);
        var room = await CreateClinicRoomAsync(context);
        await AssignDoctorToRoomAsync(context, seeded.Doctor.Id, room.Id);
        await AssignDoctorToRoomAsync(context, seeded.OtherDoctor.Id, room.Id);
        context.Appointments.Add(WithRoom(ClinicTestFactory.CreateAppointment(
            seeded.OtherDoctor,
            seeded.OtherPatient,
            ClinicTestFactory.NextMondayAt(9, 0),
            AppointmentStatus.Cancelled,
            durationMinutes: 30), room.Id));
        await context.SaveChangesAsync();
        var service = ClinicTestFactory.CreateAppointmentService(context);

        var result = await service.CreateAsync(new CreateAppointmentDto
        {
            DoctorId = seeded.Doctor.Id,
            PatientId = seeded.Patient.Id,
            ClinicRoomId = room.Id,
            AppointmentDate = ClinicTestFactory.NextMondayAt(9, 15),
            DurationMinutes = 30
        });

        Assert.True(result.Success);
    }

    private static async Task<Riaya.Api.DTOs.Department.DepartmentDto> CreateDepartmentAsync(Riaya.Api.Data.AppDbContext context)
    {
        var result = await ClinicTestFactory.CreateDepartmentService(context).CreateAsync(new UpsertDepartmentDto
        {
            Name = $"Department {Guid.NewGuid():N}"
        });

        return result.Data!;
    }

    private static async Task<Riaya.Api.DTOs.ClinicRoom.ClinicRoomDto> CreateClinicRoomAsync(Riaya.Api.Data.AppDbContext context)
    {
        var department = await CreateDepartmentAsync(context);
        var result = await ClinicTestFactory.CreateClinicRoomService(context).CreateAsync(new UpsertClinicRoomDto
        {
            Name = $"Clinic {Guid.NewGuid():N}",
            RoomNumber = $"R-{Guid.NewGuid():N}"[..10],
            DepartmentId = department.Id
        });

        return result.Data!;
    }

    private static async Task AssignDoctorToRoomAsync(Riaya.Api.Data.AppDbContext context, int doctorId, int roomId)
    {
        await ClinicTestFactory.CreateDoctorClinicAssignmentService(context).CreateAsync(new UpsertDoctorClinicAssignmentDto
        {
            DoctorId = doctorId,
            ClinicRoomId = roomId,
            ActiveFrom = DateTime.Today.AddDays(-1)
        });
    }

    private static Appointment WithRoom(Appointment appointment, int roomId)
    {
        appointment.ClinicRoomId = roomId;
        return appointment;
    }
}
