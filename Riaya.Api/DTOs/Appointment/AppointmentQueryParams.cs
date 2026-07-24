using System.ComponentModel.DataAnnotations;
using Riaya.Api.DTOs.Common;

namespace Riaya.Api.DTOs.Appointment;

public class AppointmentQueryParams : PaginationParams
{
    [Range(1, int.MaxValue)]
    public int? DoctorId { get; set; }

    [Range(1, int.MaxValue)]
    public int? PatientId { get; set; }

    [StringLength(30)]
    public string? Status { get; set; }

    public DateTime? Date { get; set; }
}
