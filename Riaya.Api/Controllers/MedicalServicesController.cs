using Riaya.Api.Common;
using Riaya.Api.Constants;
using Riaya.Api.DTOs.Billing;
using Riaya.Api.Extensions;
using Riaya.Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Riaya.Api.Controllers;

[Route("api/[controller]")]
[Route("api/v1/[controller]")]
[ApiController]
[Authorize(Policy = AppPolicies.ClinicStaff)]
public class MedicalServicesController : ControllerBase
{
    private readonly IMedicalServiceService _medicalServiceService;

    public MedicalServicesController(IMedicalServiceService medicalServiceService)
    {
        _medicalServiceService = medicalServiceService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Ok(ApiResponse<object>.SuccessResponse(await _medicalServiceService.GetAllAsync()));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var service = await _medicalServiceService.GetByIdAsync(id);
        if (service is null)
            return NotFound(ApiResponse<string>.FailResponse("Medical service not found."));

        return Ok(ApiResponse<object>.SuccessResponse(service));
    }

    [HttpPost]
    [Authorize(Policy = AppPolicies.AdminOrReceptionist)]
    public async Task<IActionResult> Create(UpsertMedicalServiceDto dto)
    {
        var result = await _medicalServiceService.CreateAsync(dto);
        if (!result.Success)
            return this.ToErrorResponse(result);

        return Created($"/api/medicalservices/{result.Data!.Id}", ApiResponse<object>.SuccessResponse(result.Data, result.Message));
    }

    [HttpPut("{id}")]
    [Authorize(Policy = AppPolicies.AdminOrReceptionist)]
    public async Task<IActionResult> Update(int id, UpsertMedicalServiceDto dto)
    {
        var result = await _medicalServiceService.UpdateAsync(id, dto);
        if (!result.Success)
            return this.ToErrorResponse(result);

        return Ok(ApiResponse<object>.SuccessResponse(result.Data, result.Message));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = AppPolicies.AdminOnly)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _medicalServiceService.DeleteAsync(id);
        if (!result.Success)
            return this.ToErrorResponse(result);

        return Ok(ApiResponse<object>.SuccessResponse(null, result.Message));
    }
}
