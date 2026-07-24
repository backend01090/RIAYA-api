using Riaya.Api.Constants;
using Riaya.Api.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Riaya.Api.Data.Seed;

public static class IdentitySeeder
{
    public static readonly string[] Roles = AppRoles.All;

    public static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        foreach (var role in Roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    public static async Task SeedAdminAsync(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IOptions<AdminSeedOptions> adminSeedOptions)
    {
        await SeedRolesAsync(roleManager);

        var admin = adminSeedOptions.Value;

        var existingUser = await userManager.FindByEmailAsync(admin.Email);
        if (existingUser is not null)
        {
            var roles = await userManager.GetRolesAsync(existingUser);

            if (!roles.Contains(AppRoles.Admin))
            {
                await userManager.AddToRoleAsync(existingUser, AppRoles.Admin);
            }

            return;
        }

        var user = new ApplicationUser
        {
            FullName = admin.FullName,
            Email = admin.Email,
            UserName = admin.Email,
            EmailConfirmed = true
        };

        var createResult = await userManager.CreateAsync(user, admin.Password);

        if (!createResult.Succeeded)
        {
            var errors = string.Join(" | ", createResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to seed admin user: {errors}");
        }

        var roleResult = await userManager.AddToRoleAsync(user, AppRoles.Admin);

        if (!roleResult.Succeeded)
        {
            var errors = string.Join(" | ", roleResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to assign admin role: {errors}");
        }
    }
}
