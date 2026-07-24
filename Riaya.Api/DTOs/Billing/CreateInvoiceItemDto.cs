using System.ComponentModel.DataAnnotations;

namespace Riaya.Api.DTOs.Billing;

public class CreateInvoiceItemDto
{
    [Range(1, int.MaxValue)]
    public int? MedicalServiceId { get; set; }

    [MaxLength(250)]
    public string? Description { get; set; }

    [Range(1, 1000)]
    public int Quantity { get; set; } = 1;

    [Range(typeof(decimal), "0.01", "999999999")]
    public decimal? UnitPrice { get; set; }
}
