using Riaya.Api.Constants;
using Riaya.Api.Controllers;
using Riaya.Api.DTOs.Appointment;
using Microsoft.AspNetCore.Authorization;

namespace Riaya.Tests;

public class AuthorizationMetadataTests
{
    [Fact]
    public void AppointmentsCreate_RequiresAdminOrReceptionistPolicy()
    {
        var method = typeof(AppointmentsController).GetMethod(
            nameof(AppointmentsController.Create),
            new[] { typeof(CreateAppointmentDto) });

        Assert.NotNull(method);

        var authorizeAttribute = method!
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
            .Cast<AuthorizeAttribute>()
            .Single();

        Assert.Equal(AppPolicies.AdminOrReceptionist, authorizeAttribute.Policy);
    }
}

