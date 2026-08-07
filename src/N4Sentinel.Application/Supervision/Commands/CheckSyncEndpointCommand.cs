using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;

namespace N4Sentinel.Application.Supervision.Commands;

/// <summary>Vérification explicite, jamais automatique — cf. décision Sprint 10 (pas de sondage périodique).</summary>
public sealed record CheckSyncEndpointCommand(Guid SyncEndpointId) : IRequest;

public sealed class CheckSyncEndpointCommandValidator : AbstractValidator<CheckSyncEndpointCommand>
{
    public CheckSyncEndpointCommandValidator()
    {
        RuleFor(x => x.SyncEndpointId).NotEmpty();
    }
}

public sealed class CheckSyncEndpointCommandHandler(
    ISyncEndpointRepository syncEndpoints,
    ISupervisionSignalProvider signalProvider,
    IUnitOfWork unitOfWork) : IRequestHandler<CheckSyncEndpointCommand>
{
    public async Task Handle(CheckSyncEndpointCommand request, CancellationToken cancellationToken)
    {
        var endpoint = await syncEndpoints.GetByIdAsync(request.SyncEndpointId, cancellationToken)
            ?? throw new KeyNotFoundException($"Point de synchronisation '{request.SyncEndpointId}' introuvable.");

        var signal = await signalProvider.CheckSyncEndpointAsync(endpoint, cancellationToken);

        endpoint.RecordSyncCheck(
            signal.QueueSize, signal.ConsumerCount, signal.LastNormalExchangeUtc, signal.AnomalyDescription);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
