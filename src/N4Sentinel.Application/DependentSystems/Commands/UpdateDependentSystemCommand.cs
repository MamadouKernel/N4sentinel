using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.DependentSystems.Commands;

public sealed record UpdateDependentSystemCommand(
    Guid Id,
    string Name,
    string? Description,
    ComponentGovernance Governance) : IRequest;

public sealed class UpdateDependentSystemCommandValidator : AbstractValidator<UpdateDependentSystemCommand>
{
    public UpdateDependentSystemCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.Governance).IsInEnum();
    }
}

public sealed class UpdateDependentSystemCommandHandler(
    IDependentSystemRepository dependentSystems,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateDependentSystemCommand>
{
    public async Task Handle(UpdateDependentSystemCommand request, CancellationToken cancellationToken)
    {
        var system = await dependentSystems.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Système dépendant '{request.Id}' introuvable.");

        system.UpdateDetails(request.Name, request.Description, request.Governance);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
