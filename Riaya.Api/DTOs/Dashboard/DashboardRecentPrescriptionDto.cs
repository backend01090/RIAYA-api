namespace Riaya.Api.DTOs.Dashboard;

public class DashboardRecentPrescriptionDto
{
    public int PrescriptionId { get; set; }
    public string MedicationName { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public DateTime AppointmentDate { get; set; }
}
