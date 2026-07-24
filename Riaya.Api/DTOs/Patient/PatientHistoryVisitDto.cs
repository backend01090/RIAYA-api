namespace Riaya.Api.DTOs.Patient;

public class PatientHistoryVisitDto
{
    public int VisitId { get; set; }
    public int AppointmentId { get; set; }
    public DateTime AppointmentDate { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string Symptoms { get; set; } = string.Empty;
    public string Diagnosis { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;

    public List<PatientHistoryPrescriptionDto> Prescriptions { get; set; } = new();
}
