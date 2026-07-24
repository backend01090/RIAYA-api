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
[Authorize(Policy = AppPolicies.AdminOrReceptionist)]
public class InvoicesController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;

    public InvoicesController(IInvoiceService invoiceService)
    {
        _invoiceService = invoiceService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Ok(ApiResponse<object>.SuccessResponse(await _invoiceService.GetAllAsync()));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var invoice = await _invoiceService.GetByIdAsync(id);
        if (invoice is null)
            return NotFound(ApiResponse<string>.FailResponse("Invoice not found."));

        return Ok(ApiResponse<object>.SuccessResponse(invoice));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateInvoiceDto dto)
    {
        var result = await _invoiceService.CreateAsync(dto);
        if (!result.Success)
            return this.ToErrorResponse(result);

        return Created($"/api/invoices/{result.Data!.Id}", ApiResponse<object>.SuccessResponse(result.Data, result.Message));
    }

    [HttpPost("{id}/items")]
    public async Task<IActionResult> AddItem(int id, CreateInvoiceItemDto dto)
    {
        var result = await _invoiceService.AddItemAsync(id, dto);
        if (!result.Success)
            return this.ToErrorResponse(result);

        return Ok(ApiResponse<object>.SuccessResponse(result.Data, result.Message));
    }

    [HttpPatch("{id}/cancel")]
    [Authorize(Policy = AppPolicies.AdminOnly)]
    public async Task<IActionResult> Cancel(int id)
    {
        var result = await _invoiceService.CancelAsync(id);
        if (!result.Success)
            return this.ToErrorResponse(result);

        return Ok(ApiResponse<object>.SuccessResponse(result.Data, result.Message));
    }
}
