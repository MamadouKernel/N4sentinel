using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Diagnostics.Dtos;

namespace N4Sentinel.Application.Diagnostics.Queries;

public sealed record ListDiagnosticSignalsByEnvironmentQuery(Guid EnvironmentId) : IRequest<IReadOnlyList<DiagnosticSignalDto>>;

public sealed class ListDiagnosticSignalsByEnvironmentQueryHandler(IDiagnosticSignalRepository signals)
    : IRequestHandler<ListDiagnosticSignalsByEnvironmentQuery, IReadOnlyList<DiagnosticSignalDto>>
{
    public async Task<IReadOnlyList<DiagnosticSignalDto>> Handle(
        ListDiagnosticSignalsByEnvironmentQuery request, CancellationToken cancellationToken)
    {
        var list = await signals.ListByEnvironmentAsync(request.EnvironmentId, cancellationToken);

        return list.OrderByDescending(s => s.CollectedAtUtc).Select(DiagnosticsMapper.ToDto).ToList();
    }
}
