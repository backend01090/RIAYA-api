using Riaya.Api.Common;
using Riaya.Api.Constants;
using Riaya.Api.DTOs.DoctorClinicAssignment;
using Riaya.Api.Extensions;
using Riaya.Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Riaya.Api.Controllers;

[Route("api/[controller]")]
[Route("api/v1/[controller]")]
[ApiController]
[Authorize(Policy = AppPolicies.AdminOnly)]
public class DoctorClinicAssignmentsController : ControllerBase
{
    private readonly IDoctorClinicAssignmentService _assignmentService;

    public DoctorClinicAssignmentsController(IDoctorClinicAssignmentService assignmentService)
    {
        _assignmentService = assignmentService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Ok(ApiResponse<object>.SuccessResponse(await _assignmentService.GetAllAsync()));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var assignment = await _assignmentService.GetByIdAsync(id);
        if (assignment is null)
            return NotFound(ApiResponse<string>.FailResponse("Doctor clinic assignment not found."));

        return Ok(ApiResponse<object>.SuccessResponse(assignment));
    }

    [HttpPost]
    public async Task<IActionResult> Create(UpsertDoctorClinicAssignmentDto dto)
    {
        var result = await _assignmentService.CreateAsync(dto);
        if (!result.Success)
            return this.ToErrorResponse(result);

        return Created($"/api/doctorclinicassignments/{result.Data!.Id}", ApiResponse<object>.SuccessResponse(result.Data, result.Message));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, UpsertDoctorClinicAssignmentDto dto)
    {
        var result = await _assignmentService.UpdateAsync(id, dto);
        if (!result.Success)
            return this.ToErrorResponse(result);

        return Ok(ApiResponse<object>.SuccessResponse(result.Data, result.Message));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _assignmentService.DeleteAsync(id);
        if (!result.Success)
            return this.ToErrorResponse(result);

        return Ok(ApiResponse<object>.SuccessResponse(null, result.Message));
    }
}
