using Riaya.Api.Interfaces;
using Riaya.Api.Services;

namespace Riaya.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IDoctorService, DoctorService>();
        services.AddScoped<IAppointmentService, AppointmentService>();
        services.AddScoped<IVisitService, VisitService>();
        services.AddScoped<IPatientService, PatientService>();
        services.AddScoped<ISpecializationService, SpecializationService>();
        services.AddScoped<IDoctorScheduleService, DoctorScheduleService>();
        services.AddScoped<IPrescriptionService, PrescriptionService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IDepartmentService, DepartmentService>();
        services.AddScoped<IClinicRoomService, ClinicRoomService>();
        services.AddScoped<IDoctorClinicAssignmentService, DoctorClinicAssignmentService>();
        services.AddScoped<IMedicalServiceService, MedicalServiceService>();
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<IPaymentService, PaymentService>();

        return services;
    }
}
