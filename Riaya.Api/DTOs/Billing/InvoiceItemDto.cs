namespace Riaya.Api.DTOs.Billing;

public class InvoiceItemDto
{
    public int Id { get; set; }
    public int? MedicalServiceId { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}
