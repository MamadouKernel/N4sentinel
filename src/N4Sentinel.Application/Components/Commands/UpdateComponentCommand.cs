using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Common;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Components.Commands;

public sealed record UpdateComponentCommand(
    Guid Id,
    string Name,
    string Role,
    string? HostName,
    string? IpAddress,
    string? DnsName,
    string? OperatingSystem,
    string? ServiceOrProcessName,
    string? HealthCheckDescription,
    ComponentCriticality Criticality,
    ComponentGovernance Governance,
    string? TechnicalOwner,
    string? FunctionalOwner,
    IReadOnlyCollection<Guid> DependsOnComponentIds,
    string ActorUserId) : IRequest, IAuditableRequest
{
    string IAuditableRequest.Action => "Modification de composant";
    string IAuditableRequest.Summary => $"Composant '{Id}' modifié.";
}

public sealed class UpdateComponentCommandValidator : AbstractValidator<UpdateComponentCommand>
{
    public UpdateComponentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Role).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Criticality).IsInEnum();
        RuleFor(x => x.Governance).IsInEnum();
        RuleForEach(x => x.DependsOnComponentIds).NotEqual(x => x.Id)
            .WithMessage("Un composant ne peut pas dépendre de lui-même.");
        RuleFor(x => x.ActorUserId).NotEmpty();
    }
}

public sealed class UpdateComponentCommandHandler(
    IComponentRepository components,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateComponentCommand>
{
    public async Task Handle(UpdateComponentCommand request, CancellationToken cancellationToken)
    {
        var component = await components.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Composant '{request.Id}' introuvable.");

        component.UpdateDetails(
            request.Name, request.Role, request.HostName, request.IpAddress, request.DnsName,
            request.OperatingSystem, request.ServiceOrProcessName, request.HealthCheckDescription,
            request.Criticality, request.Governance, request.TechnicalOwner, request.FunctionalOwner);

        component.ReplaceDependencies(request.DependsOnComponentIds);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
