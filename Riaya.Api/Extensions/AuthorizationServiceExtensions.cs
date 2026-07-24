using Riaya.Api.Constants;

namespace Riaya.Api.Extensions;

public static class AuthorizationServiceExtensions
{
    public static IServiceCollection AddApplicationAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(AppPolicies.AdminOnly, policy =>
                policy.RequireRole(AppRoles.Admin));

            options.AddPolicy(AppPolicies.AdminOrDoctor, policy =>
                policy.RequireRole(AppRoles.Admin, AppRoles.Doctor));

            options.AddPolicy(AppPolicies.AdminOrReceptionist, policy =>
                policy.RequireRole(AppRoles.Admin, AppRoles.Receptionist));

            options.AddPolicy(AppPolicies.ClinicStaff, policy =>
                policy.RequireRole(AppRoles.Admin, AppRoles.Doctor, AppRoles.Receptionist));
        });

        return services;
    }
}
