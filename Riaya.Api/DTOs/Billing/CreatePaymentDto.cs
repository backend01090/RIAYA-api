using System.ComponentModel.DataAnnotations;
using Riaya.Api.Enums;

namespace Riaya.Api.DTOs.Billing;

public class CreatePaymentDto
{
    [Range(1, int.MaxValue)]
    public int InvoiceId { get; set; }

    [Range(typeof(decimal), "0.01", "999999999")]
    public decimal Amount { get; set; }

    public PaymentMethod Method { get; set; } = PaymentMethod.Cash;

    [MaxLength(500)]
    public string? Notes { get; set; }
}
