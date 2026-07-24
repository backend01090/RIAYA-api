using System.ComponentModel.DataAnnotations;

namespace Riaya.Api.DTOs.DoctorSchedule;

public class CreateDoctorScheduleDto : IValidatableObject
{
    [Range(1, int.MaxValue)]
    public int DoctorId { get; set; }

    [Required]
    public DayOfWeek DayOfWeek { get; set; }

    [Required]
    public TimeSpan StartTime { get; set; }

    [Required]
    public TimeSpan EndTime { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Enum.IsDefined(typeof(DayOfWeek), DayOfWeek))
        {
            yield return new ValidationResult(
                "Day of week must be a valid value.",
                new[] { nameof(DayOfWeek) });
        }

        if (StartTime >= EndTime)
        {
            yield return new ValidationResult(
                "Start time must be earlier than end time.",
                new[] { nameof(StartTime), nameof(EndTime) });
        }

        if (StartTime < TimeSpan.Zero || StartTime >= TimeSpan.FromDays(1))
        {
            yield return new ValidationResult(
                "Start time must be within a valid 24-hour range.",
                new[] { nameof(StartTime) });
        }

        if (EndTime <= TimeSpan.Zero || EndTime > TimeSpan.FromDays(1))
        {
            yield return new ValidationResult(
                "End time must be within a valid 24-hour range.",
                new[] { nameof(EndTime) });
        }
    }
}

