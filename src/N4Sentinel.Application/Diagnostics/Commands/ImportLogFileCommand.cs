using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Diagnostics.Commands;

/// <summary>Import d'un journal par contenu texte collé (FR-070) — les secrets sont masqués avant stockage (FR-078).</summary>
public sealed record ImportLogFileCommand(
    Guid EnvironmentId, string FileName, string? Source, string Content, int? RetentionDays, string? CorrelationReference = null) : IRequest<Guid>;

public sealed class ImportLogFileCommandValidator : AbstractValidator<ImportLogFileCommand>
{
    public ImportLogFileCommandValidator()
    {
        RuleFor(x => x.EnvironmentId).NotEmpty();
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Content).NotEmpty();
        RuleFor(x => x.RetentionDays).GreaterThan(0).When(x => x.RetentionDays.HasValue);
        RuleFor(x => x.CorrelationReference).MaximumLength(200);
    }
}

public sealed class ImportLogFileCommandHandler(
    IEnvironmentRepository environments, IImportedLogFileRepository logFiles, IUnitOfWork unitOfWork)
    : IRequestHandler<ImportLogFileCommand, Guid>
{
    public async Task<Guid> Handle(ImportLogFileCommand request, CancellationToken cancellationToken)
    {
        _ = await environments.GetByIdAsync(request.EnvironmentId, cancellationToken)
            ?? throw new KeyNotFoundException($"Environnement '{request.EnvironmentId}' introuvable.");

        var file = new ImportedLogFile(
            request.EnvironmentId, request.FileName, request.Source, request.Content, request.RetentionDays, request.CorrelationReference);

        logFiles.Add(file);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return file.Id;
    }
}
