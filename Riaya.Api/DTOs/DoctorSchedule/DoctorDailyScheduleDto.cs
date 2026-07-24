namespace Riaya.Api.DTOs.DoctorSchedule;

public class DoctorDailyScheduleDto
{
    public int DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string SpecializationName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public bool IsWorkingDay { get; set; }
    public List<DoctorDailyWorkingSlotDto> WorkingSlots { get; set; } = new();
    public List<DoctorDailyScheduleItemDto> Appointments { get; set; } = new();
}
