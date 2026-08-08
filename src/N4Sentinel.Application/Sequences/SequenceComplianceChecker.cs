using N4Sentinel.Application.Abstractions;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Services;

namespace N4Sentinel.Application.Sequences;

/// <summary>
/// Confronte l'ordre des étapes d'une version de workflow à la séquence active de son environnement.
///
/// Mise en œuvre de FR-044 (« Empêcher XPS avant Bridge ou Center avant les Cluster Nodes, sauf workflow
/// exceptionnel approuvé et documenté »). Le contrôle vit dans la couche Application et non dans le Domaine
/// parce qu'il croise trois agrégats distincts — la version de workflow, la séquence de référence et le
/// référentiel des composants — qu'une entité ne peut pas atteindre seule.
/// </summary>
public interface ISequenceComplianceChecker
{
    Task<IReadOnlyList<string>> FindViolationsAsync(
        Workflow workflow, WorkflowVersion version, CancellationToken cancellationToken);
}

public sealed class SequenceComplianceChecker(
    ISequenceTemplateRepository templates,
    IComponentRepository components) : ISequenceComplianceChecker
{
    public async Task<IReadOnlyList<string>> FindViolationsAsync(
        Workflow workflow, WorkflowVersion version, CancellationToken cancellationToken)
    {
        // Seuls l'arrêt et le démarrage sont ordonnés par une séquence. Un workflow de diagnostic ou de
        // contrôle d'état n'a pas d'ordre de référence à respecter.
        if (workflow.Type is not (WorkflowType.Stop or WorkflowType.Start))
        {
            return [];
        }

        var template = await templates.GetActiveForEnvironmentAsync(
            workflow.EnvironmentId, workflow.Type, cancellationToken);

        if (template is null)
        {
            // Sans séquence de référence, il n'y a pas d'ordre à opposer : on ne bloque pas sur une règle
            // qui n'existe pas.
            return [];
        }

        var environmentComponents = await components.ListByEnvironmentAsync(workflow.EnvironmentId, cancellationToken);
        var kindByComponentId = environmentComponents.ToDictionary(c => c.Id, c => c.Kind);

        // Les étapes sans composant (points de contrôle) ne portent aucun type et ne participent donc pas à
        // l'ordre vérifié.
        var authoredOrder = version.Steps
            .Where(s => s.ComponentId is not null && kindByComponentId.ContainsKey(s.ComponentId.Value))
            .Select(s => kindByComponentId[s.ComponentId!.Value])
            .Where(k => k != N4ComponentKind.Unspecified)
            .ToList();

        return SequencePlanner.FindOrderViolations(template, authoredOrder);
    }
}
