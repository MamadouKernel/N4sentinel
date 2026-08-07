using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Diagnostics.Dtos;

namespace N4Sentinel.Application.Diagnostics.Queries;

public sealed record SearchLogFilesGlobalQuery(
    Guid EnvironmentId, string? FreeText, string? LevelFilter, string? CorrelationReference)
    : IRequest<IReadOnlyList<GlobalLogSearchResultDto>>;

public sealed class SearchLogFilesGlobalQueryHandler(IImportedLogFileRepository logFiles)
    : IRequestHandler<SearchLogFilesGlobalQuery, IReadOnlyList<GlobalLogSearchResultDto>>
{
    private const int ContextSize = 2;

    public async Task<IReadOnlyList<GlobalLogSearchResultDto>> Handle(
        SearchLogFilesGlobalQuery request, CancellationToken cancellationToken)
    {
        IReadOnlyList<Domain.Entities.ImportedLogFile> files;

        if (!string.IsNullOrWhiteSpace(request.CorrelationReference))
        {
            files = await logFiles.ListByCorrelationAsync(request.CorrelationReference, cancellationToken);
            files = files.Where(f => f.EnvironmentId == request.EnvironmentId).ToList();
        }
        else
        {
            files = await logFiles.ListByEnvironmentAsync(request.EnvironmentId, cancellationToken);
        }

        var results = new List<GlobalLogSearchResultDto>();

        foreach (var file in files)
        {
            var lines = file.Content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var matches = new List<(string Line, int LineNumber)>();

            for (var i = 0; i < lines.Length; i++)
            {
                var matchesFreeText = string.IsNullOrWhiteSpace(request.FreeText) ||
                    lines[i].Contains(request.FreeText, StringComparison.OrdinalIgnoreCase);
                var matchesLevel = string.IsNullOrWhiteSpace(request.LevelFilter) ||
                    lines[i].Contains(request.LevelFilter, StringComparison.OrdinalIgnoreCase);

                if (matchesFreeText && matchesLevel)
                {
                    matches.Add((lines[i], i + 1));
                }
            }

            if (matches.Count == 0) continue;

            var groupedMatches = matches
                .GroupBy(m => m.Line.Trim())
                .Select(group =>
                {
                    var firstIndex = group.Min(m => m.LineNumber) - 1;
                    var contextStart = Math.Max(0, firstIndex - ContextSize);
                    var contextBefore = lines.Skip(contextStart).Take(firstIndex - contextStart).ToList();
                    var contextAfter = lines.Skip(firstIndex + 1).Take(ContextSize).ToList();

                    return new LogLineMatchDto(
                        group.Key, group.Count(), group.Min(m => m.LineNumber), group.Max(m => m.LineNumber),
                        contextBefore, contextAfter);
                })
                .OrderBy(m => m.FirstLineNumber)
                .ToList();

            results.Add(new GlobalLogSearchResultDto(
                file.Id, file.FileName, file.Source, file.CorrelationReference, matches.Count, groupedMatches));
        }

        return results;
    }
}
