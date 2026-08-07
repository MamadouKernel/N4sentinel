using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Assistant.Dtos;

namespace N4Sentinel.Application.Assistant.Queries;

/// <summary>FR-081 : recherche plein texte par symptôme, composant, message d'erreur ou opération, sur les documents indexés (Actifs) uniquement.</summary>
public sealed record SearchDocumentsQuery(string FreeText) : IRequest<IReadOnlyList<DocumentSearchResultDto>>;

public sealed class SearchDocumentsQueryHandler(IDocumentRepository documents)
    : IRequestHandler<SearchDocumentsQuery, IReadOnlyList<DocumentSearchResultDto>>
{
    public async Task<IReadOnlyList<DocumentSearchResultDto>> Handle(
        SearchDocumentsQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.FreeText))
        {
            return [];
        }

        var indexed = await documents.ListIndexedAsync(cancellationToken);
        var results = new List<DocumentSearchResultDto>();

        foreach (var document in indexed)
        {
            var lines = document.Content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains(request.FreeText, StringComparison.OrdinalIgnoreCase) ||
                    document.Title.Contains(request.FreeText, StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(new DocumentSearchResultDto(
                        document.Id, document.DocumentKey, document.VersionNumber, document.Title,
                        document.Category, document.N4Version, i + 1, lines[i].Trim()));
                }
            }
        }

        return results;
    }
}
