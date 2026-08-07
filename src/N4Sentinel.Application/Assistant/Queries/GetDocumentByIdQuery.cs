using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Assistant.Dtos;

namespace N4Sentinel.Application.Assistant.Queries;

public sealed record GetDocumentByIdQuery(Guid DocumentId) : IRequest<DocumentDto?>;

public sealed class GetDocumentByIdQueryHandler(IDocumentRepository documents)
    : IRequestHandler<GetDocumentByIdQuery, DocumentDto?>
{
    public async Task<DocumentDto?> Handle(GetDocumentByIdQuery request, CancellationToken cancellationToken)
    {
        var document = await documents.GetByIdAsync(request.DocumentId, cancellationToken);

        return document is null ? null : AssistantMapper.ToDto(document);
    }
}
