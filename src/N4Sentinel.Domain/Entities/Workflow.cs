using N4Sentinel.Domain.Exceptions;

namespace N4Sentinel.Domain.Entities;

/// <summary>
/// Conteneur nommé et versionné pour un scénario opérationnel N4 (FR-003). Chaque modification substantielle
/// crée une nouvelle <see cref="WorkflowVersion"/> plutôt que de modifier une version déjà sortie de Brouillon
/// — une exécution passée reste ainsi rattachée à la version exacte utilisée à l'époque.
/// </summary>
public class Workflow
{
    private readonly List<WorkflowVersion> _versions = [];
    private readonly List<Guid> _targetComponentIds = [];

    private Workflow()
    {
        Name = string.Empty;
    }

    public Workflow(
        Guid environmentId, string name, WorkflowType type, WorkflowScope scope,
        IEnumerable<Guid> targetComponentIds)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainRuleException("Le nom du workflow est obligatoire.");
        }

        var targets = targetComponentIds.Distinct().ToList();
        if (scope != WorkflowScope.Full && targets.Count == 0)
        {
            throw new DomainRuleException(
                "Un workflow partiel ou unitaire doit désigner au moins un composant cible.");
        }

        Id = Guid.NewGuid();
        EnvironmentId = environmentId;
        Name = name.Trim();
        Type = type;
        Scope = scope;
        _targetComponentIds.AddRange(targets);
        CreatedAtUtc = DateTime.UtcNow;

        _versions.Add(new WorkflowVersion(Id, versionNumber: 1));
    }

    public Guid Id { get; private set; }

    public Guid EnvironmentId { get; private set; }

    public string Name { get; private set; }

    public WorkflowType Type { get; private set; }

    public WorkflowScope Scope { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public IReadOnlyCollection<Guid> TargetComponentIds => _targetComponentIds;

    public IReadOnlyCollection<WorkflowVersion> Versions => _versions;

    public WorkflowVersion LatestVersion => _versions.OrderByDescending(v => v.VersionNumber).First();

    public WorkflowVersion? ActiveVersion => _versions.FirstOrDefault(v => v.Status == WorkflowVersionStatus.Active);

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainRuleException("Le nom du workflow est obligatoire.");
        }

        Name = name.Trim();
    }

    /// <summary>
    /// Crée une nouvelle version Brouillon en clonant les étapes de la dernière version, pour permettre sa
    /// modification (FR-003 : "toute modification doit créer une nouvelle version"). Refuse s'il existe déjà
    /// une version Brouillon en cours (elle doit être modifiée directement, pas dupliquée).
    /// </summary>
    public WorkflowVersion CreateNewDraftVersion()
    {
        var latest = LatestVersion;
        if (latest.Status == WorkflowVersionStatus.Draft)
        {
            throw new DomainRuleException(
                "Une version Brouillon existe déjà pour ce workflow ; modifiez-la au lieu d'en créer une nouvelle.");
        }

        var draft = latest.CloneAsNewDraft(latest.VersionNumber + 1);
        _versions.Add(draft);
        return draft;
    }

    public void SubmitVersionForValidation(Guid versionId) => GetVersion(versionId).SubmitForValidation();

    public void ValidateVersion(Guid versionId) => GetVersion(versionId).Validate();

    /// <summary>Active la version indiquée et désactive automatiquement l'ancienne version Active du même workflow, s'il y en a une.</summary>
    public void ActivateVersion(Guid versionId)
    {
        var version = GetVersion(versionId);
        var previousActive = _versions.FirstOrDefault(v => v.Id != versionId && v.Status == WorkflowVersionStatus.Active);

        version.Activate();
        previousActive?.Disable();
    }

    public void DisableVersion(Guid versionId) => GetVersion(versionId).Disable();

    private WorkflowVersion GetVersion(Guid versionId) =>
        _versions.FirstOrDefault(v => v.Id == versionId)
        ?? throw new KeyNotFoundException($"Version '{versionId}' introuvable pour ce workflow.");
}
