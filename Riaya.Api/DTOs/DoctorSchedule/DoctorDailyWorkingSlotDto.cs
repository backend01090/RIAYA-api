namespace Riaya.Api.DTOs.DoctorSchedule;

public class DoctorDailyWorkingSlotDto
{
    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
}
