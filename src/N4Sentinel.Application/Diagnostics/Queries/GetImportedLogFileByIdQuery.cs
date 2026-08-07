using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Diagnostics.Dtos;

namespace N4Sentinel.Application.Diagnostics.Queries;

public sealed record GetImportedLogFileByIdQuery(Guid LogFileId) : IRequest<ImportedLogFileDetailDto?>;

public sealed class GetImportedLogFileByIdQueryHandler(IImportedLogFileRepository logFiles)
    : IRequestHandler<GetImportedLogFileByIdQuery, ImportedLogFileDetailDto?>
{
    public async Task<ImportedLogFileDetailDto?> Handle(
        GetImportedLogFileByIdQuery request, CancellationToken cancellationToken)
    {
        var file = await logFiles.GetByIdAsync(request.LogFileId, cancellationToken);
        return file is null ? null : DiagnosticsMapper.ToDetailDto(file);
    }
}
