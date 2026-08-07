using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Diagnostics.Dtos;

namespace N4Sentinel.Application.Diagnostics.Queries;

public sealed record ListImportedLogFilesByEnvironmentQuery(Guid EnvironmentId) : IRequest<IReadOnlyList<ImportedLogFileDto>>;

public sealed class ListImportedLogFilesByEnvironmentQueryHandler(IImportedLogFileRepository logFiles)
    : IRequestHandler<ListImportedLogFilesByEnvironmentQuery, IReadOnlyList<ImportedLogFileDto>>
{
    public async Task<IReadOnlyList<ImportedLogFileDto>> Handle(
        ListImportedLogFilesByEnvironmentQuery request, CancellationToken cancellationToken)
    {
        var list = await logFiles.ListByEnvironmentAsync(request.EnvironmentId, cancellationToken);

        return list.OrderByDescending(f => f.ImportedAtUtc).Select(DiagnosticsMapper.ToDto).ToList();
    }
}
