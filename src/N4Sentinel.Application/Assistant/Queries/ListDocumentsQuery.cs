using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Assistant.Dtos;

namespace N4Sentinel.Application.Assistant.Queries;

public sealed record ListDocumentsQuery : IRequest<IReadOnlyList<DocumentDto>>;

public sealed class ListDocumentsQueryHandler(IDocumentRepository documents)
    : IRequestHandler<ListDocumentsQuery, IReadOnlyList<DocumentDto>>
{
    public async Task<IReadOnlyList<DocumentDto>> Handle(ListDocumentsQuery request, CancellationToken cancellationToken)
    {
        var list = await documents.ListAllAsync(cancellationToken);

        return list.OrderBy(d => d.DocumentKey).ThenByDescending(d => d.VersionNumber).Select(AssistantMapper.ToDto).ToList();
    }
}
