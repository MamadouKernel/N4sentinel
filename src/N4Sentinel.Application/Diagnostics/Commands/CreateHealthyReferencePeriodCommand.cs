using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Common;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Diagnostics.Commands;

/// <summary>FR-066 : enregistre une période validée comme saine sur un environnement, utilisable comme référence de comparaison.</summary>
public sealed record CreateHealthyReferencePeriodCommand(
    Guid EnvironmentId, string Label, DateTime PeriodStartUtc, DateTime PeriodEndUtc, string? Notes, string ValidatedByUserId)
    : IRequest<Guid>, IAuditableRequest
{
    string IAuditableRequest.ActorUserId => ValidatedByUserId;
    string IAuditableRequest.Action => "Création d'une période de référence saine";
    string IAuditableRequest.Summary => $"Période de référence '{Label}' validée pour l'environnement '{EnvironmentId}'.";
}

public sealed class CreateHealthyReferencePeriodCommandValidator : AbstractValidator<CreateHealthyReferencePeriodCommand>
{
    public CreateHealthyReferencePeriodCommandValidator()
    {
        RuleFor(x => x.EnvironmentId).NotEmpty();
        RuleFor(x => x.Label).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ValidatedByUserId).NotEmpty();
    }
}

public sealed class CreateHealthyReferencePeriodCommandHandler(
    IHealthyReferencePeriodRepository periods, IUnitOfWork unitOfWork) : IRequestHandler<CreateHealthyReferencePeriodCommand, Guid>
{
    public async Task<Guid> Handle(CreateHealthyReferencePeriodCommand request, CancellationToken cancellationToken)
    {
        var period = new HealthyReferencePeriod(
            request.EnvironmentId, request.Label, request.PeriodStartUtc, request.PeriodEndUtc, request.Notes,
            request.ValidatedByUserId);

        periods.Add(period);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return period.Id;
    }
}
