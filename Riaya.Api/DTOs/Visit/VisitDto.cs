namespace Riaya.Api.DTOs.Visit;

public class VisitDto
{
    public int Id { get; set; }
    public int AppointmentId { get; set; }
    public DateTime AppointmentDate { get; set; }

    public string PatientName { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;

    public string Symptoms { get; set; } = string.Empty;
    public string Diagnosis { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}
