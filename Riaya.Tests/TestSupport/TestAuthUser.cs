namespace Riaya.Tests.TestSupport;

internal sealed class TestAuthUser
{
    public string? UserId { get; init; }
    public IReadOnlyCollection<string> Roles { get; init; } = Array.Empty<string>();
}

