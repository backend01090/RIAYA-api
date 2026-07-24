using Riaya.Api.Constants;
using Riaya.Api.DTOs.Doctor;
using Riaya.Api.DTOs.Prescription;
using Riaya.Api.DTOs.Visit;
using Riaya.Api.Entities;
using Riaya.Api.Enums;
using Riaya.Tests.TestSupport;

namespace Riaya.Tests;

public class PatientPrivacyTests
{
    [Fact]
    public async Task Doctor_CannotReadUnrelatedPatientHistory()
    {
        await using var context = ClinicTestFactory.CreateContext(ClinicTestFactory.DoctorUserId);
        var seeded = await SeedClinicalHistoryAsync(context);
        var service = ClinicTestFactory.CreatePatientService(
            context,
            ClinicTestFactory.DoctorUserId,
            AppRoles.Doctor);

        var result = await service.GetHistoryAsync(seeded.OtherPatient.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task Doctor_CanReadOwnPatientHistory()
    {
        await using var context = ClinicTestFactory.CreateContext(ClinicTestFactory.DoctorUserId);
        var seeded = await SeedClinicalHistoryAsync(context);
        var service = ClinicTestFactory.CreatePatientService(
            context,
            ClinicTestFactory.DoctorUserId,
            AppRoles.Doctor);

        var result = await service.GetHistoryAsync(seeded.Patient.Id);

        Assert.NotNull(result);
        var visit = Assert.Single(result.Visits);
        Assert.Equal("Migraine", visit.Diagnosis);
        Assert.Single(visit.Prescriptions);
    }

    [Fact]
    public async Task Receptionist_CannotSeeDiagnosisNotesOrPrescriptionDetails()
    {
        await using var context = ClinicTestFactory.CreateContext("receptionist-user");
        var seeded = await SeedClinicalHistoryAsync(context);
        var service = ClinicTestFactory.CreatePatientService(
            context,
            "receptionist-user",
            AppRoles.Receptionist);

        var result = await service.GetHistoryAsync(seeded.Patient.Id);

        Assert.NotNull(result);
        var visit = Assert.Single(result.Visits);
        Assert.Equal(string.Empty, visit.Symptoms);
        Assert.Equal(string.Empty, visit.Diagnosis);
        Assert.Equal(string.Empty, visit.Notes);
        Assert.Empty(visit.Prescriptions);
    }

    [Fact]
    public async Task Admin_CanSeeFullPatientHistory()
    {
        await using var context = ClinicTestFactory.CreateContext("admin-user");
        var seeded = await SeedClinicalHistoryAsync(context);
        var service = ClinicTestFactory.CreatePatientService(
            context,
            "admin-user",
            AppRoles.Admin);

        var result = await service.GetHistoryAsync(seeded.Patient.Id);

        Assert.NotNull(result);
        var visit = Assert.Single(result.Visits);
        Assert.Equal("Migraine", visit.Diagnosis);
        Assert.Equal("Neurology follow-up if symptoms persist.", visit.Notes);
        Assert.Equal("Ibuprofen", Assert.Single(visit.Prescriptions).MedicationName);
    }

    [Fact]
    public async Task Receptionist_SearchByDiagnosis_DoesNotRevealMatchingVisits()
    {
        await using var context = ClinicTestFactory.CreateContext("receptionist-user");
        await SeedClinicalHistoryAsync(context);
        var service = ClinicTestFactory.CreateVisitService(
            context,
            "receptionist-user",
            AppRoles.Receptionist);

        var result = await service.GetAllAsync(new VisitQueryParams
        {
            Search = "Migraine"
        });

        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task Receptionist_SearchByMedicationName_DoesNotRevealMatchingPrescriptions()
    {
        await using var context = ClinicTestFactory.CreateContext("receptionist-user");
        await SeedClinicalHistoryAsync(context);
        var service = ClinicTestFactory.CreatePrescriptionService(
            context,
            "receptionist-user",
            AppRoles.Receptionist);

        var result = await service.GetAllAsync(new PrescriptionQueryParams
        {
            Search = "Ibuprofen"
        });

        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task Admin_CanSearchFullClinicalData()
    {
        await using var context = ClinicTestFactory.CreateContext("admin-user");
        await SeedClinicalHistoryAsync(context);
        var visitService = ClinicTestFactory.CreateVisitService(context, "admin-user", AppRoles.Admin);
        var prescriptionService = ClinicTestFactory.CreatePrescriptionService(context, "admin-user", AppRoles.Admin);

        var visits = await visitService.GetAllAsync(new VisitQueryParams { Search = "Migraine" });
        var prescriptions = await prescriptionService.GetAllAsync(new PrescriptionQueryParams { Search = "Ibuprofen" });

        Assert.Single(visits.Items);
        Assert.Single(prescriptions.Items);
    }

    [Fact]
    public async Task Doctor_SearchByOtherDoctorClinicalData_ReturnsNoResults()
    {
        await using var context = ClinicTestFactory.CreateContext(ClinicTestFactory.DoctorUserId);
        await SeedClinicalHistoryAsync(context);
        var visitService = ClinicTestFactory.CreateVisitService(
            context,
            ClinicTestFactory.DoctorUserId,
            AppRoles.Doctor);
        var prescriptionService = ClinicTestFactory.CreatePrescriptionService(
            context,
            ClinicTestFactory.DoctorUserId,
            AppRoles.Doctor);

        var visits = await visitService.GetAllAsync(new VisitQueryParams { Search = "Bronchitis" });
        var prescriptions = await prescriptionService.GetAllAsync(new PrescriptionQueryParams { Search = "Amoxicillin" });

        Assert.Empty(visits.Items);
        Assert.Empty(prescriptions.Items);
    }

    [Fact]
    public async Task CreateDoctor_ReturnsError_WhenApplicationUserIsNotDoctor()
    {
        await using var context = ClinicTestFactory.CreateContext();
        var specialization = new Specialization { Name = "Cardiology" };
        var user = new ApplicationUser
        {
            Id = "receptionist-user",
            FullName = "Reception User",
            Email = "reception@example.com",
            UserName = "reception@example.com",
            EmailConfirmed = true
        };

        context.Specializations.Add(specialization);
        context.Users.Add(user);
        await context.SaveChangesAsync();
        await ClinicTestFactory.AddUserRoleAsync(context, user, AppRoles.Receptionist);

        var service = ClinicTestFactory.CreateDoctorService(context);

        var result = await service.CreateAsync(new CreateDoctorDto
        {
            FullName = "Dr. Wrong Link",
            PhoneNumber = "07770000000",
            SpecializationId = specialization.Id,
            ApplicationUserId = user.Id
        });

        Assert.False(result.Success);
        Assert.Equal("Application user must have the Doctor role before linking to a doctor profile.", result.Message);
    }

    private static async Task<SeededClinic> SeedClinicalHistoryAsync(Riaya.Api.Data.AppDbContext context)
    {
        var seeded = await ClinicTestFactory.SeedClinicAsync(context);

        var ownAppointment = ClinicTestFactory.CreateAppointment(
            seeded.Doctor,
            seeded.Patient,
            ClinicTestFactory.PreviousMondayAt(9, 0),
            AppointmentStatus.Completed);

        var otherAppointment = ClinicTestFactory.CreateAppointment(
            seeded.OtherDoctor,
            seeded.OtherPatient,
            ClinicTestFactory.PreviousMondayAt(10, 0),
            AppointmentStatus.Completed);

        context.Appointments.AddRange(ownAppointment, otherAppointment);
        await context.SaveChangesAsync();

        var ownVisit = new Visit
        {
            AppointmentId = ownAppointment.Id,
            Symptoms = "Headache",
            Diagnosis = "Migraine",
            Notes = "Neurology follow-up if symptoms persist."
        };

        var otherVisit = new Visit
        {
            AppointmentId = otherAppointment.Id,
            Symptoms = "Cough",
            Diagnosis = "Bronchitis",
            Notes = "Hydration and rest."
        };

        context.Visits.AddRange(ownVisit, otherVisit);
        await context.SaveChangesAsync();

        context.Prescriptions.AddRange(
            new Prescription
            {
                VisitId = ownVisit.Id,
                MedicationName = "Ibuprofen",
                Dosage = "400 mg",
                Instructions = "After food",
                DurationInDays = 3
            },
            new Prescription
            {
                VisitId = otherVisit.Id,
                MedicationName = "Amoxicillin",
                Dosage = "500 mg",
                Instructions = "Every 8 hours",
                DurationInDays = 7
            });

        await context.SaveChangesAsync();
        return seeded;
    }
}
