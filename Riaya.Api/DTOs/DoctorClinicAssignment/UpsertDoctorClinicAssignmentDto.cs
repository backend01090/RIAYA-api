using System.ComponentModel.DataAnnotations;

namespace Riaya.Api.DTOs.DoctorClinicAssignment;

public class UpsertDoctorClinicAssignmentDto
{
    [Range(1, int.MaxValue)]
    public int DoctorId { get; set; }

    [Range(1, int.MaxValue)]
    public int ClinicRoomId { get; set; }

    public bool IsPrimary { get; set; }

    [Required]
    public DateTime ActiveFrom { get; set; }

    public DateTime? ActiveTo { get; set; }
}
