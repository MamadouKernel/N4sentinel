using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Supervision.Commands;

public sealed record CreateSharedFolderCommand(
    Guid EnvironmentId, string Name, SharedFolderCategory Category, string Path) : IRequest<Guid>;

public sealed class CreateSharedFolderCommandValidator : AbstractValidator<CreateSharedFolderCommand>
{
    public CreateSharedFolderCommandValidator()
    {
        RuleFor(x => x.EnvironmentId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Category).IsInEnum();
        RuleFor(x => x.Path).NotEmpty().MaximumLength(500);
    }
}

public sealed class CreateSharedFolderCommandHandler(
    IEnvironmentRepository environments,
    ISharedFolderRepository sharedFolders,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateSharedFolderCommand, Guid>
{
    public async Task<Guid> Handle(CreateSharedFolderCommand request, CancellationToken cancellationToken)
    {
        _ = await environments.GetByIdAsync(request.EnvironmentId, cancellationToken)
            ?? throw new KeyNotFoundException($"Environnement '{request.EnvironmentId}' introuvable.");

        var folder = new SharedFolder(request.EnvironmentId, request.Name, request.Category, request.Path);

        sharedFolders.Add(folder);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return folder.Id;
    }
}
