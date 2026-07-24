namespace Riaya.Api.DTOs.DoctorSchedule;

public class DoctorDailyScheduleItemDto
{
    public int AppointmentId { get; set; }
    public DateTime AppointmentDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public int PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
}
