namespace N4Sentinel.Application.Operations.Dtos;

public sealed record SlowestStepDto(string StepName, string? ComponentName, double DurationSeconds, DateTime CompletedAtUtc);

public sealed record RecurringErrorDto(string StepName, string Message, int Occurrences);

/// <summary>Indicateurs de pilotage agrégés (FR-094), calculés uniquement sur des exécutions réelles — jamais estimés.</summary>
public sealed record OperationIndicatorsDto(
    int TotalOperations,
    int CompletedCount,
    int FailedCount,
    int CancelledCount,
    double? SuccessRatePercent,
    double? AverageDurationSeconds,
    IReadOnlyList<SlowestStepDto> SlowestSteps,
    IReadOnlyList<RecurringErrorDto> RecurringErrors);
