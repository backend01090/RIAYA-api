using System.ComponentModel.DataAnnotations;

namespace Riaya.Api.DTOs.Billing;

public class UpsertMedicalServiceDto
{
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "999999999")]
    public decimal Price { get; set; }

    public bool IsActive { get; set; } = true;
}
