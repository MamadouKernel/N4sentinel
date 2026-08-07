using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Sops.Dtos;

namespace N4Sentinel.Application.Sops.Queries;

public sealed record GetSopByIdQuery(Guid SopId) : IRequest<SopDto?>;

public sealed class GetSopByIdQueryHandler(ISopRepository sops) : IRequestHandler<GetSopByIdQuery, SopDto?>
{
    public async Task<SopDto?> Handle(GetSopByIdQuery request, CancellationToken cancellationToken)
    {
        var sop = await sops.GetByIdAsync(request.SopId, cancellationToken);
        return sop is null ? null : SopMapper.ToDto(sop);
    }
}
