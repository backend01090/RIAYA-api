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
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpGet("invoice/{invoiceId}")]
    public async Task<IActionResult> GetByInvoiceId(int invoiceId)
    {
        return Ok(ApiResponse<object>.SuccessResponse(await _paymentService.GetByInvoiceIdAsync(invoiceId)));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreatePaymentDto dto)
    {
        var result = await _paymentService.CreateAsync(dto);
        if (!result.Success)
            return this.ToErrorResponse(result);

        return Created($"/api/payments/{result.Data!.Id}", ApiResponse<object>.SuccessResponse(result.Data, result.Message));
    }
}
