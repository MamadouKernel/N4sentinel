using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Diagnostics.Dtos;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Diagnostics.Queries;

/// <summary>Règles Actives d'un domaine, proposées comme hypothèses candidates lors d'un diagnostic (FR-062/065).</summary>
public sealed record ListActiveDiagnosticRulesByDomainQuery(DiagnosticDomain Domain) : IRequest<IReadOnlyList<DiagnosticRuleDto>>;

public sealed class ListActiveDiagnosticRulesByDomainQueryHandler(IDiagnosticRuleRepository rules)
    : IRequestHandler<ListActiveDiagnosticRulesByDomainQuery, IReadOnlyList<DiagnosticRuleDto>>
{
    public async Task<IReadOnlyList<DiagnosticRuleDto>> Handle(
        ListActiveDiagnosticRulesByDomainQuery request, CancellationToken cancellationToken)
    {
        var all = await rules.ListAllAsync(cancellationToken);

        return all
            .Where(r => r.Domain == request.Domain && r.Status == DiagnosticRuleStatus.Active)
            .OrderBy(r => r.RuleKey)
            .Select(DiagnosticsMapper.ToDto)
            .ToList();
    }
}
