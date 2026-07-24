using Riaya.Api.Common;
using Riaya.Api.Data;
using Riaya.Api.DTOs.Billing;
using Riaya.Api.Enums;
using Riaya.Api.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Riaya.Api.Services;

public class PaymentService : IPaymentService
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public PaymentService(AppDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<List<PaymentDto>> GetByInvoiceIdAsync(int invoiceId, CancellationToken cancellationToken = default)
    {
        return await _context.Payments
            .AsNoTracking()
            .Where(p => p.InvoiceId == invoiceId)
            .OrderByDescending(p => p.PaidAtUtc)
            .Select(p => new PaymentDto
            {
                Id = p.Id,
                InvoiceId = p.InvoiceId,
                Amount = p.Amount,
                Method = p.Method.ToString(),
                PaidAtUtc = p.PaidAtUtc,
                ReceivedByUserId = p.ReceivedByUserId,
                Notes = p.Notes
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ServiceResult<PaymentDto>> CreateAsync(CreatePaymentDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.Amount <= 0)
            return ServiceResult<PaymentDto>.Fail("Payment amount must be greater than zero.", ErrorType.Validation, "InvalidPaymentAmount");

        var invoice = await _context.Invoices
            .Include(i => i.Items)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == dto.InvoiceId, cancellationToken);

        if (invoice is null)
            return ServiceResult<PaymentDto>.Fail("Invoice not found.", ErrorType.NotFound, "InvoiceNotFound");

        if (invoice.Status == InvoiceStatus.Cancelled)
            return ServiceResult<PaymentDto>.Fail("Cancelled invoice cannot receive payments.", ErrorType.BusinessRule, "CancelledInvoice");

        if (dto.Amount > invoice.RemainingAmount)
            return ServiceResult<PaymentDto>.Fail("Payment amount cannot exceed invoice remaining amount.", ErrorType.BusinessRule, "InvoiceOverpayment");

        var payment = new Riaya.Api.Entities.Payment
        {
            InvoiceId = invoice.Id,
            Amount = dto.Amount,
            Method = dto.Method,
            PaidAtUtc = DateTime.UtcNow,
            ReceivedByUserId = _currentUserService.UserId,
            Notes = dto.Notes?.Trim()
        };

        invoice.Payments.Add(payment);
        InvoiceService.RecalculateInvoice(invoice);

        await _context.SaveChangesAsync(cancellationToken);

        return ServiceResult<PaymentDto>.Ok(new PaymentDto
        {
            Id = payment.Id,
            InvoiceId = payment.InvoiceId,
            Amount = payment.Amount,
            Method = payment.Method.ToString(),
            PaidAtUtc = payment.PaidAtUtc,
            ReceivedByUserId = payment.ReceivedByUserId,
            Notes = payment.Notes
        }, "Payment recorded successfully.");
    }
}
