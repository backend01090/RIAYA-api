using Riaya.Api.Common;
using Riaya.Api.Constants;
using Riaya.Api.DTOs.ClinicRoom;
using Riaya.Api.Extensions;
using Riaya.Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Riaya.Api.Controllers;

[Route("api/[controller]")]
[Route("api/v1/[controller]")]
[ApiController]
[Authorize(Policy = AppPolicies.ClinicStaff)]
public class ClinicRoomsController : ControllerBase
{
    private readonly IClinicRoomService _clinicRoomService;

    public ClinicRoomsController(IClinicRoomService clinicRoomService)
    {
        _clinicRoomService = clinicRoomService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Ok(ApiResponse<object>.SuccessResponse(await _clinicRoomService.GetAllAsync()));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var room = await _clinicRoomService.GetByIdAsync(id);
        if (room is null)
            return NotFound(ApiResponse<string>.FailResponse("Clinic room not found."));

        return Ok(ApiResponse<object>.SuccessResponse(room));
    }

    [HttpPost]
    [Authorize(Policy = AppPolicies.AdminOnly)]
    public async Task<IActionResult> Create(UpsertClinicRoomDto dto)
    {
        var result = await _clinicRoomService.CreateAsync(dto);
        if (!result.Success)
            return this.ToErrorResponse(result);

        return Created($"/api/clinicrooms/{result.Data!.Id}", ApiResponse<object>.SuccessResponse(result.Data, result.Message));
    }

    [HttpPut("{id}")]
    [Authorize(Policy = AppPolicies.AdminOnly)]
    public async Task<IActionResult> Update(int id, UpsertClinicRoomDto dto)
    {
        var result = await _clinicRoomService.UpdateAsync(id, dto);
        if (!result.Success)
            return this.ToErrorResponse(result);

        return Ok(ApiResponse<object>.SuccessResponse(result.Data, result.Message));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = AppPolicies.AdminOnly)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _clinicRoomService.DeleteAsync(id);
        if (!result.Success)
            return this.ToErrorResponse(result);

        return Ok(ApiResponse<object>.SuccessResponse(null, result.Message));
    }
}
