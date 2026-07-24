namespace Riaya.Api.Entities;

public class InvoiceItem : BaseEntity
{
    public int Id { get; set; }

    public int InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }

    public int? MedicalServiceId { get; set; }
    public MedicalService? MedicalService { get; set; }

    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}
