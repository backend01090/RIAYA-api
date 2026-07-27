using System.Threading.RateLimiting;
using System.Text.Json.Serialization;
using Riaya.Api.Common;
using Riaya.Api.Converters;
using Riaya.Api.Data;
using Riaya.Api.Data.Seed;
using Riaya.Api.Entities;
using Riaya.Api.Extensions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext();
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        // Clients post ISO-8601 without an offset, which binds as Kind=Unspecified.
        // Npgsql rejects that for the timestamptz columns, so any handler putting a
        // request date into a query or a save threw and surfaced as a 500.
        options.JsonSerializerOptions.Converters.Add(new UtcDateTimeConverter());
        options.JsonSerializerOptions.Converters.Add(new UtcNullableDateTimeConverter());
    });
builder.Services.AddApplicationValidationResponse();
builder.Services.AddApplicationSwagger();
builder.Services.AddHttpContextAccessor();
builder.Services.AddResponseCaching();
builder.Services.AddHealthChecks();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        var response = ApiResponse<object>.FailResponse("Too many requests. Please try again later.");
        await context.HttpContext.Response.WriteAsJsonAsync(response, cancellationToken);
    };

    options.AddFixedWindowLimiter("api", limiterOptions =>
    {
        limiterOptions.PermitLimit = 100;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueLimit = 0;
        limiterOptions.AutoReplenishment = true;
    });
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Configure<AdminSeedOptions>(
    builder.Configuration.GetSection(AdminSeedOptions.SectionName));

builder.Services.Configure<DemoSeedOptions>(
    builder.Configuration.GetSection(DemoSeedOptions.SectionName));

builder.Configuration.ValidateJwtConfiguration(builder.Environment);

if (builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(AppContext.BaseDirectory, "DataProtectionKeys")));
}

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.User.RequireUniqueEmail = true;

        options.Password.RequiredLength = 6;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;

        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddApplicationAuthorization();
builder.Services.AddApplicationServices();

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseGlobalExceptionMiddleware();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "RIAYA API v1");
    });
}

app.UseResponseCaching();

app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();

app.MapControllers().RequireRateLimiting("api");
app.MapHealthChecks("/health").AllowAnonymous();

app.MapGet("/", () => Results.Ok(ApiResponse<string>.SuccessResponse("Riaya.Api API is running.")))
    .AllowAnonymous()
    .WithName("Root");

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;

    var dbContext = services.GetRequiredService<AppDbContext>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var adminSeedOptions = services.GetRequiredService<IOptions<AdminSeedOptions>>();
    var demoSeedOptions = services.GetRequiredService<IOptions<DemoSeedOptions>>();

    await dbContext.Database.MigrateAsync();

    await IdentitySeeder.SeedRolesAsync(roleManager);

    if (adminSeedOptions.Value.Enabled)
    {
        await IdentitySeeder.SeedAdminAsync(userManager, roleManager, adminSeedOptions);
    }

    if (demoSeedOptions.Value.Enabled)
    {
        await DemoDataSeeder.SeedAsync(dbContext, demoSeedOptions);
    }
}

app.Run();

public partial class Program;

