using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Diagnostics.Dtos;

namespace N4Sentinel.Application.Diagnostics.Queries;

public sealed record ListDiagnosticCasesByEnvironmentQuery(Guid EnvironmentId) : IRequest<IReadOnlyList<DiagnosticCaseDto>>;

public sealed class ListDiagnosticCasesByEnvironmentQueryHandler(IDiagnosticCaseRepository cases)
    : IRequestHandler<ListDiagnosticCasesByEnvironmentQuery, IReadOnlyList<DiagnosticCaseDto>>
{
    public async Task<IReadOnlyList<DiagnosticCaseDto>> Handle(
        ListDiagnosticCasesByEnvironmentQuery request, CancellationToken cancellationToken)
    {
        var list = await cases.ListByEnvironmentAsync(request.EnvironmentId, cancellationToken);

        return list.OrderByDescending(c => c.CreatedAtUtc).Select(DiagnosticsMapper.ToDto).ToList();
    }
}
