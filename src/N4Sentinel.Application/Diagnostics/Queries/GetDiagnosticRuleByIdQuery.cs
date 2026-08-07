using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Diagnostics.Dtos;

namespace N4Sentinel.Application.Diagnostics.Queries;

public sealed record GetDiagnosticRuleByIdQuery(Guid RuleId) : IRequest<DiagnosticRuleDto?>;

public sealed class GetDiagnosticRuleByIdQueryHandler(IDiagnosticRuleRepository rules)
    : IRequestHandler<GetDiagnosticRuleByIdQuery, DiagnosticRuleDto?>
{
    public async Task<DiagnosticRuleDto?> Handle(GetDiagnosticRuleByIdQuery request, CancellationToken cancellationToken)
    {
        var rule = await rules.GetByIdAsync(request.RuleId, cancellationToken);

        return rule is null ? null : DiagnosticsMapper.ToDto(rule);
    }
}
