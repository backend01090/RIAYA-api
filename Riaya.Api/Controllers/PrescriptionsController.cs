using Riaya.Api.Common;
using Riaya.Api.Constants;
using Riaya.Api.DTOs.Prescription;
using Riaya.Api.Extensions;
using Riaya.Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Riaya.Api.Controllers;

[Route("api/[controller]")]
[Route("api/v1/[controller]")]
[ApiController]
[Authorize(Policy = AppPolicies.ClinicStaff)]
public class PrescriptionsController : ControllerBase
{
    private readonly IPrescriptionService _prescriptionService;

    public PrescriptionsController(IPrescriptionService prescriptionService)
    {
        _prescriptionService = prescriptionService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PrescriptionQueryParams queryParams)
    {
        var prescriptions = await _prescriptionService.GetAllAsync(queryParams);
        return Ok(ApiResponse<object>.SuccessResponse(prescriptions));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var prescription = await _prescriptionService.GetByIdAsync(id);
        if (prescription is null) return NotFound(ApiResponse<string>.FailResponse("Prescription not found."));
        return Ok(ApiResponse<object>.SuccessResponse(prescription));
    }

    [HttpPost]
    [Authorize(Policy = AppPolicies.AdminOrDoctor)]
    public async Task<IActionResult> Create(CreatePrescriptionDto dto)
    {
        var result = await _prescriptionService.CreateAsync(dto);

        if (!result.Success)
            return this.ToErrorResponse(result);

        return Created(
            $"/api/prescriptions/{result.Data!.Id}",
            ApiResponse<object>.SuccessResponse(result.Data, "Prescription created successfully."));
    }

    [HttpPut("{id}")]
    [Authorize(Policy = AppPolicies.AdminOrDoctor)]
    public async Task<IActionResult> Update(int id, UpdatePrescriptionDto dto)
    {
        var result = await _prescriptionService.UpdateAsync(id, dto);

        if (!result.Success)
            return this.ToErrorResponse(result);

        return Ok(ApiResponse<object>.SuccessResponse(result.Data, "Prescription updated successfully."));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = AppPolicies.AdminOnly)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _prescriptionService.DeleteAsync(id);

        if (!result.Success)
            return this.ToErrorResponse(result);

        return Ok(ApiResponse<object>.SuccessResponse(null, result.Message));
    }
}

