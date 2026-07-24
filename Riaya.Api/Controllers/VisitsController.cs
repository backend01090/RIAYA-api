using Riaya.Api.Common;
using Riaya.Api.Constants;
using Riaya.Api.DTOs.Visit;
using Riaya.Api.Extensions;
using Riaya.Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Riaya.Api.Controllers;

[Route("api/[controller]")]
[Route("api/v1/[controller]")]
[ApiController]
[Authorize(Policy = AppPolicies.ClinicStaff)]
public class VisitsController : ControllerBase
{
    private readonly IVisitService _visitService;

    public VisitsController(IVisitService visitService)
    {
        _visitService = visitService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] VisitQueryParams queryParams)
    {
        var visits = await _visitService.GetAllAsync(queryParams);
        return Ok(ApiResponse<object>.SuccessResponse(visits));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var visit = await _visitService.GetByIdAsync(id);
        if (visit is null) return NotFound(ApiResponse<string>.FailResponse("Visit not found."));
        return Ok(ApiResponse<object>.SuccessResponse(visit));
    }

    [HttpPost]
    [Authorize(Policy = AppPolicies.AdminOrDoctor)]
    public async Task<IActionResult> Create(CreateVisitDto dto)
    {
        var result = await _visitService.CreateAsync(dto);

        if (!result.Success)
            return this.ToErrorResponse(result);

        return Created(
            $"/api/visits/{result.Data!.Id}",
            ApiResponse<object>.SuccessResponse(result.Data, "Visit created successfully."));
    }

    [HttpPut("{id}")]
    [Authorize(Policy = AppPolicies.AdminOrDoctor)]
    public async Task<IActionResult> Update(int id, UpdateVisitDto dto)
    {
        var result = await _visitService.UpdateAsync(id, dto);

        if (!result.Success)
            return this.ToErrorResponse(result);

        return Ok(ApiResponse<object>.SuccessResponse(result.Data, "Visit updated successfully."));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = AppPolicies.AdminOnly)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _visitService.DeleteAsync(id);

        if (!result.Success)
            return this.ToErrorResponse(result);

        return Ok(ApiResponse<object>.SuccessResponse(null, result.Message));
    }
}

