using System.ComponentModel.DataAnnotations;

namespace Riaya.Api.DTOs.Appointment;

public class CreateAppointmentDto
{
    [Range(1, int.MaxValue)]
    public int DoctorId { get; set; }

    [Range(1, int.MaxValue)]
    public int PatientId { get; set; }

    [Range(1, int.MaxValue)]
    public int? ClinicRoomId { get; set; }

    [Required]
    public DateTime AppointmentDate { get; set; }

    [Range(5, 240)]
    public int? DurationMinutes { get; set; }
}
