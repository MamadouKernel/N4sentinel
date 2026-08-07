using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.DependentSystems.Commands;

public sealed record CreateDependentSystemCommand(
    Guid EnvironmentId,
    string Name,
    string? Description,
    ComponentGovernance Governance) : IRequest<Guid>;

public sealed class CreateDependentSystemCommandValidator : AbstractValidator<CreateDependentSystemCommand>
{
    public CreateDependentSystemCommandValidator()
    {
        RuleFor(x => x.EnvironmentId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.Governance).IsInEnum();
    }
}

public sealed class CreateDependentSystemCommandHandler(
    IEnvironmentRepository environments,
    IDependentSystemRepository dependentSystems,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateDependentSystemCommand, Guid>
{
    public async Task<Guid> Handle(CreateDependentSystemCommand request, CancellationToken cancellationToken)
    {
        _ = await environments.GetByIdAsync(request.EnvironmentId, cancellationToken)
            ?? throw new KeyNotFoundException($"Environnement '{request.EnvironmentId}' introuvable.");

        var system = new DependentSystem(request.EnvironmentId, request.Name, request.Description, request.Governance);

        dependentSystems.Add(system);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return system.Id;
    }
}
