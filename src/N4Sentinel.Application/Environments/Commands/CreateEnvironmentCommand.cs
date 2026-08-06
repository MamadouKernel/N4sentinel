using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Environments.Commands;

public sealed record CreateEnvironmentCommand(
    string Name,
    string Code,
    EnvironmentKind Kind,
    string? Description) : IRequest<Guid>;

public sealed class CreateEnvironmentCommandValidator : AbstractValidator<CreateEnvironmentCommand>
{
    public CreateEnvironmentCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Kind).IsInEnum();
        RuleFor(x => x.Description).MaximumLength(2000);
    }
}

public sealed class CreateEnvironmentCommandHandler(
    IEnvironmentRepository environments,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateEnvironmentCommand, Guid>
{
    public async Task<Guid> Handle(CreateEnvironmentCommand request, CancellationToken cancellationToken)
    {
        if (await environments.ExistsWithCodeAsync(request.Code, cancellationToken))
        {
            throw new ValidationException(
                $"Un environnement avec le code '{request.Code}' existe déjà.");
        }

        var environment = new N4Environment(request.Name, request.Code, request.Kind, request.Description);
        environments.Add(environment);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return environment.Id;
    }
}
