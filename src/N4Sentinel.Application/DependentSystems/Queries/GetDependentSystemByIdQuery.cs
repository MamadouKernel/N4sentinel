using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.DependentSystems.Dtos;

namespace N4Sentinel.Application.DependentSystems.Queries;

public sealed record GetDependentSystemByIdQuery(Guid Id) : IRequest<DependentSystemDto?>;

public sealed class GetDependentSystemByIdQueryHandler(IDependentSystemRepository dependentSystems)
    : IRequestHandler<GetDependentSystemByIdQuery, DependentSystemDto?>
{
    public async Task<DependentSystemDto?> Handle(GetDependentSystemByIdQuery request, CancellationToken cancellationToken)
    {
        var s = await dependentSystems.GetByIdAsync(request.Id, cancellationToken);
        if (s is null)
        {
            return null;
        }

        return new DependentSystemDto(s.Id, s.EnvironmentId, s.Name, s.Description, s.Governance, s.CreatedAtUtc, s.UpdatedAtUtc);
    }
}
