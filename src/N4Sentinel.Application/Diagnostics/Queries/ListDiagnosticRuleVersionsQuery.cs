using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Diagnostics.Dtos;

namespace N4Sentinel.Application.Diagnostics.Queries;

public sealed record ListDiagnosticRuleVersionsQuery(string RuleKey) : IRequest<IReadOnlyList<DiagnosticRuleDto>>;

public sealed class ListDiagnosticRuleVersionsQueryHandler(IDiagnosticRuleRepository rules)
    : IRequestHandler<ListDiagnosticRuleVersionsQuery, IReadOnlyList<DiagnosticRuleDto>>
{
    public async Task<IReadOnlyList<DiagnosticRuleDto>> Handle(ListDiagnosticRuleVersionsQuery request, CancellationToken cancellationToken)
    {
        var list = await rules.ListByRuleKeyAsync(request.RuleKey, cancellationToken);

        return list.OrderByDescending(r => r.VersionNumber).Select(DiagnosticsMapper.ToDto).ToList();
    }
}
