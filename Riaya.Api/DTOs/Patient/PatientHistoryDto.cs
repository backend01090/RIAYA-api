namespace Riaya.Api.DTOs.Patient;

public class PatientHistoryDto
{
    public int PatientId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;

    public int TotalAppointments { get; set; }
    public int TotalVisits { get; set; }
    public DateTime? LastVisitDate { get; set; }

    public List<PatientHistoryVisitDto> Visits { get; set; } = new();
}
