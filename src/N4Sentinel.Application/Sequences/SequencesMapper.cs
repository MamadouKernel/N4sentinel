using N4Sentinel.Application.Sequences.Dtos;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Services;

namespace N4Sentinel.Application.Sequences;

internal static class SequencesMapper
{
    public static SequenceTierDto ToDto(this SequenceTier tier) => new(
        tier.Id,
        tier.Position,
        tier.Kind,
        tier.ComponentKind,
        tier.Label,
        tier.Execution,
        tier.SuccessCriteria,
        tier.IsOptional,
        tier.SettleDelaySeconds,
        tier.SourceReference);

    public static SequenceTemplateDto ToDto(this SequenceTemplate template) => new(
        template.Id,
        template.TemplateKey,
        template.VersionNumber,
        template.WorkflowType,
        template.Name,
        template.Description,
        template.Status,
        template.EnvironmentId,
        template.CreatedAtUtc,
        template.UpdatedAtUtc,
        template.Tiers.Select(t => t.ToDto()).ToList());

    public static SequencePlanStepDto ToDto(this SequencePlanStep step) => new(
        step.Position,
        step.ComponentId,
        step.ComponentName,
        step.ComponentKind,
        step.Action,
        step.SuccessCriteria,
        step.WaitsForPreviousStep,
        step.SettleDelaySeconds,
        step.SourceReference);
}
