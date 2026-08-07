using Microsoft.EntityFrameworkCore;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Infrastructure.Persistence.Repositories;

public class EfDiagnosticRuleRepository(AppDbContext dbContext) : IDiagnosticRuleRepository
{
    public Task<DiagnosticRule?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.DiagnosticRules.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyList<DiagnosticRule>> ListByRuleKeyAsync(
        string ruleKey, CancellationToken cancellationToken) =>
        await dbContext.DiagnosticRules.Where(r => r.RuleKey == ruleKey).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<DiagnosticRule>> ListAllAsync(CancellationToken cancellationToken) =>
        await dbContext.DiagnosticRules.ToListAsync(cancellationToken);

    public void Add(DiagnosticRule rule) => dbContext.DiagnosticRules.Add(rule);
}
