using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Environments.Dtos;

namespace N4Sentinel.Application.Environments.Queries;

public sealed record ListEnvironmentsQuery : IRequest<IReadOnlyList<EnvironmentDto>>;

public sealed class ListEnvironmentsQueryHandler(IEnvironmentRepository environments)
    : IRequestHandler<ListEnvironmentsQuery, IReadOnlyList<EnvironmentDto>>
{
    public async Task<IReadOnlyList<EnvironmentDto>> Handle(
        ListEnvironmentsQuery request, CancellationToken cancellationToken)
    {
        var all = await environments.ListAllAsync(cancellationToken);

        return all
            .OrderBy(e => e.Kind != Domain.Entities.EnvironmentKind.Production)
            .ThenBy(e => e.Name)
            .Select(e => new EnvironmentDto(
                e.Id, e.Name, e.Code, e.Kind, e.Status, e.Description,
                e.Components.Count, e.CreatedAtUtc, e.UpdatedAtUtc))
            .ToList();
    }
}
