using Riaya.Api.Data;
using Riaya.Api.Data.Seed;
using Riaya.Api.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Riaya.Tests.TestSupport;

internal sealed class ClinicWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly string? _userId;
    private readonly string[] _roles;

    public ClinicWebApplicationFactory(string? userId = null, params string[] roles)
    {
        _userId = userId;
        _roles = roles;
        _connection.Open();
    }

    public async Task<SeededClinic> SeedClinicAsync()
    {
        return await ExecuteDbContextAsync(ClinicTestFactory.SeedClinicAsync);
    }

    public async Task SeedRolesAsync()
    {
        using var scope = Services.CreateScope();
        await EnsureDatabaseCreatedAsync(scope.ServiceProvider);
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        await IdentitySeeder.SeedRolesAsync(roleManager);
    }

    public async Task<ApplicationUser> CreateUserAsync(
        string email,
        string password,
        string role,
        string fullName = "Integration Test User",
        string? userId = null)
    {
        using var scope = Services.CreateScope();
        await EnsureDatabaseCreatedAsync(scope.ServiceProvider);
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        await SeedRolesAsync();

        var user = new ApplicationUser
        {
            Id = userId ?? Guid.NewGuid().ToString(),
            Email = email,
            UserName = email,
            FullName = fullName,
            EmailConfirmed = true
        };

        var createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            var errors = string.Join(" | ", createResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create test user: {errors}");
        }

        var roleResult = await userManager.AddToRoleAsync(user, role);
        if (!roleResult.Succeeded)
        {
            var errors = string.Join(" | ", roleResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to assign test user role: {errors}");
        }

        return user;
    }

    public async Task ExecuteDbContextAsync(Func<AppDbContext, Task> action)
    {
        using var scope = Services.CreateScope();
        await EnsureDatabaseCreatedAsync(scope.ServiceProvider);
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await action(context);
    }

    public async Task<T> ExecuteDbContextAsync<T>(Func<AppDbContext, Task<T>> action)
    {
        using var scope = Services.CreateScope();
        await EnsureDatabaseCreatedAsync(scope.ServiceProvider);
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await action(context);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(_connection));

            services.RemoveAll<TestAuthUser>();
            services.AddSingleton(new TestAuthUser
            {
                UserId = _userId,
                Roles = _roles
            });

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                TestAuthHandler.SchemeName,
                _ => { });
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _connection.Dispose();
        }

        base.Dispose(disposing);
    }

    private static async Task EnsureDatabaseCreatedAsync(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.GetRequiredService<AppDbContext>();
        await context.Database.EnsureCreatedAsync();
    }
}

