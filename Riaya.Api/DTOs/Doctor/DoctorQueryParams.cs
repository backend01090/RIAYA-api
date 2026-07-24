using System.ComponentModel.DataAnnotations;
using Riaya.Api.DTOs.Common;

namespace Riaya.Api.DTOs.Doctor;

public class DoctorQueryParams : PaginationParams
{
    [StringLength(100)]
    public string? Search { get; set; }

    [Range(1, int.MaxValue)]
    public int? SpecializationId { get; set; }
}
