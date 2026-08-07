using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Common;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Components.Commands;

public sealed record CreateComponentCommand(
    Guid EnvironmentId,
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
    string ActorUserId) : IRequest<Guid>, IAuditableRequest
{
    string IAuditableRequest.Action => "Création de composant";
    string IAuditableRequest.Summary => $"Composant '{Name}' créé sur l'environnement '{EnvironmentId}'.";
}

public sealed class CreateComponentCommandValidator : AbstractValidator<CreateComponentCommand>
{
    public CreateComponentCommandValidator()
    {
        RuleFor(x => x.EnvironmentId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Role).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Criticality).IsInEnum();
        RuleFor(x => x.Governance).IsInEnum();
        RuleFor(x => x.HostName).MaximumLength(255);
        RuleFor(x => x.IpAddress).MaximumLength(45);
        RuleFor(x => x.DnsName).MaximumLength(255);
        RuleFor(x => x.ActorUserId).NotEmpty();
    }
}

public sealed class CreateComponentCommandHandler(
    IEnvironmentRepository environments,
    IComponentRepository components,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateComponentCommand, Guid>
{
    public async Task<Guid> Handle(CreateComponentCommand request, CancellationToken cancellationToken)
    {
        _ = await environments.GetByIdAsync(request.EnvironmentId, cancellationToken)
            ?? throw new KeyNotFoundException($"Environnement '{request.EnvironmentId}' introuvable.");

        var component = new N4Component(
            request.EnvironmentId, request.Name, request.Role, request.Criticality, request.Governance,
            request.HostName, request.IpAddress, request.DnsName, request.OperatingSystem,
            request.ServiceOrProcessName, request.HealthCheckDescription, request.TechnicalOwner,
            request.FunctionalOwner);

        component.ReplaceDependencies(request.DependsOnComponentIds);

        components.Add(component);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return component.Id;
    }
}
