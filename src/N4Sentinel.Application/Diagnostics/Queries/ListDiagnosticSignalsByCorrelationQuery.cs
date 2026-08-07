using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Diagnostics.Dtos;

namespace N4Sentinel.Application.Diagnostics.Queries;

public sealed record ListDiagnosticSignalsByCorrelationQuery(string CorrelationReference) : IRequest<IReadOnlyList<DiagnosticSignalDto>>;

public sealed class ListDiagnosticSignalsByCorrelationQueryHandler(IDiagnosticSignalRepository signals)
    : IRequestHandler<ListDiagnosticSignalsByCorrelationQuery, IReadOnlyList<DiagnosticSignalDto>>
{
    public async Task<IReadOnlyList<DiagnosticSignalDto>> Handle(
        ListDiagnosticSignalsByCorrelationQuery request, CancellationToken cancellationToken)
    {
        var list = await signals.ListByCorrelationAsync(request.CorrelationReference, cancellationToken);

        return list.OrderByDescending(s => s.CollectedAtUtc).Select(DiagnosticsMapper.ToDto).ToList();
    }
}
