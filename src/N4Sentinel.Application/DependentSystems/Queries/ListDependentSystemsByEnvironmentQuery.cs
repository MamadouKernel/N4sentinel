using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.DependentSystems.Dtos;

namespace N4Sentinel.Application.DependentSystems.Queries;

public sealed record ListDependentSystemsByEnvironmentQuery(Guid EnvironmentId)
    : IRequest<IReadOnlyList<DependentSystemDto>>;

public sealed class ListDependentSystemsByEnvironmentQueryHandler(IDependentSystemRepository dependentSystems)
    : IRequestHandler<ListDependentSystemsByEnvironmentQuery, IReadOnlyList<DependentSystemDto>>
{
    public async Task<IReadOnlyList<DependentSystemDto>> Handle(
        ListDependentSystemsByEnvironmentQuery request, CancellationToken cancellationToken)
    {
        var list = await dependentSystems.ListByEnvironmentAsync(request.EnvironmentId, cancellationToken);

        return list
            .OrderBy(s => s.Name)
            .Select(s => new DependentSystemDto(
                s.Id, s.EnvironmentId, s.Name, s.Description, s.Governance, s.CreatedAtUtc, s.UpdatedAtUtc))
            .ToList();
    }
}
