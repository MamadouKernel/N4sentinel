using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Diagnostics.Dtos;

namespace N4Sentinel.Application.Diagnostics.Queries;

public sealed record GetDiagnosticCaseByIdQuery(Guid DiagnosticCaseId) : IRequest<DiagnosticCaseDto?>;

public sealed class GetDiagnosticCaseByIdQueryHandler(IDiagnosticCaseRepository cases)
    : IRequestHandler<GetDiagnosticCaseByIdQuery, DiagnosticCaseDto?>
{
    public async Task<DiagnosticCaseDto?> Handle(GetDiagnosticCaseByIdQuery request, CancellationToken cancellationToken)
    {
        var diagnosticCase = await cases.GetByIdAsync(request.DiagnosticCaseId, cancellationToken);

        return diagnosticCase is null ? null : DiagnosticsMapper.ToDto(diagnosticCase);
    }
}
