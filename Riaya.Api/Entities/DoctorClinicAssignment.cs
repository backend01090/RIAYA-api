namespace Riaya.Api.Entities;

public class DoctorClinicAssignment : BaseEntity
{
    public int Id { get; set; }

    public int DoctorId { get; set; }
    public Doctor? Doctor { get; set; }

    public int ClinicRoomId { get; set; }
    public ClinicRoom? ClinicRoom { get; set; }

    public bool IsPrimary { get; set; }
    public DateTime ActiveFrom { get; set; }
    public DateTime? ActiveTo { get; set; }
}
