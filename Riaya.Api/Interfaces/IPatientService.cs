using Riaya.Api.Common;
using Riaya.Api.DTOs.Patient;

namespace Riaya.Api.Interfaces;

public interface IPatientService
{
    Task<PagedResponse<PatientDto>> GetAllAsync(PatientQueryParams queryParams, CancellationToken cancellationToken = default);
    Task<PatientDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<List<PatientDto>> SearchByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<PatientHistoryDto?> GetHistoryAsync(int id, CancellationToken cancellationToken = default);
    Task<PatientSummaryDto?> GetSummaryAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult<PatientDto>> CreateAsync(CreatePatientDto dto, CancellationToken cancellationToken = default);
    Task<ServiceResult<PatientDto>> UpdateAsync(int id, CreatePatientDto dto, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

