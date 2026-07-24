namespace Riaya.Api.DTOs.Appointment;

public class AppointmentDto
{
    public int Id { get; set; }
    public DateTime AppointmentDate { get; set; }
    public int DurationMinutes { get; set; }
    public string Status { get; set; } = string.Empty;

    public int DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;

    public int PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;

    public int? ClinicRoomId { get; set; }
    public string? ClinicRoomName { get; set; }

    public string SpecializationName { get; set; } = string.Empty;
}
