using Riaya.Api.Enums;

namespace Riaya.Api.Entities;

public class Appointment : BaseEntity
{
    public int Id { get; set; }

    public int DoctorId { get; set; }
    public Doctor? Doctor { get; set; }

    public int PatientId { get; set; }
    public Patient? Patient { get; set; }

    public int? ClinicRoomId { get; set; }
    public ClinicRoom? ClinicRoom { get; set; }

    public DateTime AppointmentDate { get; set; }
    public int DurationMinutes { get; set; } = 30;
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;
}
