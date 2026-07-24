using System.ComponentModel.DataAnnotations;

namespace Riaya.Api.DTOs.Doctor;

public class CreateDoctorDto
{
    [Required]
    [StringLength(100, MinimumLength = 3)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [Phone]
    [StringLength(20, MinimumLength = 7)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int SpecializationId { get; set; }

    public string? ApplicationUserId { get; set; }
}
