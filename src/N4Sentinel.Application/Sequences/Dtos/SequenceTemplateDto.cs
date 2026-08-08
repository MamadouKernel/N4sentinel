using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Sequences.Dtos;

public sealed record SequenceTierDto(
    Guid Id,
    int Position,
    SequenceTierKind Kind,
    N4ComponentKind ComponentKind,
    string Label,
    SequenceTierExecution Execution,
    string? SuccessCriteria,
    bool IsOptional,
    int? SettleDelaySeconds,
    string? SourceReference);

public sealed record SequenceTemplateDto(
    Guid Id,
    string TemplateKey,
    int VersionNumber,
    WorkflowType WorkflowType,
    string Name,
    string? Description,
    SequenceTemplateStatus Status,
    Guid? EnvironmentId,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    IReadOnlyList<SequenceTierDto> Tiers);

/// <summary>Une étape du plan prévisionnel, telle qu'elle sera générée dans le workflow.</summary>
public sealed record SequencePlanStepDto(
    int Position,
    Guid? ComponentId,
    string ComponentName,
    N4ComponentKind ComponentKind,
    WorkflowStepAction Action,
    string? SuccessCriteria,
    bool WaitsForPreviousStep,
    int? SettleDelaySeconds,
    string? SourceReference);

/// <summary>
/// Aperçu du dépliage d'une séquence sur un environnement : ce qui sera exécuté, et ce qui cloche dans le
/// référentiel. Permet de contrôler le plan avant de générer quoi que ce soit.
/// </summary>
public sealed record SequencePlanDto(
    Guid EnvironmentId,
    string EnvironmentName,
    Guid SequenceTemplateId,
    string SequenceName,
    WorkflowType WorkflowType,
    IReadOnlyList<SequencePlanStepDto> Steps,
    IReadOnlyList<string> Warnings);
