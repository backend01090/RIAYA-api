using System.ComponentModel.DataAnnotations;

namespace Riaya.Api.DTOs.Specialization;

public class CreateSpecializationDto
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;
}
