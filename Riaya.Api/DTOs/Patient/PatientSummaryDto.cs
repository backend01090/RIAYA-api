namespace Riaya.Api.DTOs.Patient;

public class PatientSummaryDto
{
    public int PatientId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
    public int TotalAppointments { get; set; }
    public int PendingAppointments { get; set; }
    public int ConfirmedAppointments { get; set; }
    public int CompletedAppointments { get; set; }
    public int CancelledAppointments { get; set; }
    public int TotalVisits { get; set; }
    public int TotalPrescriptions { get; set; }
    public DateTime? LastAppointmentDate { get; set; }
    public DateTime? LastVisitDate { get; set; }
}
