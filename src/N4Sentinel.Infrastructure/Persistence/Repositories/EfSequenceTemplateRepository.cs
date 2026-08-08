using Microsoft.EntityFrameworkCore;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Infrastructure.Persistence.Repositories;

public sealed class EfSequenceTemplateRepository(AppDbContext dbContext) : ISequenceTemplateRepository
{
    public Task<SequenceTemplate?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.SequenceTemplates
            .Include(t => t.Tiers)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<IReadOnlyList<SequenceTemplate>> ListByTemplateKeyAsync(
        string templateKey, CancellationToken cancellationToken) =>
        await dbContext.SequenceTemplates
            .Include(t => t.Tiers)
            .Where(t => t.TemplateKey == templateKey)
            .OrderByDescending(t => t.VersionNumber)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<SequenceTemplate>> ListAllAsync(CancellationToken cancellationToken) =>
        await dbContext.SequenceTemplates
            .Include(t => t.Tiers)
            .ToListAsync(cancellationToken);

    public async Task<SequenceTemplate?> GetActiveForEnvironmentAsync(
        Guid environmentId, WorkflowType workflowType, CancellationToken cancellationToken)
    {
        var candidates = await dbContext.SequenceTemplates
            .Include(t => t.Tiers)
            .Where(t => t.WorkflowType == workflowType
                        && t.Status == SequenceTemplateStatus.Active
                        && (t.EnvironmentId == environmentId || t.EnvironmentId == null))
            .ToListAsync(cancellationToken);

        // Une séquence propre à l'environnement l'emporte sur la séquence par défaut : c'est ce qui permet
        // à une Production et à une UAT de topologies différentes d'avoir chacune leur ordre.
        return candidates
            .OrderByDescending(t => t.EnvironmentId.HasValue)
            .ThenByDescending(t => t.VersionNumber)
            .FirstOrDefault();
    }

    public void Add(SequenceTemplate template) => dbContext.SequenceTemplates.Add(template);
}
