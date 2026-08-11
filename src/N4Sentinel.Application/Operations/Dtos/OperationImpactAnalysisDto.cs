using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Operations.Dtos;

/// <summary>
/// Impact d'une action sur un composant ciblé (FR-041) : les autres composants du référentiel qui le
/// déclarent comme dépendance — donc susceptibles d'être affectés si l'action réussit.
/// </summary>
public sealed record ComponentImpactDto(
    Guid ComponentId, string ComponentName, ComponentCriticality Criticality, IReadOnlyList<string> DependentComponentNames);

public sealed record OperationImpactAnalysisDto(IReadOnlyList<ComponentImpactDto> Impacts)
{
    public bool HasDependents => Impacts.Any(i => i.DependentComponentNames.Count > 0);
}
