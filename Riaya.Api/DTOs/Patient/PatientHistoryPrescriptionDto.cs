namespace Riaya.Api.DTOs.Patient;

public class PatientHistoryPrescriptionDto
{
    public int PrescriptionId { get; set; }
    public string MedicationName { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty;
    public string Instructions { get; set; } = string.Empty;
    public int DurationInDays { get; set; }
}
