using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;

namespace N4Sentinel.Application.Diagnostics.Commands;

/// <summary>Résumé (FR-073), signatures connues (FR-075) et verdict nuancé (FR-074).</summary>
public sealed record AnalyzeLogFileCommand(Guid LogFileId) : IRequest;

public sealed class AnalyzeLogFileCommandValidator : AbstractValidator<AnalyzeLogFileCommand>
{
    public AnalyzeLogFileCommandValidator() => RuleFor(x => x.LogFileId).NotEmpty();
}

public sealed class AnalyzeLogFileCommandHandler(IImportedLogFileRepository logFiles, IUnitOfWork unitOfWork)
    : IRequestHandler<AnalyzeLogFileCommand>
{
    public async Task Handle(AnalyzeLogFileCommand request, CancellationToken cancellationToken)
    {
        var file = await logFiles.GetByIdAsync(request.LogFileId, cancellationToken)
            ?? throw new KeyNotFoundException($"Journal '{request.LogFileId}' introuvable.");

        file.Analyze();

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
