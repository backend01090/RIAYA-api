using System.ComponentModel.DataAnnotations;

namespace Riaya.Api.DTOs.Patient;

public class CreatePatientDto
{
    [Required]
    [StringLength(100, MinimumLength = 3)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [Phone]
    [StringLength(20, MinimumLength = 7)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    public DateTime DateOfBirth { get; set; }

    [Required]
    [StringLength(20, MinimumLength = 4)]
    public string Gender { get; set; } = string.Empty;
}
