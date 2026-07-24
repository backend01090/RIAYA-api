namespace Riaya.Api.Extensions;

public static class ConfigurationValidationExtensions
{
    private const string DefaultJwtKey = "REPLACE_IN_USER_SECRETS_OR_ENV__MIN_32_CHARS";
    private const int MinimumJwtKeyLength = 32;

    public static void ValidateJwtConfiguration(this IConfiguration configuration, IWebHostEnvironment environment)
    {
        var key = configuration["Jwt:Key"];
        var issuer = configuration["Jwt:Issuer"];
        var audience = configuration["Jwt:Audience"];
        var durationValue = configuration["Jwt:DurationInMinutes"];
        var refreshTokenDurationValue = configuration["Jwt:RefreshTokenDurationInDays"];

        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("Jwt:Key is required.");

        if (key.Length < MinimumJwtKeyLength)
            throw new InvalidOperationException($"Jwt:Key must be at least {MinimumJwtKeyLength} characters.");

        if (!environment.IsDevelopment() &&
            !environment.IsEnvironment("Testing") &&
            key == DefaultJwtKey)
        {
            throw new InvalidOperationException("Jwt:Key must be provided from secrets or environment variables outside Development.");
        }

        if (string.IsNullOrWhiteSpace(issuer))
            throw new InvalidOperationException("Jwt:Issuer is required.");

        if (string.IsNullOrWhiteSpace(audience))
            throw new InvalidOperationException("Jwt:Audience is required.");

        if (!double.TryParse(durationValue, out var durationInMinutes) || durationInMinutes <= 0)
            throw new InvalidOperationException("Jwt:DurationInMinutes must be a positive number.");

        if (!string.IsNullOrWhiteSpace(refreshTokenDurationValue) &&
            (!int.TryParse(refreshTokenDurationValue, out var refreshTokenDurationInDays) || refreshTokenDurationInDays <= 0))
        {
            throw new InvalidOperationException("Jwt:RefreshTokenDurationInDays must be a positive number.");
        }
    }
}

