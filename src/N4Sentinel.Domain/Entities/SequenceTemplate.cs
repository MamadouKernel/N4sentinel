using N4Sentinel.Domain.Exceptions;

namespace N4Sentinel.Domain.Entities;

/// <summary>
/// Ordre d'arrêt ou de démarrage des composants N4, **stocké et versionné en base plutôt qu'écrit dans le
/// code**, pour rester modifiable par un administrateur sans recompilation.
///
/// Les valeurs livrées à l'installation reprennent les séquences officielles Navis — arrêt
/// <c>[GUIDE §1.10.7 p.455]</c>, démarrage <c>[GUIDE §1.10.9 p.457-458]</c>, confirmées par
/// <c>[ITADMIN sl.112-119 et sl.178-179]</c> — mais ce ne sont que des valeurs initiales, pas une contrainte
/// figée.
///
/// Point déterminant : il existe **deux séquences indépendantes**, jamais l'une déduite de l'autre. Le
/// démarrage n'est pas l'inverse de l'arrêt — « You must start the N4 Cluster nodes before you start the
/// Center node, the Standby Center node, the Bridge daemon, and XPS » <c>[GUIDE p.458]</c>. L'invariant
/// commun aux deux séquences est que le Center Node suit immédiatement les Cluster Nodes ; mais cela le place
/// en 2ᵉ position au démarrage et en dernière position à l'arrêt, tandis que XPS ferme la marche au démarrage.
/// Un moteur qui inverserait une séquence pour obtenir l'autre produirait un ordre faux.
///
/// Versionnement : nouvelle ligne partageant le même <see cref="TemplateKey"/> avec un
/// <see cref="VersionNumber"/> incrémenté, comme <see cref="DiagnosticRule"/> et <see cref="Document"/>. Les
/// paliers enfants sont recopiés dans la nouvelle version, qui repart à l'état Brouillon.
/// </summary>
public class SequenceTemplate
{
    private readonly List<SequenceTier> _tiers = [];

    private SequenceTemplate()
    {
        TemplateKey = string.Empty;
        Name = string.Empty;
    }

    public SequenceTemplate(string templateKey, WorkflowType workflowType, string name, string? description)
        : this(templateKey, 1, workflowType, name, description)
    {
    }

    private SequenceTemplate(
        string templateKey, int versionNumber, WorkflowType workflowType, string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(templateKey))
        {
            throw new DomainRuleException("L'identifiant de la séquence est obligatoire.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainRuleException("Le nom de la séquence est obligatoire.");
        }

        if (workflowType is not (WorkflowType.Stop or WorkflowType.Start))
        {
            throw new DomainRuleException(
                "Une séquence ne peut décrire qu'un arrêt ou un démarrage. Un redémarrage est l'enchaînement " +
                "de la séquence d'arrêt puis de la séquence de démarrage, jamais un troisième ordre.");
        }

        Id = Guid.NewGuid();
        TemplateKey = templateKey.Trim();
        VersionNumber = versionNumber;
        WorkflowType = workflowType;
        Name = name.Trim();
        Description = description?.Trim();
        Status = SequenceTemplateStatus.Draft;
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }

    /// <summary>Identifiant stable partagé par toutes les versions successives.</summary>
    public string TemplateKey { get; private set; }

    public int VersionNumber { get; private set; }

    /// <summary>Restreint à <see cref="WorkflowType.Stop"/> ou <see cref="WorkflowType.Start"/>.</summary>
    public WorkflowType WorkflowType { get; private set; }

    public string Name { get; private set; }

    public string? Description { get; private set; }

    public SequenceTemplateStatus Status { get; private set; }

    /// <summary>
    /// Séquence propre à un environnement, ou <c>null</c> pour la séquence par défaut applicable à tous.
    /// Permet à une Production et à une UAT de topologies différentes d'avoir chacune leur ordre.
    /// </summary>
    public Guid? EnvironmentId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public IReadOnlyList<SequenceTier> Tiers => _tiers.OrderBy(t => t.Position).ToList();

    public SequenceTier AddTier(
        N4ComponentKind componentKind,
        string label,
        SequenceTierExecution execution,
        string? successCriteria,
        bool isOptional,
        int? settleDelaySeconds,
        string? sourceReference,
        SequenceTierKind kind = SequenceTierKind.ComponentAction)
    {
        EnsureDraft();

        // L'unicité ne vaut que pour les paliers d'action : un type de composant ne peut occuper qu'un seul
        // rang, c'est ce qui rend l'ordre non ambigu. Les points de contrôle, eux, peuvent être multiples
        // (contrôles d'infrastructure en ouverture, recette en clôture).
        if (kind == SequenceTierKind.ComponentAction && _tiers.Any(
                t => t.Kind == SequenceTierKind.ComponentAction && t.ComponentKind == componentKind))
        {
            throw new DomainRuleException(
                $"Le type de composant « {componentKind} » figure déjà dans cette séquence. Un type ne peut " +
                "occuper qu'un seul rang : c'est ce qui garantit un ordre non ambigu.");
        }

        var tier = new SequenceTier(
            _tiers.Count + 1, componentKind, label, execution, successCriteria, isOptional, settleDelaySeconds,
            sourceReference, kind);

        _tiers.Add(tier);
        Touch();
        return tier;
    }

    /// <summary>Ajoute un point de contrôle sans composant cible (prérequis, confirmation, recette finale).</summary>
    public SequenceTier AddCheckpoint(string label, string? successCriteria, string? sourceReference) =>
        AddTier(
            N4ComponentKind.Unspecified, label, SequenceTierExecution.Sequential, successCriteria,
            isOptional: false, settleDelaySeconds: null, sourceReference, SequenceTierKind.Checkpoint);

    public void RemoveTier(Guid tierId)
    {
        EnsureDraft();

        var tier = _tiers.SingleOrDefault(t => t.Id == tierId)
            ?? throw new DomainRuleException("Palier introuvable dans cette séquence.");

        _tiers.Remove(tier);
        Renumber();
        Touch();
    }

    /// <summary>Déplace un palier d'un rang vers le haut ou vers le bas — l'outil de réordonnancement de l'UI.</summary>
    public void MoveTier(Guid tierId, bool up)
    {
        EnsureDraft();

        var ordered = _tiers.OrderBy(t => t.Position).ToList();
        var index = ordered.FindIndex(t => t.Id == tierId);

        if (index < 0)
        {
            throw new DomainRuleException("Palier introuvable dans cette séquence.");
        }

        var target = up ? index - 1 : index + 1;

        if (target < 0 || target >= ordered.Count)
        {
            return;
        }

        (ordered[index], ordered[target]) = (ordered[target], ordered[index]);

        for (var i = 0; i < ordered.Count; i++)
        {
            ordered[i].ChangePosition(i + 1);
        }

        Touch();
    }

    public void ScopeToEnvironment(Guid? environmentId)
    {
        EnsureDraft();
        EnvironmentId = environmentId;
        Touch();
    }

    public void Rename(string name, string? description)
    {
        EnsureDraft();

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainRuleException("Le nom de la séquence est obligatoire.");
        }

        Name = name.Trim();
        Description = description?.Trim();
        Touch();
    }

    public void SubmitForValidation()
    {
        EnsureDraft();

        if (_tiers.Count == 0)
        {
            throw new DomainRuleException("Une séquence sans aucun palier ne peut pas être soumise à validation.");
        }

        Status = SequenceTemplateStatus.PendingValidation;
        Touch();
    }

    public void Validate()
    {
        EnsureStatus(SequenceTemplateStatus.PendingValidation);
        Status = SequenceTemplateStatus.Validated;
        Touch();
    }

    public void Activate()
    {
        EnsureStatus(SequenceTemplateStatus.Validated);
        Status = SequenceTemplateStatus.Active;
        Touch();
    }

    public void Disable()
    {
        if (Status is SequenceTemplateStatus.Disabled)
        {
            throw new DomainRuleException("Cette séquence est déjà désactivée.");
        }

        Status = SequenceTemplateStatus.Disabled;
        Touch();
    }

    /// <summary>Nouvelle version Brouillon reprenant les paliers actuels, pour modifier un ordre déjà actif.</summary>
    public SequenceTemplate CreateNewVersion()
    {
        var next = new SequenceTemplate(TemplateKey, VersionNumber + 1, WorkflowType, Name, Description)
        {
            EnvironmentId = EnvironmentId,
        };

        foreach (var tier in Tiers)
        {
            next._tiers.Add(tier.Duplicate());
        }

        return next;
    }

    private void Renumber()
    {
        var ordered = _tiers.OrderBy(t => t.Position).ToList();

        for (var i = 0; i < ordered.Count; i++)
        {
            ordered[i].ChangePosition(i + 1);
        }
    }

    private void EnsureDraft()
    {
        if (Status != SequenceTemplateStatus.Draft)
        {
            throw new DomainRuleException(
                "Seule une séquence à l'état Brouillon peut être modifiée. Créez une nouvelle version pour " +
                "faire évoluer un ordre déjà validé ou actif.");
        }
    }

    private void EnsureStatus(SequenceTemplateStatus expected)
    {
        if (Status != expected)
        {
            throw new DomainRuleException($"Opération impossible : la séquence est à l'état « {Status} ».");
        }
    }

    private void Touch() => UpdatedAtUtc = DateTime.UtcNow;
}
