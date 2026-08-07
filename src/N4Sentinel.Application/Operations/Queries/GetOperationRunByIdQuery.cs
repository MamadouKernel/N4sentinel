using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Operations.Dtos;

namespace N4Sentinel.Application.Operations.Queries;

public sealed record GetOperationRunByIdQuery(Guid Id) : IRequest<OperationRunDto?>;

public sealed class GetOperationRunByIdQueryHandler(IOperationRunRepository operationRuns)
    : IRequestHandler<GetOperationRunByIdQuery, OperationRunDto?>
{
    public async Task<OperationRunDto?> Handle(GetOperationRunByIdQuery request, CancellationToken cancellationToken)
    {
        var run = await operationRuns.GetByIdAsync(request.Id, cancellationToken);
        return run is null ? null : OperationRunMapper.ToDto(run);
    }
}
