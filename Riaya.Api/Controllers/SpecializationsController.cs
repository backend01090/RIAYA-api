using Riaya.Api.Common;
using Riaya.Api.Constants;
using Riaya.Api.DTOs.Specialization;
using Riaya.Api.Extensions;
using Riaya.Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Riaya.Api.Controllers;

[Route("api/[controller]")]
[Route("api/v1/[controller]")]
[ApiController]
[Authorize(Policy = AppPolicies.ClinicStaff)]
public class SpecializationsController : ControllerBase
{
    private readonly ISpecializationService _specializationService;

    public SpecializationsController(ISpecializationService specializationService)
    {
        _specializationService = specializationService;
    }

    [HttpGet]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.None, NoStore = false)]
    public async Task<IActionResult> GetAll()
    {
        var specializations = await _specializationService.GetAllAsync();
        return Ok(ApiResponse<object>.SuccessResponse(specializations));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var specialization = await _specializationService.GetByIdAsync(id);
        if (specialization is null)
            return NotFound(ApiResponse<string>.FailResponse("Specialization not found."));

        return Ok(ApiResponse<object>.SuccessResponse(specialization));
    }

    [HttpPost]
    [Authorize(Policy = AppPolicies.AdminOnly)]
    public async Task<IActionResult> Create(CreateSpecializationDto dto)
    {
        var result = await _specializationService.CreateAsync(dto);
        if (!result.Success)
            return this.ToErrorResponse(result);

        return Created(
            $"/api/specializations/{result.Data!.Id}",
            ApiResponse<object>.SuccessResponse(result.Data, result.Message));
    }

    [HttpPut("{id}")]
    [Authorize(Policy = AppPolicies.AdminOnly)]
    public async Task<IActionResult> Update(int id, CreateSpecializationDto dto)
    {
        var result = await _specializationService.UpdateAsync(id, dto);
        if (!result.Success)
            return this.ToErrorResponse(result);

        return Ok(ApiResponse<object>.SuccessResponse(result.Data, result.Message));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = AppPolicies.AdminOnly)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _specializationService.DeleteAsync(id);
        if (!result.Success)
            return this.ToErrorResponse(result);

        return Ok(ApiResponse<object>.SuccessResponse(null, result.Message));
    }
}
