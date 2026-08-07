using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Abstractions;

public interface IDiagnosticRuleRepository
{
    Task<DiagnosticRule?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<DiagnosticRule>> ListByRuleKeyAsync(string ruleKey, CancellationToken cancellationToken);

    Task<IReadOnlyList<DiagnosticRule>> ListAllAsync(CancellationToken cancellationToken);

    void Add(DiagnosticRule rule);
}
