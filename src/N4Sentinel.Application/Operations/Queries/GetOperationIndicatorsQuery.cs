using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Operations.Dtos;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Operations.Queries;

/// <summary>Indicateurs agrégés de pilotage (FR-094). <see cref="EnvironmentId"/> nul = tous environnements confondus.</summary>
public sealed record GetOperationIndicatorsQuery(Guid? EnvironmentId) : IRequest<OperationIndicatorsDto>;

/// <summary>
/// Aucun indicateur n'est jamais estimé ou inventé (principe "no-fake-automation" du projet) : chaque valeur
/// est directement calculée à partir des <see cref="OperationRun"/>/<see cref="OperationStepExecution"/>
/// réellement enregistrées. Un jeu de données vide produit des indicateurs nuls plutôt qu'une valeur par défaut
/// trompeuse.
/// </summary>
public sealed class GetOperationIndicatorsQueryHandler(IOperationRunRepository operationRuns)
    : IRequestHandler<GetOperationIndicatorsQuery, OperationIndicatorsDto>
{
    private const int TopN = 5;

    public async Task<OperationIndicatorsDto> Handle(GetOperationIndicatorsQuery request, CancellationToken cancellationToken)
    {
        var runs = request.EnvironmentId is Guid environmentId
            ? await operationRuns.ListByEnvironmentAsync(environmentId, cancellationToken)
            : await operationRuns.ListAllAsync(cancellationToken);

        var completed = runs.Where(r => r.Status == OperationRunStatus.Completed).ToList();
        var failed = runs.Where(r => r.Status == OperationRunStatus.Failed).ToList();
        var cancelled = runs.Where(r => r.Status == OperationRunStatus.Cancelled).ToList();
        var terminalCount = completed.Count + failed.Count + cancelled.Count;

        double? successRate = terminalCount == 0 ? null : 100.0 * completed.Count / terminalCount;

        var completedDurations = completed
            .Where(r => r.CompletedAtUtc is not null)
            .Select(r => (r.CompletedAtUtc!.Value - r.RequestedAtUtc).TotalSeconds)
            .ToList();
        double? averageDuration = completedDurations.Count == 0 ? null : completedDurations.Average();

        var slowestSteps = runs
            .SelectMany(r => r.StepExecutions)
            .Where(s => s.Status == OperationStepExecutionStatus.Succeeded && s.StartedAtUtc is not null && s.CompletedAtUtc is not null)
            .Select(s => new SlowestStepDto(
                s.Name, s.ComponentName, (s.CompletedAtUtc!.Value - s.StartedAtUtc!.Value).TotalSeconds, s.CompletedAtUtc!.Value))
            .OrderByDescending(s => s.DurationSeconds)
            .Take(TopN)
            .ToList();

        var recurringErrors = runs
            .SelectMany(r => r.StepExecutions)
            .Where(s => s.Status == OperationStepExecutionStatus.Failed && !string.IsNullOrWhiteSpace(s.ResultMessage))
            .GroupBy(s => (s.Name, Message: s.ResultMessage!))
            .Select(g => new RecurringErrorDto(g.Key.Name, g.Key.Message, g.Count()))
            .OrderByDescending(e => e.Occurrences)
            .Take(TopN)
            .ToList();

        return new OperationIndicatorsDto(
            runs.Count, completed.Count, failed.Count, cancelled.Count, successRate, averageDuration,
            slowestSteps, recurringErrors);
    }
}
