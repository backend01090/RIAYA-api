using System.ComponentModel.DataAnnotations;
using Riaya.Api.DTOs.Common;

namespace Riaya.Api.DTOs.Patient;

public class PatientQueryParams : PaginationParams
{
    [StringLength(100)]
    public string? Search { get; set; }

    [StringLength(20)]
    public string? Gender { get; set; }
}
