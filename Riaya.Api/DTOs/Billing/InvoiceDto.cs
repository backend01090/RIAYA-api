namespace Riaya.Api.DTOs.Billing;

public class InvoiceDto
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public int? AppointmentId { get; set; }
    public int? VisitId { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime IssuedAtUtc { get; set; }
    public List<InvoiceItemDto> Items { get; set; } = new();
}
