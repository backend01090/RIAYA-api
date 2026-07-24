namespace Riaya.Api.DTOs.Prescription;

public class PrescriptionDto
{
    public int Id { get; set; }

    public int VisitId { get; set; }
    public int AppointmentId { get; set; }
    public DateTime AppointmentDate { get; set; }

    public string PatientName { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;

    public string MedicationName { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty;
    public string Instructions { get; set; } = string.Empty;
    public int DurationInDays { get; set; }
}
