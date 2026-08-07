using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Diagnostics.Dtos;

namespace N4Sentinel.Application.Diagnostics.Queries;

/// <summary>Filtrage par texte libre et niveau (FR-077), avec contexte et regroupement des lignes identiques (FR-076).</summary>
public sealed record SearchLogFileLinesQuery(Guid LogFileId, string? FreeText, string? LevelFilter)
    : IRequest<IReadOnlyList<LogLineMatchDto>>;

public sealed class SearchLogFileLinesQueryHandler(IImportedLogFileRepository logFiles)
    : IRequestHandler<SearchLogFileLinesQuery, IReadOnlyList<LogLineMatchDto>>
{
    private const int ContextSize = 2;

    public async Task<IReadOnlyList<LogLineMatchDto>> Handle(
        SearchLogFileLinesQuery request, CancellationToken cancellationToken)
    {
        var file = await logFiles.GetByIdAsync(request.LogFileId, cancellationToken)
            ?? throw new KeyNotFoundException($"Journal '{request.LogFileId}' introuvable.");

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

        return matches
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
    }
}
