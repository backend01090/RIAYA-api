using Riaya.Api.Common;
using Riaya.Api.Constants;
using Riaya.Api.DTOs.Appointment;
using Riaya.Api.Extensions;
using Riaya.Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Riaya.Api.Controllers;

[Route("api/[controller]")]
[Route("api/v1/[controller]")]
[ApiController]
[Authorize(Policy = AppPolicies.ClinicStaff)]
public class AppointmentsController : ControllerBase
{
    private readonly IAppointmentService _appointmentService;

    public AppointmentsController(IAppointmentService appointmentService)
    {
        _appointmentService = appointmentService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] AppointmentQueryParams queryParams)
    {
        var appointments = await _appointmentService.GetAllAsync(queryParams);
        return Ok(ApiResponse<object>.SuccessResponse(appointments));
    }

    [HttpGet("upcoming")]
    public async Task<IActionResult> GetUpcoming([FromQuery] int days = 7)
    {
        var appointments = await _appointmentService.GetUpcomingAsync(days);
        return Ok(ApiResponse<object>.SuccessResponse(appointments));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var appointment = await _appointmentService.GetByIdAsync(id);
        if (appointment is null) return NotFound(ApiResponse<string>.FailResponse("Appointment not found."));
        return Ok(ApiResponse<object>.SuccessResponse(appointment));
    }

    [HttpPost]
    [Authorize(Policy = AppPolicies.AdminOrReceptionist)]
    public async Task<IActionResult> Create(CreateAppointmentDto dto)
    {
        var result = await _appointmentService.CreateAsync(dto);

        if (!result.Success)
            return this.ToErrorResponse(result);

        return Created(
            $"/api/appointments/{result.Data!.Id}",
            ApiResponse<object>.SuccessResponse(result.Data, "Appointment created successfully."));
    }

    [HttpPatch("{id}/confirm")]
    [Authorize(Policy = AppPolicies.AdminOrReceptionist)]
    public async Task<IActionResult> Confirm(int id)
    {
        var result = await _appointmentService.ConfirmAsync(id);

        if (!result.Success)
            return this.ToErrorResponse(result);

        return Ok(ApiResponse<object>.SuccessResponse(null, result.Message));
    }

    [HttpPatch("{id}/cancel")]
    [Authorize(Policy = AppPolicies.AdminOrReceptionist)]
    public async Task<IActionResult> Cancel(int id)
    {
        var result = await _appointmentService.CancelAsync(id);

        if (!result.Success)
            return this.ToErrorResponse(result);

        return Ok(ApiResponse<object>.SuccessResponse(null, result.Message));
    }

    [HttpPatch("{id}/check-in")]
    [Authorize(Policy = AppPolicies.AdminOrReceptionist)]
    public async Task<IActionResult> CheckIn(int id)
    {
        var result = await _appointmentService.CheckInAsync(id);

        if (!result.Success)
            return this.ToErrorResponse(result);

        return Ok(ApiResponse<object>.SuccessResponse(null, result.Message));
    }

    [HttpPatch("{id}/complete")]
    [Authorize(Policy = AppPolicies.AdminOrDoctor)]
    public async Task<IActionResult> Complete(int id)
    {
        var result = await _appointmentService.CompleteAsync(id);

        if (!result.Success)
            return this.ToErrorResponse(result);

        return Ok(ApiResponse<object>.SuccessResponse(null, result.Message));
    }

    [HttpPatch("{id}/no-show")]
    [Authorize(Policy = AppPolicies.AdminOrReceptionist)]
    public async Task<IActionResult> MarkNoShow(int id)
    {
        var result = await _appointmentService.MarkNoShowAsync(id);

        if (!result.Success)
            return this.ToErrorResponse(result);

        return Ok(ApiResponse<object>.SuccessResponse(null, result.Message));
    }
}

