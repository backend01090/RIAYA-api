namespace Riaya.Api.DTOs.DoctorClinicAssignment;

public class DoctorClinicAssignmentDto
{
    public int Id { get; set; }
    public int DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public int ClinicRoomId { get; set; }
    public string ClinicRoomName { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public DateTime ActiveFrom { get; set; }
    public DateTime? ActiveTo { get; set; }
}
