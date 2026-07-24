using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Riaya.Api.Constants;
using Riaya.Api.DTOs.Appointment;
using Riaya.Api.Enums;
using Riaya.Tests.TestSupport;

namespace Riaya.Tests;

public class ApiIntegrationTests
{
    private const string StrongPassword = "Admin@12345";

    [Fact]
    public async Task Root_ReturnsRunningMessage()
    {
        using var factory = new ClinicWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/");

        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(json.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal("Riaya.Api API is running.", json.RootElement.GetProperty("data").GetString());
    }

    [Fact]
    public async Task Health_ReturnsOkWithoutAuthentication()
    {
        using var factory = new ClinicWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Appointments_ReturnsUnauthorized_WhenUserIsAnonymous()
    {
        using var factory = new ClinicWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/appointments");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task VersionedAppointmentsRoute_ReturnsOk_WhenUserIsClinicStaff()
    {
        using var factory = new ClinicWebApplicationFactory(
            "admin-user",
            AppRoles.Admin);
        await factory.SeedClinicAsync();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/appointments");

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task DoctorsMe_ReturnsCurrentDoctor_WhenUserIsAuthenticatedDoctor()
    {
        using var factory = new ClinicWebApplicationFactory(
            ClinicTestFactory.DoctorUserId,
            AppRoles.Doctor);
        var seeded = await factory.SeedClinicAsync();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/doctors/me");

        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var data = json.RootElement.GetProperty("data");

        Assert.True(json.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal(seeded.Doctor.Id, data.GetProperty("doctorId").GetInt32());
        Assert.Equal(ClinicTestFactory.DoctorUserId, data.GetProperty("userId").GetString());
        Assert.Equal("Dr. Ahmed Ali", data.GetProperty("fullName").GetString());
        Assert.Equal("doctor1@example.com", data.GetProperty("email").GetString());
        Assert.Equal("Cardiology", data.GetProperty("specializationName").GetString());
        Assert.True(data.GetProperty("isActive").GetBoolean());
    }

    [Fact]
    public async Task DoctorsMe_ReturnsUnauthorized_WhenUserIsAnonymous()
    {
        using var factory = new ClinicWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/doctors/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DoctorsMe_ReturnsNotFound_WhenDoctorUserHasNoLinkedDoctorProfile()
    {
        const string userId = "doctor-without-profile";
        using var factory = new ClinicWebApplicationFactory(userId, AppRoles.Doctor);
        await factory.CreateUserAsync(
            "doctor-without-profile@example.com",
            StrongPassword,
            AppRoles.Doctor,
            "Dr. Missing Profile",
            userId);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/doctors/me");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateAppointment_ReturnsForbidden_WhenUserIsDoctor()
    {
        using var factory = new ClinicWebApplicationFactory(
            ClinicTestFactory.DoctorUserId,
            AppRoles.Doctor);
        var seeded = await factory.SeedClinicAsync();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/appointments",
            ClinicTestFactory.CreateAppointmentDto(
                seeded,
                ClinicTestFactory.NextMondayAt(9, 0)));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Appointments_ReturnsOnlyCurrentDoctorAppointments_WhenUserIsDoctor()
    {
        using var factory = new ClinicWebApplicationFactory(
            ClinicTestFactory.DoctorUserId,
            AppRoles.Doctor);
        var seeded = await factory.SeedClinicAsync();

        await factory.ExecuteDbContextAsync(async context =>
        {
            context.Appointments.AddRange(
                ClinicTestFactory.CreateAppointment(
                    seeded.Doctor,
                    seeded.Patient,
                    ClinicTestFactory.NextMondayAt(9, 0),
                    AppointmentStatus.Confirmed),
                ClinicTestFactory.CreateAppointment(
                    seeded.OtherDoctor,
                    seeded.OtherPatient,
                    ClinicTestFactory.NextMondayAt(9, 15),
                    AppointmentStatus.Confirmed));

            await context.SaveChangesAsync();
        });

        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/appointments");

        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var items = json.RootElement.GetProperty("data").GetProperty("items");

        Assert.Equal(1, items.GetArrayLength());
        Assert.Equal(seeded.Doctor.Id, items[0].GetProperty("doctorId").GetInt32());
    }

    [Fact]
    public async Task CreateAppointment_ReturnsCreated_WhenReceptionistSendsValidRequest()
    {
        using var factory = new ClinicWebApplicationFactory(
            "receptionist-user",
            AppRoles.Receptionist);
        var seeded = await factory.SeedClinicAsync();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/appointments",
            new CreateAppointmentDto
            {
                DoctorId = seeded.Doctor.Id,
                PatientId = seeded.Patient.Id,
                AppointmentDate = ClinicTestFactory.NextMondayAt(9, 0)
            });

        Assert.True(
            response.StatusCode == HttpStatusCode.Created,
            await response.Content.ReadAsStringAsync());
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(json.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal("Pending", json.RootElement.GetProperty("data").GetProperty("status").GetString());
    }
}

