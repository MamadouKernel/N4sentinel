using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Diagnostics.Dtos;

namespace N4Sentinel.Application.Diagnostics.Queries;

public sealed record ListDiagnosticRulesQuery : IRequest<IReadOnlyList<DiagnosticRuleDto>>;

public sealed class ListDiagnosticRulesQueryHandler(IDiagnosticRuleRepository rules)
    : IRequestHandler<ListDiagnosticRulesQuery, IReadOnlyList<DiagnosticRuleDto>>
{
    public async Task<IReadOnlyList<DiagnosticRuleDto>> Handle(ListDiagnosticRulesQuery request, CancellationToken cancellationToken)
    {
        var list = await rules.ListAllAsync(cancellationToken);

        return list.OrderBy(r => r.RuleKey).ThenByDescending(r => r.VersionNumber).Select(DiagnosticsMapper.ToDto).ToList();
    }
}
