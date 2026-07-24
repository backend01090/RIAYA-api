using Riaya.Api.Common;
using Riaya.Api.Data;
using Riaya.Api.DTOs.Billing;
using Riaya.Api.Entities;
using Riaya.Api.Enums;
using Riaya.Api.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Riaya.Api.Services;

public class InvoiceService : IInvoiceService
{
    private readonly AppDbContext _context;

    public InvoiceService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<InvoiceDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await ProjectInvoices(_context.Invoices.AsNoTracking())
            .OrderByDescending(i => i.IssuedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<InvoiceDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await ProjectInvoices(_context.Invoices.AsNoTracking().Where(i => i.Id == id))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ServiceResult<InvoiceDto>> CreateAsync(CreateInvoiceDto dto, CancellationToken cancellationToken = default)
    {
        var validation = await ValidateInvoiceReferencesAsync(dto, cancellationToken);
        if (!validation.Success)
            return ServiceResult<InvoiceDto>.Fail(validation.Message, validation.ErrorType, validation.ErrorCode);

        if (dto.Items.Count == 0)
            return ServiceResult<InvoiceDto>.Fail("Invoice must contain at least one item.", ErrorType.Validation, "InvoiceItemsRequired");

        var invoice = new Invoice
        {
            PatientId = dto.PatientId,
            AppointmentId = dto.AppointmentId,
            VisitId = dto.VisitId,
            IssuedAtUtc = DateTime.UtcNow
        };

        foreach (var itemDto in dto.Items)
        {
            var itemResult = await BuildInvoiceItemAsync(itemDto, cancellationToken);
            if (!itemResult.Success)
                return ServiceResult<InvoiceDto>.Fail(itemResult.Message, itemResult.ErrorType, itemResult.ErrorCode);

            invoice.Items.Add(itemResult.Data!);
        }

        RecalculateInvoice(invoice);
        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync(cancellationToken);

        return ServiceResult<InvoiceDto>.Ok(await GetByIdAsync(invoice.Id, cancellationToken), "Invoice created successfully.");
    }

    public async Task<ServiceResult<InvoiceDto>> AddItemAsync(int invoiceId, CreateInvoiceItemDto dto, CancellationToken cancellationToken = default)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == invoiceId, cancellationToken);

        if (invoice is null)
            return ServiceResult<InvoiceDto>.Fail("Invoice not found.", ErrorType.NotFound, "InvoiceNotFound");

        if (invoice.Status is InvoiceStatus.Paid or InvoiceStatus.PartiallyPaid or InvoiceStatus.Cancelled)
        {
            return ServiceResult<InvoiceDto>.Fail(
                "Invoice items cannot be changed after payment has started or the invoice is cancelled.",
                ErrorType.BusinessRule,
                "InvoiceItemsLocked");
        }

        var itemResult = await BuildInvoiceItemAsync(dto, cancellationToken);
        if (!itemResult.Success)
            return ServiceResult<InvoiceDto>.Fail(itemResult.Message, itemResult.ErrorType, itemResult.ErrorCode);

        invoice.Items.Add(itemResult.Data!);
        RecalculateInvoice(invoice);

        await _context.SaveChangesAsync(cancellationToken);

        return ServiceResult<InvoiceDto>.Ok(await GetByIdAsync(invoice.Id, cancellationToken), "Invoice item added successfully.");
    }

    public async Task<ServiceResult<InvoiceDto>> CancelAsync(int invoiceId, CancellationToken cancellationToken = default)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Items)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == invoiceId, cancellationToken);

        if (invoice is null)
            return ServiceResult<InvoiceDto>.Fail("Invoice not found.", ErrorType.NotFound, "InvoiceNotFound");

        if (invoice.Payments.Any())
        {
            return ServiceResult<InvoiceDto>.Fail(
                "Invoice has payments and requires a refund or reversal workflow before cancellation.",
                ErrorType.BusinessRule,
                "InvoiceHasPayments");
        }

        invoice.Status = InvoiceStatus.Cancelled;
        await _context.SaveChangesAsync(cancellationToken);

        return ServiceResult<InvoiceDto>.Ok(await GetByIdAsync(invoice.Id, cancellationToken), "Invoice cancelled successfully.");
    }

    private async Task<ServiceResult> ValidateInvoiceReferencesAsync(CreateInvoiceDto dto, CancellationToken cancellationToken)
    {
        var patientExists = await _context.Patients.AnyAsync(p => p.Id == dto.PatientId, cancellationToken);
        if (!patientExists)
            return ServiceResult.Fail("Invalid patient id.", ErrorType.Validation, "InvalidPatient");

        if (dto.AppointmentId.HasValue)
        {
            var appointmentValid = await _context.Appointments.AnyAsync(
                a => a.Id == dto.AppointmentId.Value && a.PatientId == dto.PatientId,
                cancellationToken);

            if (!appointmentValid)
                return ServiceResult.Fail("Appointment must belong to the invoice patient.", ErrorType.Validation, "InvalidAppointment");
        }

        if (dto.VisitId.HasValue)
        {
            var visitValid = await _context.Visits.AnyAsync(
                v => v.Id == dto.VisitId.Value &&
                     v.Appointment != null &&
                     v.Appointment.PatientId == dto.PatientId,
                cancellationToken);

            if (!visitValid)
                return ServiceResult.Fail("Visit must belong to the invoice patient.", ErrorType.Validation, "InvalidVisit");
        }

        return ServiceResult.Ok();
    }

    private async Task<ServiceResult<InvoiceItem>> BuildInvoiceItemAsync(CreateInvoiceItemDto dto, CancellationToken cancellationToken)
    {
        if (dto.Quantity <= 0)
            return ServiceResult<InvoiceItem>.Fail("Invoice item quantity must be greater than zero.", ErrorType.Validation, "InvalidQuantity");

        string description;
        decimal unitPrice;

        if (dto.MedicalServiceId.HasValue)
        {
            var medicalService = await _context.MedicalServices
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == dto.MedicalServiceId.Value && s.IsActive, cancellationToken);

            if (medicalService is null)
                return ServiceResult<InvoiceItem>.Fail("Invalid active medical service id.", ErrorType.Validation, "InvalidMedicalService");

            description = string.IsNullOrWhiteSpace(dto.Description)
                ? medicalService.Name
                : dto.Description.Trim();
            unitPrice = dto.UnitPrice ?? medicalService.Price;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(dto.Description))
                return ServiceResult<InvoiceItem>.Fail("Invoice item description is required when no medical service is selected.", ErrorType.Validation, "DescriptionRequired");

            if (!dto.UnitPrice.HasValue || dto.UnitPrice.Value <= 0)
                return ServiceResult<InvoiceItem>.Fail("Invoice item unit price is required when no medical service is selected.", ErrorType.Validation, "UnitPriceRequired");

            description = dto.Description.Trim();
            unitPrice = dto.UnitPrice.Value;
        }

        var item = new InvoiceItem
        {
            MedicalServiceId = dto.MedicalServiceId,
            Description = description,
            Quantity = dto.Quantity,
            UnitPrice = unitPrice,
            TotalPrice = unitPrice * dto.Quantity
        };

        return ServiceResult<InvoiceItem>.Ok(item);
    }

    internal static void RecalculateInvoice(Invoice invoice)
    {
        invoice.TotalAmount = invoice.Items.Sum(i => i.TotalPrice);
        invoice.PaidAmount = invoice.Payments.Sum(p => p.Amount);
        invoice.RemainingAmount = invoice.TotalAmount - invoice.PaidAmount;

        if (invoice.Status == InvoiceStatus.Cancelled)
            return;

        invoice.Status = invoice.TotalAmount <= 0
            ? InvoiceStatus.Draft
            : invoice.PaidAmount <= 0
                ? InvoiceStatus.Issued
                : invoice.PaidAmount < invoice.TotalAmount
                    ? InvoiceStatus.PartiallyPaid
                    : InvoiceStatus.Paid;
    }

    private IQueryable<InvoiceDto> ProjectInvoices(IQueryable<Invoice> query)
    {
        return query.Select(i => new InvoiceDto
        {
            Id = i.Id,
            PatientId = i.PatientId,
            PatientName = i.Patient != null ? i.Patient.FullName : string.Empty,
            AppointmentId = i.AppointmentId,
            VisitId = i.VisitId,
            TotalAmount = i.TotalAmount,
            PaidAmount = i.PaidAmount,
            RemainingAmount = i.RemainingAmount,
            Status = i.Status.ToString(),
            IssuedAtUtc = i.IssuedAtUtc,
            Items = i.Items
                .OrderBy(item => item.Id)
                .Select(item => new InvoiceItemDto
                {
                    Id = item.Id,
                    MedicalServiceId = item.MedicalServiceId,
                    Description = item.Description,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TotalPrice = item.TotalPrice
                })
                .ToList()
        });
    }
}
