using System.ComponentModel.DataAnnotations;

namespace Riaya.Api.DTOs.Visit;

public class UpdateVisitDto
{
    [Required]
    [StringLength(1000, MinimumLength = 2)]
    public string Symptoms { get; set; } = string.Empty;

    [Required]
    [StringLength(1000, MinimumLength = 2)]
    public string Diagnosis { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Notes { get; set; }
}

