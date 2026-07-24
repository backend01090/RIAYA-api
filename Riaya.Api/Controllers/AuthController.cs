using Riaya.Api.Common;
using Riaya.Api.Constants;
using Riaya.Api.Data.Seed;
using Riaya.Api.DTOs.Auth;
using Riaya.Api.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Riaya.Api.Controllers;

[Route("api/[controller]")]
[Route("api/v1/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private const int MaxTokenIssueAttempts = 3;
    private const string ConcurrencyFailureCode = "ConcurrencyFailure";
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> TokenIssueLocks = new();

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IConfiguration _configuration;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        RoleManager<IdentityRole> roleManager,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _configuration = configuration;
    }

    [HttpPost("register")]
    [Authorize(Policy = AppPolicies.AdminOnly)]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        var normalizedRole = IdentitySeeder.Roles
            .FirstOrDefault(r => r.Equals(dto.Role, StringComparison.OrdinalIgnoreCase));

        if (normalizedRole is null)
        {
            return BadRequest(ApiResponse<object>.FailResponse("Invalid role."));
        }

        if (!await _roleManager.RoleExistsAsync(normalizedRole))
        {
            return BadRequest(ApiResponse<object>.FailResponse("Role does not exist in the system."));
        }

        var existingUser = await _userManager.FindByEmailAsync(dto.Email);
        if (existingUser is not null)
        {
            return BadRequest(ApiResponse<object>.FailResponse("Email is already registered."));
        }

        var user = new ApplicationUser
        {
            FullName = dto.FullName,
            Email = dto.Email,
            UserName = dto.Email
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(" | ", result.Errors.Select(e => e.Description));
            return BadRequest(ApiResponse<object>.FailResponse(errors));
        }

        var roleResult = await _userManager.AddToRoleAsync(user, normalizedRole);
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);

            var errors = string.Join(" | ", roleResult.Errors.Select(e => e.Description));
            return BadRequest(ApiResponse<object>.FailResponse(errors));
        }

        return Ok(ApiResponse<object>.SuccessResponse(
            new { user.Email, user.FullName, Role = normalizedRole },
            "User registered successfully."));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);

        if (user is null)
        {
            return Unauthorized(ApiResponse<string>.FailResponse("Invalid email or password."));
        }

        var tokenLock = GetTokenIssueLock(user.Id);
        await tokenLock.WaitAsync();

        try
        {
            user = await _userManager.FindByIdAsync(user.Id);
            if (user is null)
            {
                return Unauthorized(ApiResponse<string>.FailResponse("Invalid email or password."));
            }

            Microsoft.AspNetCore.Identity.SignInResult signInResult;

            try
            {
                signInResult = await _signInManager.CheckPasswordSignInAsync(
                    user,
                    dto.Password,
                    lockoutOnFailure: true);
            }
            catch (DbUpdateException)
            {
                return Conflict(ApiResponse<string>.FailResponse("Could not sign in because the account changed during sign-in. Please try again."));
            }

            if (signInResult.IsLockedOut)
            {
                return Unauthorized(ApiResponse<string>.FailResponse("Account is temporarily locked. Please try again later."));
            }

            if (!signInResult.Succeeded)
            {
                return Unauthorized(ApiResponse<string>.FailResponse("Invalid email or password."));
            }

            var roles = await _userManager.GetRolesAsync(user);
            var tokenIssueResult = await TryIssueTokensAsync(user, roles);
            if (!tokenIssueResult.Success)
            {
                return TokenIssueFailure(tokenIssueResult.StatusCode, tokenIssueResult.Message);
            }

            return Ok(ApiResponse<AuthResponseDto>.SuccessResponse(tokenIssueResult.AuthResponse, "Login successful."));
        }
        finally
        {
            tokenLock.Release();
        }
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh(RefreshTokenRequestDto dto)
    {
        var tokenLock = GetTokenIssueLock(dto.UserId);
        await tokenLock.WaitAsync();

        try
        {
            var user = await _userManager.FindByIdAsync(dto.UserId);
            if (user is null || !IsRefreshTokenValid(user, dto.RefreshToken))
            {
                return Unauthorized(ApiResponse<string>.FailResponse("Invalid or expired refresh token."));
            }

            var roles = await _userManager.GetRolesAsync(user);
            var tokenIssueResult = await TryIssueTokensAsync(
                user,
                roles,
                reloadedUser => IsRefreshTokenValid(reloadedUser, dto.RefreshToken));

            if (!tokenIssueResult.Success)
            {
                return TokenIssueFailure(tokenIssueResult.StatusCode, tokenIssueResult.Message);
            }

            return Ok(ApiResponse<AuthResponseDto>.SuccessResponse(tokenIssueResult.AuthResponse, "Token refreshed successfully."));
        }
        finally
        {
            tokenLock.Release();
        }
    }

    [HttpPost("revoke")]
    [Authorize]
    public async Task<IActionResult> Revoke()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(ApiResponse<string>.FailResponse("Authenticated user id was not found."));
        }

        var tokenLock = GetTokenIssueLock(userId);
        await tokenLock.WaitAsync();

        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
            {
                return NotFound(ApiResponse<string>.FailResponse("User not found."));
            }

            user.RefreshTokenHash = null;
            user.RefreshTokenExpiresAtUtc = null;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return TokenIssueFailure(
                    IsConcurrencyFailure(result) ? StatusCodes.Status409Conflict : StatusCodes.Status503ServiceUnavailable,
                    "Failed to revoke refresh token. Please try again.");
            }

            return Ok(ApiResponse<object>.SuccessResponse(null, "Refresh token revoked successfully."));
        }
        finally
        {
            tokenLock.Release();
        }
    }

    private string GenerateJwtToken(ApplicationUser user, IList<string> roles)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Email, user.Email!)
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(
                Convert.ToDouble(_configuration["Jwt:DurationInMinutes"])),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<(bool Success, AuthResponseDto? AuthResponse, int StatusCode, string Message)> TryIssueTokensAsync(
        ApplicationUser user,
        IList<string> roles,
        Func<ApplicationUser, bool>? validateReloadedUser = null)
    {
        var userId = user.Id;

        for (var attempt = 1; attempt <= MaxTokenIssueAttempts; attempt++)
        {
            if (attempt > 1)
            {
                var reloadedUser = await _userManager.FindByIdAsync(userId);
                if (reloadedUser is null)
                {
                    return (false, null, StatusCodes.Status404NotFound, "User not found.");
                }

                if (validateReloadedUser is not null && !validateReloadedUser(reloadedUser))
                {
                    return (false, null, StatusCodes.Status401Unauthorized, "Invalid or expired refresh token.");
                }

                user = reloadedUser;
            }

            var refreshToken = GenerateRefreshToken();
            var refreshTokenExpiresAtUtc = DateTime.UtcNow.AddDays(GetRefreshTokenDurationInDays());

            user.RefreshTokenHash = HashRefreshToken(refreshToken);
            user.RefreshTokenExpiresAtUtc = refreshTokenExpiresAtUtc;

            IdentityResult updateResult;

            try
            {
                updateResult = await _userManager.UpdateAsync(user);
            }
            catch (DbUpdateConcurrencyException)
            {
                continue;
            }
            catch (DbUpdateException)
            {
                return (false, null, StatusCodes.Status409Conflict, "Could not issue tokens because the account changed during sign-in. Please try again.");
            }
            if (updateResult.Succeeded)
            {
                return (true, new AuthResponseDto
                {
                    UserId = user.Id,
                    Token = GenerateJwtToken(user, roles),
                    RefreshToken = refreshToken,
                    RefreshTokenExpiresAtUtc = refreshTokenExpiresAtUtc,
                    Email = user.Email!,
                    FullName = user.FullName,
                    Roles = roles
                }, StatusCodes.Status200OK, string.Empty);
            }

            if (!IsConcurrencyFailure(updateResult))
            {
                return (false, null, StatusCodes.Status503ServiceUnavailable, "Failed to issue tokens. Please try again.");
            }
        }

        return (false, null, StatusCodes.Status409Conflict, "Could not issue tokens because the account changed during sign-in. Please try again.");
    }

    private IActionResult TokenIssueFailure(int statusCode, string message)
    {
        var response = ApiResponse<string>.FailResponse(message);

        return statusCode switch
        {
            StatusCodes.Status401Unauthorized => Unauthorized(response),
            StatusCodes.Status404NotFound => NotFound(response),
            StatusCodes.Status409Conflict => Conflict(response),
            _ => StatusCode(statusCode, response)
        };
    }

    private static bool IsConcurrencyFailure(IdentityResult result)
    {
        return result.Errors.Any(error => error.Code == ConcurrencyFailureCode);
    }

    private static SemaphoreSlim GetTokenIssueLock(string userId)
    {
        return TokenIssueLocks.GetOrAdd(userId, _ => new SemaphoreSlim(1, 1));
    }

    private bool IsRefreshTokenValid(ApplicationUser user, string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(user.RefreshTokenHash) ||
            user.RefreshTokenExpiresAtUtc is null ||
            user.RefreshTokenExpiresAtUtc <= DateTime.UtcNow)
        {
            return false;
        }

        var refreshTokenHash = HashRefreshToken(refreshToken);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(user.RefreshTokenHash),
            Encoding.UTF8.GetBytes(refreshTokenHash));
    }

    private int GetRefreshTokenDurationInDays()
    {
        var value = _configuration["Jwt:RefreshTokenDurationInDays"];
        return int.TryParse(value, out var days) && days > 0 ? days : 7;
    }

    private static string GenerateRefreshToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }

    private static string HashRefreshToken(string refreshToken)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
    }
}

