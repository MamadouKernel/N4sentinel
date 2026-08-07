using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using N4Sentinel.Application.Users;

namespace N4Sentinel.Web.Data;

/// <summary>
/// Crée les 4 rôles applicatifs et un compte Administrateur de démonstration.
/// N'est appelé qu'en environnement de développement (cf. Program.cs) : en Production, la création des
/// comptes et l'attribution des rôles doivent être une opération explicite et auditée, pas un seed automatique.
/// </summary>
public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var adminEmail = configuration["Seed:AdminEmail"] ?? "admin@n4sentinel.local";
        var adminPassword = configuration["Seed:AdminPassword"] ?? "N4Sentinel!2026Dev";

        var admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
            };

            var result = await userManager.CreateAsync(admin, adminPassword);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Échec de création du compte administrateur de démonstration : " +
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }

        if (!await userManager.IsInRoleAsync(admin, Roles.Administrateur))
        {
            await userManager.AddToRoleAsync(admin, Roles.Administrateur);
        }
    }
}
