using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Sops.Dtos;

namespace N4Sentinel.Application.Sops.Queries;

public sealed record GetSopExecutionByIdQuery(Guid SopExecutionId) : IRequest<SopExecutionDto?>;

public sealed class GetSopExecutionByIdQueryHandler(ISopExecutionRepository executions, ISopRepository sops)
    : IRequestHandler<GetSopExecutionByIdQuery, SopExecutionDto?>
{
    public async Task<SopExecutionDto?> Handle(GetSopExecutionByIdQuery request, CancellationToken cancellationToken)
    {
        var execution = await executions.GetByIdAsync(request.SopExecutionId, cancellationToken);
        if (execution is null)
        {
            return null;
        }

        var sop = await sops.GetByIdAsync(execution.SopId, cancellationToken)
            ?? throw new KeyNotFoundException($"SOP '{execution.SopId}' introuvable.");

        return SopMapper.ToDto(execution, sop);
    }
}
