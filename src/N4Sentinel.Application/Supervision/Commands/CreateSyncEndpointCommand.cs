using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Supervision.Commands;

public sealed record CreateSyncEndpointCommand(Guid EnvironmentId, string Name) : IRequest<Guid>;

public sealed class CreateSyncEndpointCommandValidator : AbstractValidator<CreateSyncEndpointCommand>
{
    public CreateSyncEndpointCommandValidator()
    {
        RuleFor(x => x.EnvironmentId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

public sealed class CreateSyncEndpointCommandHandler(
    IEnvironmentRepository environments,
    ISyncEndpointRepository syncEndpoints,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateSyncEndpointCommand, Guid>
{
    public async Task<Guid> Handle(CreateSyncEndpointCommand request, CancellationToken cancellationToken)
    {
        _ = await environments.GetByIdAsync(request.EnvironmentId, cancellationToken)
            ?? throw new KeyNotFoundException($"Environnement '{request.EnvironmentId}' introuvable.");

        var endpoint = new SyncEndpoint(request.EnvironmentId, request.Name);

        syncEndpoints.Add(endpoint);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return endpoint.Id;
    }
}
