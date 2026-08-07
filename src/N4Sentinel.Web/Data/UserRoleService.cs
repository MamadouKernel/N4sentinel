using Microsoft.AspNetCore.Identity;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Users.Dtos;

namespace N4Sentinel.Web.Data;

/// <summary>Implémentation de <see cref="IUserRoleService"/> adossée à ASP.NET Core Identity (E11.1/E11.3).</summary>
public sealed class UserRoleService(UserManager<ApplicationUser> userManager) : IUserRoleService
{
    public async Task<IReadOnlyList<UserRoleInfoDto>> ListUsersAsync(CancellationToken cancellationToken)
    {
        var users = userManager.Users.OrderBy(u => u.Email).ToList();
        var result = new List<UserRoleInfoDto>();

        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            var isLockedOut = await userManager.IsLockedOutAsync(user);
            result.Add(new UserRoleInfoDto(user.Id, user.Email, roles.ToList(), isLockedOut));
        }

        return result;
    }

    public async Task GrantRoleAsync(string userId, string role, CancellationToken cancellationToken)
    {
        var user = await FindUserAsync(userId);
        var result = await userManager.AddToRoleAsync(user, role);
        EnsureSucceeded(result);
    }

    public async Task RevokeRoleAsync(string userId, string role, CancellationToken cancellationToken)
    {
        var user = await FindUserAsync(userId);
        var result = await userManager.RemoveFromRoleAsync(user, role);
        EnsureSucceeded(result);
    }

    public async Task LockAsync(string userId, CancellationToken cancellationToken)
    {
        var user = await FindUserAsync(userId);
        await userManager.SetLockoutEnabledAsync(user, true);
        var result = await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
        EnsureSucceeded(result);
    }

    public async Task UnlockAsync(string userId, CancellationToken cancellationToken)
    {
        var user = await FindUserAsync(userId);
        var result = await userManager.SetLockoutEndDateAsync(user, null);
        EnsureSucceeded(result);
    }

    private async Task<ApplicationUser> FindUserAsync(string userId) =>
        await userManager.FindByIdAsync(userId)
            ?? throw new KeyNotFoundException($"Utilisateur '{userId}' introuvable.");

    private static void EnsureSucceeded(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }
}
