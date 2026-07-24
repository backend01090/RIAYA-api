using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Riaya.Api.Constants;
using Riaya.Api.DTOs.Auth;
using Riaya.Tests.TestSupport;

namespace Riaya.Tests;

public class AuthIntegrationTests
{
    private const string StrongPassword = "Admin@12345";

    [Fact]
    public async Task Login_ReturnsTokenAndRoles_WhenCredentialsAreValid()
    {
        using var factory = new ClinicWebApplicationFactory();
        await factory.CreateUserAsync("doctor@example.com", StrongPassword, AppRoles.Doctor, "Dr. Test");
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginDto
        {
            Email = "doctor@example.com",
            Password = StrongPassword
        });

        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var data = json.RootElement.GetProperty("data");
        var token = data.GetProperty("token").GetString();
        var refreshToken = data.GetProperty("refreshToken").GetString();

        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.False(string.IsNullOrWhiteSpace(refreshToken));
        Assert.False(string.IsNullOrWhiteSpace(data.GetProperty("userId").GetString()));
        Assert.True(data.GetProperty("refreshTokenExpiresAtUtc").GetDateTime() > DateTime.UtcNow);
        Assert.Equal("doctor@example.com", data.GetProperty("email").GetString());
        Assert.Equal(AppRoles.Doctor, data.GetProperty("roles")[0].GetString());

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.Role && c.Value == AppRoles.Doctor);
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenPasswordIsInvalid()
    {
        using var factory = new ClinicWebApplicationFactory();
        await factory.CreateUserAsync("doctor@example.com", StrongPassword, AppRoles.Doctor);
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginDto
        {
            Email = "doctor@example.com",
            Password = "Wrong@12345"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_DoesNotReturnServerError_WhenSameUserLogsInConcurrently()
    {
        using var factory = new ClinicWebApplicationFactory();
        await factory.CreateUserAsync("doctor@example.com", StrongPassword, AppRoles.Doctor);
        using var client = factory.CreateClient();

        var loginTasks = Enumerable.Range(0, 6)
            .Select(_ => client.PostAsJsonAsync("/api/auth/login", new LoginDto
            {
                Email = "doctor@example.com",
                Password = StrongPassword
            }))
            .ToArray();

        var responses = await Task.WhenAll(loginTasks);
        var responseBodies = await Task.WhenAll(responses.Select(r => r.Content.ReadAsStringAsync()));

        for (var i = 0; i < responses.Length; i++)
        {
            Assert.NotEqual(HttpStatusCode.InternalServerError, responses[i].StatusCode);
            Assert.True(
                responses[i].IsSuccessStatusCode || responses[i].StatusCode == HttpStatusCode.Conflict,
                responseBodies[i]);
        }

        Assert.Contains(responses, response => response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task Refresh_ReturnsNewAccessAndRefreshTokens_WhenRefreshTokenIsValid()
    {
        using var factory = new ClinicWebApplicationFactory();
        await factory.CreateUserAsync("doctor@example.com", StrongPassword, AppRoles.Doctor, "Dr. Test");
        using var client = factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginDto
        {
            Email = "doctor@example.com",
            Password = StrongPassword
        });

        loginResponse.EnsureSuccessStatusCode();
        using var loginJson = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync());
        var loginData = loginJson.RootElement.GetProperty("data");
        var userId = loginData.GetProperty("userId").GetString()!;
        var refreshToken = loginData.GetProperty("refreshToken").GetString()!;

        var refreshResponse = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequestDto
        {
            UserId = userId,
            RefreshToken = refreshToken
        });

        refreshResponse.EnsureSuccessStatusCode();
        using var refreshJson = JsonDocument.Parse(await refreshResponse.Content.ReadAsStringAsync());
        var refreshData = refreshJson.RootElement.GetProperty("data");

        Assert.False(string.IsNullOrWhiteSpace(refreshData.GetProperty("token").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(refreshData.GetProperty("refreshToken").GetString()));
        Assert.NotEqual(refreshToken, refreshData.GetProperty("refreshToken").GetString());
        Assert.Equal(userId, refreshData.GetProperty("userId").GetString());
    }

    [Fact]
    public async Task Refresh_ReturnsUnauthorized_WhenRefreshTokenIsInvalid()
    {
        using var factory = new ClinicWebApplicationFactory();
        var user = await factory.CreateUserAsync("doctor@example.com", StrongPassword, AppRoles.Doctor);
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequestDto
        {
            UserId = user.Id,
            RefreshToken = "invalid-refresh-token"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Revoke_InvalidatesCurrentRefreshToken_WhenUserIsAuthenticated()
    {
        const string userId = "doctor-user";
        using var factory = new ClinicWebApplicationFactory(userId, AppRoles.Doctor);
        await factory.CreateUserAsync(
            "doctor@example.com",
            StrongPassword,
            AppRoles.Doctor,
            "Dr. Test",
            userId);
        using var client = factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginDto
        {
            Email = "doctor@example.com",
            Password = StrongPassword
        });

        loginResponse.EnsureSuccessStatusCode();
        using var loginJson = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync());
        var loginData = loginJson.RootElement.GetProperty("data");
        var refreshToken = loginData.GetProperty("refreshToken").GetString()!;

        var revokeResponse = await client.PostAsync("/api/auth/revoke", null);

        revokeResponse.EnsureSuccessStatusCode();

        var refreshResponse = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequestDto
        {
            UserId = userId,
            RefreshToken = refreshToken
        });

        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
    }

    [Fact]
    public async Task Register_ReturnsForbidden_WhenAuthenticatedUserIsNotAdmin()
    {
        using var factory = new ClinicWebApplicationFactory("doctor-user", AppRoles.Doctor);
        await factory.SeedRolesAsync();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterDto
        {
            FullName = "Reception User",
            Email = "reception@example.com",
            Password = StrongPassword,
            Role = AppRoles.Receptionist
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Register_CreatesUser_WhenAdminSendsValidRequest()
    {
        using var factory = new ClinicWebApplicationFactory("admin-user", AppRoles.Admin);
        await factory.SeedRolesAsync();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterDto
        {
            FullName = "Reception User",
            Email = "reception@example.com",
            Password = StrongPassword,
            Role = AppRoles.Receptionist
        });

        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var data = json.RootElement.GetProperty("data");

        Assert.True(json.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal("reception@example.com", data.GetProperty("email").GetString());
        Assert.Equal(AppRoles.Receptionist, data.GetProperty("role").GetString());
    }
}

