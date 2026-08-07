using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Assistant.Dtos;

namespace N4Sentinel.Application.Assistant.Queries;

public sealed record ListDocumentVersionsQuery(string DocumentKey) : IRequest<IReadOnlyList<DocumentDto>>;

public sealed class ListDocumentVersionsQueryHandler(IDocumentRepository documents)
    : IRequestHandler<ListDocumentVersionsQuery, IReadOnlyList<DocumentDto>>
{
    public async Task<IReadOnlyList<DocumentDto>> Handle(ListDocumentVersionsQuery request, CancellationToken cancellationToken)
    {
        var list = await documents.ListByDocumentKeyAsync(request.DocumentKey, cancellationToken);

        return list.OrderByDescending(d => d.VersionNumber).Select(AssistantMapper.ToDto).ToList();
    }
}
