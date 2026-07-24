namespace Riaya.Api.DTOs.Dashboard;

public class DashboardRecentAppointmentDto
{
    public int AppointmentId { get; set; }
    public DateTime AppointmentDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string SpecializationName { get; set; } = string.Empty;
}
