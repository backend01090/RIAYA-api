namespace Riaya.Api.DTOs.Dashboard;

public class DashboardRecentVisitDto
{
    public int VisitId { get; set; }
    public DateTime AppointmentDate { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string Diagnosis { get; set; } = string.Empty;
}
