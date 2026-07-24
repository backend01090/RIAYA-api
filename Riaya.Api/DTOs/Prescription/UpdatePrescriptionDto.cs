using System.ComponentModel.DataAnnotations;

namespace Riaya.Api.DTOs.Prescription;

public class UpdatePrescriptionDto
{
    [Required]
    [StringLength(200, MinimumLength = 2)]
    public string MedicationName { get; set; } = string.Empty;

    [Required]
    [StringLength(200, MinimumLength = 2)]
    public string Dosage { get; set; } = string.Empty;

    [Required]
    [StringLength(1000, MinimumLength = 2)]
    public string Instructions { get; set; } = string.Empty;

    [Range(1, 365)]
    public int DurationInDays { get; set; }
}

