using Riaya.Api.Common;
using Riaya.Api.DTOs.Billing;

namespace Riaya.Api.Interfaces;

public interface IInvoiceService
{
    Task<List<InvoiceDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<InvoiceDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult<InvoiceDto>> CreateAsync(CreateInvoiceDto dto, CancellationToken cancellationToken = default);
    Task<ServiceResult<InvoiceDto>> AddItemAsync(int invoiceId, CreateInvoiceItemDto dto, CancellationToken cancellationToken = default);
    Task<ServiceResult<InvoiceDto>> CancelAsync(int invoiceId, CancellationToken cancellationToken = default);
}
