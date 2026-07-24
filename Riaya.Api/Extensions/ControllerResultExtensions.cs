using Riaya.Api.Common;
using Microsoft.AspNetCore.Mvc;

namespace Riaya.Api.Extensions;

public static class ControllerResultExtensions
{
    public static IActionResult ToErrorResponse(this ControllerBase controller, ServiceResult result)
    {
        var response = ApiResponse<string>.FailResponse(result.Message);

        return result.ErrorType switch
        {
            ErrorType.NotFound => controller.NotFound(response),
            ErrorType.Conflict => controller.Conflict(response),
            ErrorType.Forbidden => controller.Forbid(),
            ErrorType.Unauthorized => controller.Unauthorized(response),
            ErrorType.BusinessRule => controller.BadRequest(response),
            ErrorType.Validation => controller.BadRequest(response),
            _ => controller.BadRequest(response)
        };
    }
}
