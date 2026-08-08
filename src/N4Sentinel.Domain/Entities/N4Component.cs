using N4Sentinel.Domain.Exceptions;

namespace N4Sentinel.Domain.Entities;

/// <summary>
/// Un composant de l'écosystème N4 rattaché à un environnement (nœud applicatif, Center Node, Bridge,
/// XPS, ECN4, base de données, file de messages, dossier partagé...). Champs minimaux définis par FR-002.
/// </summary>
public class N4Component
{
    private readonly List<Guid> _dependsOnComponentIds = [];

    private N4Component()
    {
        Name = string.Empty;
        Role = string.Empty;
    }

    public N4Component(
        Guid environmentId,
        string name,
        string role,
        ComponentCriticality criticality,
        ComponentGovernance governance,
        string? hostName = null,
        string? ipAddress = null,
        string? dnsName = null,
        string? operatingSystem = null,
        string? serviceOrProcessName = null,
        string? healthCheckDescription = null,
        string? technicalOwner = null,
        string? functionalOwner = null,
        N4ComponentKind kind = N4ComponentKind.Unspecified)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainRuleException("Le nom logique du composant est obligatoire.");
        }

        if (string.IsNullOrWhiteSpace(role))
        {
            throw new DomainRuleException("Le rôle du composant est obligatoire.");
        }

        Id = Guid.NewGuid();
        EnvironmentId = environmentId;
        Name = name.Trim();
        Role = role.Trim();
        Kind = kind;
        Criticality = criticality;
        Governance = governance;
        HostName = hostName?.Trim();
        IpAddress = ipAddress?.Trim();
        DnsName = dnsName?.Trim();
        OperatingSystem = operatingSystem?.Trim();
        ServiceOrProcessName = serviceOrProcessName?.Trim();
        HealthCheckDescription = healthCheckDescription?.Trim();
        TechnicalOwner = technicalOwner?.Trim();
        FunctionalOwner = functionalOwner?.Trim();
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid EnvironmentId { get; private set; }

    public N4Environment? Environment { get; private set; }

    public string Name { get; private set; }

    public string Role { get; private set; }

    /// <summary>
    /// Nature technique du composant. <see cref="Role"/> reste un libellé libre à destination des humains ;
    /// <see cref="Kind"/> est ce sur quoi les règles automatiques s'appuient — notamment le séquencement
    /// d'arrêt/démarrage, qui doit savoir qu'un composant est un Cluster Node sans dépendre de son
    /// orthographe.
    /// </summary>
    public N4ComponentKind Kind { get; private set; }

    public string? HostName { get; private set; }

    public string? IpAddress { get; private set; }

    public string? DnsName { get; private set; }

    public string? OperatingSystem { get; private set; }

    public string? ServiceOrProcessName { get; private set; }

    public string? HealthCheckDescription { get; private set; }

    public ComponentCriticality Criticality { get; private set; }

    public ComponentGovernance Governance { get; private set; }

    public string? TechnicalOwner { get; private set; }

    public string? FunctionalOwner { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    /// <summary>Composants dont ce composant dépend (doivent être opérationnels avant lui dans un workflow).</summary>
    public IReadOnlyCollection<Guid> DependsOnComponentIds => _dependsOnComponentIds;

    public void UpdateDetails(
        string name,
        string role,
        string? hostName,
        string? ipAddress,
        string? dnsName,
        string? operatingSystem,
        string? serviceOrProcessName,
        string? healthCheckDescription,
        ComponentCriticality criticality,
        ComponentGovernance governance,
        string? technicalOwner,
        string? functionalOwner,
        N4ComponentKind kind = N4ComponentKind.Unspecified)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainRuleException("Le nom logique du composant est obligatoire.");
        }

        if (string.IsNullOrWhiteSpace(role))
        {
            throw new DomainRuleException("Le rôle du composant est obligatoire.");
        }

        Name = name.Trim();
        Role = role.Trim();
        Kind = kind;
        HostName = hostName?.Trim();
        IpAddress = ipAddress?.Trim();
        DnsName = dnsName?.Trim();
        OperatingSystem = operatingSystem?.Trim();
        ServiceOrProcessName = serviceOrProcessName?.Trim();
        HealthCheckDescription = healthCheckDescription?.Trim();
        Criticality = criticality;
        Governance = governance;
        TechnicalOwner = technicalOwner?.Trim();
        FunctionalOwner = functionalOwner?.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Déclare que ce composant dépend d'un autre. Le contrôle complet de cycles dans le graphe de
    /// dépendances (au-delà du cas trivial A&lt;-&gt;B) relève du moteur de séquencement des workflows
    /// (FR-003/FR-004, épopée ultérieure) ; ici on ne garde que les garde-fous immédiats.
    /// </summary>
    public void AddDependency(Guid dependsOnComponentId)
    {
        if (dependsOnComponentId == Id)
        {
            throw new DomainRuleException("Un composant ne peut pas dépendre de lui-même.");
        }

        if (_dependsOnComponentIds.Contains(dependsOnComponentId))
        {
            return;
        }

        _dependsOnComponentIds.Add(dependsOnComponentId);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void RemoveDependency(Guid dependsOnComponentId)
    {
        if (_dependsOnComponentIds.Remove(dependsOnComponentId))
        {
            UpdatedAtUtc = DateTime.UtcNow;
        }
    }

    /// <summary>Reconstruit la liste des dépendances (utilisé par la couche de persistance).</summary>
    public void ReplaceDependencies(IEnumerable<Guid> dependsOnComponentIds)
    {
        _dependsOnComponentIds.Clear();
        foreach (var id in dependsOnComponentIds.Distinct())
        {
            AddDependency(id);
        }
    }
}
