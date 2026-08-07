using Microsoft.EntityFrameworkCore;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Infrastructure.Persistence.Repositories;

public class EfUserEnvironmentRoleRepository(AppDbContext dbContext) : IUserEnvironmentRoleRepository
{
    public Task<UserEnvironmentRole?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.UserEnvironmentRoles.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyList<UserEnvironmentRole>> ListByUserAsync(string userId, CancellationToken cancellationToken) =>
        await dbContext.UserEnvironmentRoles.Where(r => r.UserId == userId).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<UserEnvironmentRole>> ListByEnvironmentAsync(
        Guid environmentId, CancellationToken cancellationToken) =>
        await dbContext.UserEnvironmentRoles.Where(r => r.EnvironmentId == environmentId).ToListAsync(cancellationToken);

    public Task<bool> HasAnyRoleForEnvironmentAsync(
        string userId, Guid environmentId, IReadOnlyCollection<string> roles, CancellationToken cancellationToken) =>
        dbContext.UserEnvironmentRoles.AnyAsync(
            r => r.UserId == userId && r.EnvironmentId == environmentId && roles.Contains(r.Role), cancellationToken);

    public void Add(UserEnvironmentRole role) => dbContext.UserEnvironmentRoles.Add(role);

    public void Remove(UserEnvironmentRole role) => dbContext.UserEnvironmentRoles.Remove(role);
}
