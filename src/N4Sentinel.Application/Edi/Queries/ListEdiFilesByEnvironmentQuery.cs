using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Edi.Dtos;

namespace N4Sentinel.Application.Edi.Queries;

public sealed record ListEdiFilesByEnvironmentQuery(Guid EnvironmentId) : IRequest<IReadOnlyList<EdiFileDto>>;

public sealed class ListEdiFilesByEnvironmentQueryHandler(IEdiFileRepository ediFiles)
    : IRequestHandler<ListEdiFilesByEnvironmentQuery, IReadOnlyList<EdiFileDto>>
{
    public async Task<IReadOnlyList<EdiFileDto>> Handle(ListEdiFilesByEnvironmentQuery request, CancellationToken cancellationToken)
    {
        var list = await ediFiles.ListByEnvironmentAsync(request.EnvironmentId, cancellationToken);

        return list.OrderByDescending(f => f.ReceivedAtUtc).Select(EdiMapper.ToDto).ToList();
    }
}
