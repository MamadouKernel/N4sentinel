using N4Sentinel.Domain.Common;

namespace N4Sentinel.Domain.Entities;

/// <summary>
/// §3.18 — Composant : rôle, serveur, service, endpoints, dépendances, contrôles.
/// Un composant est une cible pilotable (arrêt, démarrage, redémarrage) de l'écosystème N4.
/// </summary>
public class N4Component : Entity
{
    public Guid EnvironmentId { get; set; }

    public required string Nom { get; set; }

    /// <summary>Rôle fonctionnel en texte libre, tel que nommé par l'exploitation CIT.</summary>
    public required string Role { get; set; }

    /// <summary>
    /// Nature technique typée. Indispensable au calcul des séquences d'arrêt et de démarrage :
    /// un composant NonSpecifie reste invisible des séquences tant qu'il n'est pas typé.
    /// </summary>
    public N4ComponentKind Kind { get; set; } = N4ComponentKind.NonSpecifie;

    /// <summary>Serveur ou machine virtuelle qui héberge le composant.</summary>
    public required string Serveur { get; set; }

    public string? AdresseIp { get; set; }

    public string? NomDns { get; set; }

    public string? SystemeDExploitation { get; set; }

    /// <summary>Nom du service Windows hébergeant le composant, lorsqu'il en existe un.</summary>
    public string? NomDuService { get; set; }

    /// <summary>Processus ou mécanisme de fonctionnement, quand ce n'est pas un service Windows.</summary>
    public string? Mecanisme { get; set; }

    /// <summary>
    /// §2.4 — pilotable, uniquement supervisé, ou ni l'un ni l'autre. Par défaut le composant
    /// n'est que supervisé : accorder le pilotage doit être un geste délibéré.
    /// </summary>
    public ModeDePilotage ModeDePilotage { get; set; } = ModeDePilotage.UniquementSupervise;

    /// <summary>Responsable technique ou fonctionnel désigné pour ce composant.</summary>
    public string? Responsable { get; set; }

    public Criticality Criticite { get; set; } = Criticality.Moyenne;

    public ComponentHealth Sante { get; set; } = ComponentHealth.Inconnu;

    /// <summary>
    /// FR-052 — maintenance déclarée. Pendant une intervention, les signaux ne sont pas
    /// interprétés comme des anomalies et aucune alerte n'est levée.
    /// </summary>
    public bool EnMaintenance { get; set; }

    public DateTimeOffset? DernierControle { get; set; }

    public ValidationStatus Statut { get; set; } = ValidationStatus.Brouillon;

    public List<ComponentEndpoint> Endpoints { get; set; } = [];

    /// <summary>Composants dont celui-ci dépend pour démarrer ou fonctionner.</summary>
    public List<ComponentDependency> Dependances { get; set; } = [];

    /// <summary>Contrôles de santé à exécuter pour établir l'état réel du composant.</summary>
    public List<ComponentCheck> Controles { get; set; } = [];
}

/// <summary>Point d'accès technique d'un composant (HTTP, TCP, JMX, base de données).</summary>
public class ComponentEndpoint : Entity
{
    public Guid ComponentId { get; set; }

    public required string Libelle { get; set; }

    public required string Protocole { get; set; }

    public required string Hote { get; set; }

    public int? Port { get; set; }

    public string? Chemin { get; set; }
}

/// <summary>Dépendance déclarée entre deux composants, orientée du dépendant vers le prérequis.</summary>
public class ComponentDependency : Entity
{
    public Guid ComponentId { get; set; }

    public Guid ComposantRequisId { get; set; }

    /// <summary>Une dépendance bloquante interdit le démarrage tant que le prérequis n'est pas opérationnel.</summary>
    public bool Bloquante { get; set; } = true;

    public string? Justification { get; set; }
}

/// <summary>Contrôle de santé rattaché à un composant (service Windows, port, requête, log).</summary>
public class ComponentCheck : Entity
{
    public Guid ComponentId { get; set; }

    public required string Libelle { get; set; }

    public required string TypeDeControle { get; set; }

    /// <summary>Paramètres du contrôle, sérialisés. Ne contient jamais de secret (SEC-003).</summary>
    public string? Parametres { get; set; }

    public int TimeoutSecondes { get; set; } = 30;

    public bool Actif { get; set; } = true;
}
