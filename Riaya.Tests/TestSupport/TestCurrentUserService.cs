using Riaya.Api.Interfaces;

namespace Riaya.Tests.TestSupport;

internal sealed class TestCurrentUserService : ICurrentUserService
{
    public string? UserId { get; init; }
}

