using N4Sentinel.Domain.Exceptions;

namespace N4Sentinel.Domain.Entities;

/// <summary>
/// Un palier d'une <see cref="SequenceTemplate"/> : « à ce rang, on traite les composants de ce type ».
/// Un palier ne désigne jamais un composant précis — c'est ce qui rend la séquence indépendante du nombre
/// de nœuds réellement déployés. Un palier <see cref="N4ComponentKind.ClusterNode"/> produira autant
/// d'étapes qu'il existe de Cluster Nodes déclarés dans l'environnement, qu'il y en ait 2 ou 12.
/// </summary>
public class SequenceTier
{
    private SequenceTier()
    {
        Label = string.Empty;
    }

    internal SequenceTier(
        int position,
        N4ComponentKind componentKind,
        string label,
        SequenceTierExecution execution,
        string? successCriteria,
        bool isOptional,
        int? settleDelaySeconds,
        string? sourceReference,
        SequenceTierKind kind = SequenceTierKind.ComponentAction)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            throw new DomainRuleException("Le libellé du palier de séquence est obligatoire.");
        }

        if (settleDelaySeconds is < 0)
        {
            throw new DomainRuleException("Le délai de stabilisation ne peut pas être négatif.");
        }

        Id = Guid.NewGuid();
        Position = position;
        Kind = kind;
        ComponentKind = kind == SequenceTierKind.Checkpoint ? N4ComponentKind.Unspecified : componentKind;
        Label = label.Trim();
        Execution = execution;
        SuccessCriteria = successCriteria?.Trim();
        IsOptional = isOptional;
        SettleDelaySeconds = settleDelaySeconds;
        SourceReference = sourceReference?.Trim();
    }

    public Guid Id { get; private set; }

    public Guid SequenceTemplateId { get; private set; }

    /// <summary>Rang du palier dans la séquence, à partir de 1.</summary>
    public int Position { get; private set; }

    /// <summary>Action sur des composants, ou simple point de contrôle.</summary>
    public SequenceTierKind Kind { get; private set; }

    /// <summary>Type de composant visé. Toujours <c>Unspecified</c> pour un point de contrôle.</summary>
    public N4ComponentKind ComponentKind { get; private set; }

    public string Label { get; private set; }

    public SequenceTierExecution Execution { get; private set; }

    /// <summary>
    /// Critère observable de réussite, recopié tel quel dans l'étape générée. Pour un Cluster Node au
    /// démarrage, Navis impose de vérifier le statut ACTIVE dans la vue Cluster Services avant de passer
    /// au nœud suivant <c>[GUIDE p.457]</c>.
    /// </summary>
    public string? SuccessCriteria { get; private set; }

    /// <summary>
    /// Palier conditionnel : ignoré sans erreur si l'environnement ne déclare aucun composant de ce type
    /// (cas d'ECN4 ou de N4 Billing, soumis à licence <c>[GUIDE p.457]</c>). Un palier non optionnel dont
    /// aucun composant n'existe est signalé comme une lacune du référentiel.
    /// </summary>
    public bool IsOptional { get; private set; }

    /// <summary>Temps d'attente à respecter après le palier avant d'enchaîner, si la source en prescrit un.</summary>
    public int? SettleDelaySeconds { get; private set; }

    /// <summary>Référence documentaire justifiant ce rang (ex. « GUIDE p.455 »), affichée à l'administrateur.</summary>
    public string? SourceReference { get; private set; }

    internal void ChangePosition(int position) => Position = position;

    internal SequenceTier Duplicate() => new(
        Position, ComponentKind, Label, Execution, SuccessCriteria, IsOptional, SettleDelaySeconds, SourceReference,
        Kind);
}
