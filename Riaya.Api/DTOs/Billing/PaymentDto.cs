namespace Riaya.Api.DTOs.Billing;

public class PaymentDto
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = string.Empty;
    public DateTime PaidAtUtc { get; set; }
    public string? ReceivedByUserId { get; set; }
    public string? Notes { get; set; }
}
