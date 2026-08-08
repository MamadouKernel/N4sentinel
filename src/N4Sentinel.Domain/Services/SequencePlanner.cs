using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Exceptions;

namespace N4Sentinel.Domain.Services;

/// <summary>
/// Une étape planifiée. Soit un palier appliqué à un composant réel, soit un point de contrôle — auquel cas
/// <see cref="ComponentId"/> est nul et <see cref="Action"/> vaut <see cref="WorkflowStepAction.HealthCheck"/>.
/// </summary>
public sealed record SequencePlanStep(
    int Position,
    Guid? ComponentId,
    string ComponentName,
    N4ComponentKind ComponentKind,
    WorkflowStepAction Action,
    string? SuccessCriteria,
    bool WaitsForPreviousStep,
    int? SettleDelaySeconds,
    string? SourceReference)
{
    public bool IsCheckpoint => ComponentId is null;
}

/// <summary>Résultat du dépliage : les étapes, et les lacunes constatées dans le référentiel.</summary>
public sealed record SequencePlan(
    IReadOnlyList<SequencePlanStep> Steps,
    IReadOnlyList<string> Warnings);

/// <summary>
/// Déplie une <see cref="SequenceTemplate"/> (un ordre exprimé par *type* de composant) sur les composants
/// réellement déclarés dans un environnement, produisant la liste ordonnée des étapes à exécuter.
///
/// C'est ici que se joue la paramétrabilité par le nombre de nœuds : un palier Cluster Node produit autant
/// d'étapes qu'il existe de Cluster Nodes déclarés. Ajouter un nœud au référentiel suffit à le voir
/// apparaître au bon rang dans toutes les séquences, sans toucher ni au code ni à la séquence.
/// </summary>
public static class SequencePlanner
{
    public static SequencePlan Plan(SequenceTemplate template, IReadOnlyCollection<N4Component> components)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(components);

        if (template.Status != SequenceTemplateStatus.Active)
        {
            throw new DomainRuleException(
                "Seule une séquence active peut être dépliée en plan d'exécution. " +
                $"Séquence « {template.Name} » à l'état « {template.Status} ».");
        }

        var action = template.WorkflowType switch
        {
            WorkflowType.Stop => WorkflowStepAction.Stop,
            WorkflowType.Start => WorkflowStepAction.Start,
            _ => throw new DomainRuleException(
                "Une séquence ne peut décrire qu'un arrêt ou un démarrage."),
        };

        var steps = new List<SequencePlanStep>();
        var warnings = new List<string>();
        var position = 0;
        var previousStepExists = false;

        foreach (var tier in template.Tiers)
        {
            // Un point de contrôle ne dépend d'aucun composant : il produit toujours exactement une étape,
            // et n'est donc jamais escamoté par un référentiel incomplet.
            if (tier.Kind == SequenceTierKind.Checkpoint)
            {
                position++;

                steps.Add(new SequencePlanStep(
                    position, null, tier.Label, N4ComponentKind.Unspecified, WorkflowStepAction.HealthCheck,
                    tier.SuccessCriteria, previousStepExists, tier.SettleDelaySeconds, tier.SourceReference));

                previousStepExists = true;
                continue;
            }

            // Tri par nom : rend le plan reproductible d'une génération à l'autre, ce qui permet de comparer
            // deux exécutions et d'auditer un écart.
            var tierComponents = components
                .Where(c => c.Kind == tier.ComponentKind)
                .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (tierComponents.Count == 0)
            {
                if (!tier.IsOptional)
                {
                    warnings.Add(
                        $"Palier {tier.Position} « {tier.Label} » : aucun composant de type {tier.ComponentKind} " +
                        "n'est déclaré dans cet environnement. Le référentiel est probablement incomplet.");
                }

                continue;
            }

            // Un composant non pilotable ne peut pas faire l'objet d'une action (FR-002) : il est signalé
            // puis écarté, plutôt que de produire une étape qui échouerait à l'exécution.
            var notControllable = tierComponents
                .Where(c => c.Governance != ComponentGovernance.Controllable)
                .ToList();

            foreach (var component in notControllable)
            {
                warnings.Add(
                    $"« {component.Name} » ({tier.ComponentKind}) n'est pas déclaré pilotable : il est exclu du " +
                    "plan. Aucune action ne peut être déclenchée sur un composant non pilotable.");
            }

            var actionable = tierComponents.Except(notControllable).ToList();

            for (var i = 0; i < actionable.Count; i++)
            {
                var component = actionable[i];
                position++;

                // Séquentiel : chaque étape attend la précédente — y compris à l'intérieur du palier. C'est
                // ce chaînage qui interdit de lancer deux Cluster Nodes de front.
                var waits = tier.Execution == SequenceTierExecution.Sequential
                    ? previousStepExists
                    : previousStepExists && i == 0;

                steps.Add(new SequencePlanStep(
                    position,
                    component.Id,
                    component.Name,
                    tier.ComponentKind,
                    action,
                    tier.SuccessCriteria,
                    waits,
                    // Le délai de stabilisation ne vaut que pour la dernière étape du palier.
                    i == actionable.Count - 1 ? tier.SettleDelaySeconds : null,
                    tier.SourceReference));

                previousStepExists = true;
            }
        }

        if (steps.Count == 0)
        {
            warnings.Add(
                "Aucune étape n'a pu être planifiée : aucun composant typé et pilotable ne correspond aux " +
                "paliers de cette séquence.");
        }

        return new SequencePlan(steps, warnings);
    }

    /// <summary>
    /// Confronte un enchaînement de types de composants (celui d'un workflow saisi à la main) à l'ordre
    /// défini par la séquence, et retourne les inversions constatées. Une liste vide vaut conformité.
    ///
    /// Les types absents de la séquence sont ignorés : ils ne sont pas ordonnés par elle, donc rien ne
    /// permet de les déclarer mal placés.
    /// </summary>
    public static IReadOnlyList<string> FindOrderViolations(
        SequenceTemplate template, IReadOnlyList<N4ComponentKind> authoredOrder)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(authoredOrder);

        var rankByKind = template.Tiers
            .Where(t => t.Kind == SequenceTierKind.ComponentAction)
            .ToDictionary(t => t.ComponentKind, t => t.Position);
        var violations = new List<string>();
        var ranked = authoredOrder
            .Select((kind, index) => (Kind: kind, Index: index))
            .Where(x => rankByKind.ContainsKey(x.Kind))
            .ToList();

        for (var i = 1; i < ranked.Count; i++)
        {
            var current = ranked[i];
            var previous = ranked[i - 1];

            if (rankByKind[current.Kind] < rankByKind[previous.Kind])
            {
                violations.Add(
                    $"Étape {current.Index + 1} : {current.Kind} est placé après {previous.Kind}, alors que la " +
                    $"séquence « {template.Name} » impose l'ordre inverse " +
                    $"(rang {rankByKind[current.Kind]} avant rang {rankByKind[previous.Kind]}).");
            }
        }

        return violations;
    }
}
