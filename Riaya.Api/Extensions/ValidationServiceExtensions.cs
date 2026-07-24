using Riaya.Api.Common;
using Microsoft.AspNetCore.Mvc;

namespace Riaya.Api.Extensions;

public static class ValidationServiceExtensions
{
    public static IServiceCollection AddApplicationValidationResponse(this IServiceCollection services)
    {
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var errors = context.ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .ToDictionary(
                        x => x.Key,
                        x => x.Value!.Errors
                            .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage)
                                ? "The provided value is invalid."
                                : e.ErrorMessage)
                            .ToArray()
                    );

                var response = ApiResponse<object>.FailResponse("Validation failed.");
                response.Data = errors;

                return new BadRequestObjectResult(response);
            };
        });

        return services;
    }
}
