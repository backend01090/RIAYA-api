using System.Net;
using System.Net.Http.Json;
using Riaya.Api.Constants;
using Riaya.Api.DTOs.Billing;
using Riaya.Api.Enums;
using Riaya.Tests.TestSupport;

namespace Riaya.Tests;

public class BillingTests
{
    [Fact]
    public async Task CreateInvoice_ReturnsIssuedInvoice()
    {
        await using var context = ClinicTestFactory.CreateContext();
        var seeded = await ClinicTestFactory.SeedClinicAsync(context);
        var medicalService = await CreateMedicalServiceAsync(context, "Consultation", 50m);
        var invoiceService = ClinicTestFactory.CreateInvoiceService(context);

        var result = await invoiceService.CreateAsync(new CreateInvoiceDto
        {
            PatientId = seeded.Patient.Id,
            Items = new List<CreateInvoiceItemDto>
            {
                new() { MedicalServiceId = medicalService.Id, Quantity = 1 }
            }
        });

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("Issued", result.Data.Status);
        Assert.Equal(50m, result.Data.TotalAmount);
        Assert.Equal(50m, result.Data.RemainingAmount);
    }

    [Fact]
    public async Task AddItem_UpdatesInvoiceTotal()
    {
        await using var context = ClinicTestFactory.CreateContext();
        var seeded = await ClinicTestFactory.SeedClinicAsync(context);
        var medicalService = await CreateMedicalServiceAsync(context, "Consultation", 50m);
        var invoiceService = ClinicTestFactory.CreateInvoiceService(context);
        var invoice = await invoiceService.CreateAsync(new CreateInvoiceDto
        {
            PatientId = seeded.Patient.Id,
            Items = new List<CreateInvoiceItemDto>
            {
                new() { MedicalServiceId = medicalService.Id }
            }
        });

        var result = await invoiceService.AddItemAsync(invoice.Data!.Id, new CreateInvoiceItemDto
        {
            Description = "Lab test",
            Quantity = 2,
            UnitPrice = 15m
        });

        Assert.True(result.Success);
        Assert.Equal(80m, result.Data!.TotalAmount);
        Assert.Equal(2, result.Data.Items.Count);
    }

    [Fact]
    public async Task PartialPayment_ChangesInvoiceStatusToPartiallyPaid()
    {
        await using var context = ClinicTestFactory.CreateContext("receptionist-user");
        var invoice = await CreateInvoiceAsync(context, totalPrice: 100m);
        var paymentService = ClinicTestFactory.CreatePaymentService(context, "receptionist-user");

        var result = await paymentService.CreateAsync(new CreatePaymentDto
        {
            InvoiceId = invoice.Id,
            Amount = 40m,
            Method = PaymentMethod.Cash
        });

        Assert.True(result.Success);
        var updated = await ClinicTestFactory.CreateInvoiceService(context).GetByIdAsync(invoice.Id);
        Assert.Equal("PartiallyPaid", updated!.Status);
        Assert.Equal(60m, updated.RemainingAmount);
    }

    [Fact]
    public async Task FullPayment_ChangesInvoiceStatusToPaid()
    {
        await using var context = ClinicTestFactory.CreateContext("receptionist-user");
        var invoice = await CreateInvoiceAsync(context, totalPrice: 100m);
        var paymentService = ClinicTestFactory.CreatePaymentService(context, "receptionist-user");

        var result = await paymentService.CreateAsync(new CreatePaymentDto
        {
            InvoiceId = invoice.Id,
            Amount = 100m,
            Method = PaymentMethod.Cash
        });

        Assert.True(result.Success);
        var updated = await ClinicTestFactory.CreateInvoiceService(context).GetByIdAsync(invoice.Id);
        Assert.Equal("Paid", updated!.Status);
        Assert.Equal(0m, updated.RemainingAmount);
    }

    [Fact]
    public async Task Overpayment_IsRejected()
    {
        await using var context = ClinicTestFactory.CreateContext("receptionist-user");
        var invoice = await CreateInvoiceAsync(context, totalPrice: 100m);
        var paymentService = ClinicTestFactory.CreatePaymentService(context, "receptionist-user");

        var result = await paymentService.CreateAsync(new CreatePaymentDto
        {
            InvoiceId = invoice.Id,
            Amount = 101m,
            Method = PaymentMethod.Cash
        });

        Assert.False(result.Success);
        Assert.Equal("Payment amount cannot exceed invoice remaining amount.", result.Message);
    }

    [Fact]
    public async Task CannotAddItemToPaidInvoice()
    {
        await using var context = ClinicTestFactory.CreateContext("receptionist-user");
        var invoice = await CreateInvoiceAsync(context, totalPrice: 100m);
        await ClinicTestFactory.CreatePaymentService(context, "receptionist-user").CreateAsync(new CreatePaymentDto
        {
            InvoiceId = invoice.Id,
            Amount = 100m,
            Method = PaymentMethod.Cash
        });

        var result = await ClinicTestFactory.CreateInvoiceService(context).AddItemAsync(invoice.Id, new CreateInvoiceItemDto
        {
            Description = "Late item",
            Quantity = 1,
            UnitPrice = 10m
        });

        Assert.False(result.Success);
        Assert.Equal("Invoice items cannot be changed after payment has started or the invoice is cancelled.", result.Message);
    }

    [Fact]
    public async Task CannotAddItemToPartiallyPaidInvoice()
    {
        await using var context = ClinicTestFactory.CreateContext("receptionist-user");
        var invoice = await CreateInvoiceAsync(context, totalPrice: 100m);
        await ClinicTestFactory.CreatePaymentService(context, "receptionist-user").CreateAsync(new CreatePaymentDto
        {
            InvoiceId = invoice.Id,
            Amount = 40m,
            Method = PaymentMethod.Cash
        });

        var result = await ClinicTestFactory.CreateInvoiceService(context).AddItemAsync(invoice.Id, new CreateInvoiceItemDto
        {
            Description = "Late item",
            Quantity = 1,
            UnitPrice = 10m
        });

        Assert.False(result.Success);
        Assert.Equal("Invoice items cannot be changed after payment has started or the invoice is cancelled.", result.Message);
    }

    [Fact]
    public async Task CannotCancelInvoiceWithPayments()
    {
        await using var context = ClinicTestFactory.CreateContext("admin-user");
        var invoice = await CreateInvoiceAsync(context, totalPrice: 100m);
        await ClinicTestFactory.CreatePaymentService(context, "admin-user").CreateAsync(new CreatePaymentDto
        {
            InvoiceId = invoice.Id,
            Amount = 20m,
            Method = PaymentMethod.Cash
        });

        var result = await ClinicTestFactory.CreateInvoiceService(context).CancelAsync(invoice.Id);

        Assert.False(result.Success);
        Assert.Equal("Invoice has payments and requires a refund or reversal workflow before cancellation.", result.Message);
    }

    [Fact]
    public async Task CanCancelInvoiceWithNoPayments()
    {
        await using var context = ClinicTestFactory.CreateContext("admin-user");
        var invoice = await CreateInvoiceAsync(context, totalPrice: 100m);

        var result = await ClinicTestFactory.CreateInvoiceService(context).CancelAsync(invoice.Id);

        Assert.True(result.Success);
        Assert.Equal("Cancelled", result.Data!.Status);
    }

    [Fact]
    public async Task CancelledInvoice_CannotReceivePayment()
    {
        await using var context = ClinicTestFactory.CreateContext("admin-user");
        var invoice = await CreateInvoiceAsync(context, totalPrice: 100m);
        await ClinicTestFactory.CreateInvoiceService(context).CancelAsync(invoice.Id);
        var paymentService = ClinicTestFactory.CreatePaymentService(context, "admin-user");

        var result = await paymentService.CreateAsync(new CreatePaymentDto
        {
            InvoiceId = invoice.Id,
            Amount = 10m,
            Method = PaymentMethod.Cash
        });

        Assert.False(result.Success);
        Assert.Equal("Cancelled invoice cannot receive payments.", result.Message);
    }

    [Fact]
    public async Task Doctor_CannotRecordPayment()
    {
        using var factory = new ClinicWebApplicationFactory(
            ClinicTestFactory.DoctorUserId,
            AppRoles.Doctor);
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/payments", new CreatePaymentDto
        {
            InvoiceId = 1,
            Amount = 10m,
            Method = PaymentMethod.Cash
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static async Task<MedicalServiceDto> CreateMedicalServiceAsync(
        Riaya.Api.Data.AppDbContext context,
        string name,
        decimal price)
    {
        var result = await ClinicTestFactory.CreateMedicalServiceService(context).CreateAsync(new UpsertMedicalServiceDto
        {
            Name = name,
            Price = price
        });

        return result.Data!;
    }

    private static async Task<InvoiceDto> CreateInvoiceAsync(Riaya.Api.Data.AppDbContext context, decimal totalPrice)
    {
        var seeded = await ClinicTestFactory.SeedClinicAsync(context);
        var medicalService = await CreateMedicalServiceAsync(context, $"Service {Guid.NewGuid():N}", totalPrice);
        var result = await ClinicTestFactory.CreateInvoiceService(context).CreateAsync(new CreateInvoiceDto
        {
            PatientId = seeded.Patient.Id,
            Items = new List<CreateInvoiceItemDto>
            {
                new() { MedicalServiceId = medicalService.Id }
            }
        });

        return result.Data!;
    }
}
