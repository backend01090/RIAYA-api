using Riaya.Api.Common;
using Riaya.Api.DTOs.Billing;

namespace Riaya.Api.Interfaces;

public interface IPaymentService
{
    Task<List<PaymentDto>> GetByInvoiceIdAsync(int invoiceId, CancellationToken cancellationToken = default);
    Task<ServiceResult<PaymentDto>> CreateAsync(CreatePaymentDto dto, CancellationToken cancellationToken = default);
}
