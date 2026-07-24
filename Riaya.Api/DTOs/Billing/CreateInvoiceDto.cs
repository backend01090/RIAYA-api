using System.ComponentModel.DataAnnotations;

namespace Riaya.Api.DTOs.Billing;

public class CreateInvoiceDto
{
    [Range(1, int.MaxValue)]
    public int PatientId { get; set; }

    [Range(1, int.MaxValue)]
    public int? AppointmentId { get; set; }

    [Range(1, int.MaxValue)]
    public int? VisitId { get; set; }

    public List<CreateInvoiceItemDto> Items { get; set; } = new();
}
