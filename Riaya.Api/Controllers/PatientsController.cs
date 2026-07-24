using Riaya.Api.Common;
using Riaya.Api.Constants;
using Riaya.Api.DTOs.Patient;
using Riaya.Api.Extensions;
using Riaya.Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Riaya.Api.Controllers;

[Route("api/[controller]")]
[Route("api/v1/[controller]")]
[ApiController]
[Authorize(Policy = AppPolicies.ClinicStaff)]
public class PatientsController : ControllerBase
{
    private readonly IPatientService _patientService;

    public PatientsController(IPatientService patientService)
    {
        _patientService = patientService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PatientQueryParams queryParams)
    {
        var patients = await _patientService.GetAllAsync(queryParams);
        return Ok(ApiResponse<object>.SuccessResponse(patients));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var patient = await _patientService.GetByIdAsync(id);
        if (patient is null) return NotFound(ApiResponse<string>.FailResponse("Patient not found."));
        return Ok(ApiResponse<object>.SuccessResponse(patient));
    }

    [HttpGet("{id}/summary")]
    public async Task<IActionResult> GetSummary(int id)
    {
        var summary = await _patientService.GetSummaryAsync(id);
        if (summary is null) return NotFound(ApiResponse<string>.FailResponse("Patient not found."));
        return Ok(ApiResponse<object>.SuccessResponse(summary));
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchByName([FromQuery] string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(ApiResponse<string>.FailResponse("Patient name is required."));

        var patients = await _patientService.SearchByNameAsync(name);
        return Ok(ApiResponse<object>.SuccessResponse(patients));
    }

    [HttpGet("{id}/history")]
    public async Task<IActionResult> GetHistory(int id)
    {
        var history = await _patientService.GetHistoryAsync(id);
        if (history is null) return NotFound(ApiResponse<string>.FailResponse("Patient not found."));
        return Ok(ApiResponse<object>.SuccessResponse(history));
    }

    [HttpPost]
    [Authorize(Policy = AppPolicies.AdminOrReceptionist)]
    public async Task<IActionResult> Create(CreatePatientDto dto)
    {
        var result = await _patientService.CreateAsync(dto);
        if (!result.Success)
            return this.ToErrorResponse(result);

        return Created(
            $"/api/patients/{result.Data!.Id}",
            ApiResponse<object>.SuccessResponse(result.Data, result.Message));
    }

    [HttpPut("{id}")]
    [Authorize(Policy = AppPolicies.AdminOrReceptionist)]
    public async Task<IActionResult> Update(int id, CreatePatientDto dto)
    {
        var result = await _patientService.UpdateAsync(id, dto);

        if (!result.Success)
            return this.ToErrorResponse(result);

        return Ok(ApiResponse<object>.SuccessResponse(result.Data, result.Message));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = AppPolicies.AdminOnly)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _patientService.DeleteAsync(id);
        if (!result.Success) return this.ToErrorResponse(result);
        return Ok(ApiResponse<object>.SuccessResponse(null, result.Message));
    }
}

